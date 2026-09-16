using System.Globalization;
using Microsoft.Data.SqlClient;
using ProductsImport.Data;
using ProductsImport.Models;

namespace ProductsImport.Services;

public class NeedsAnalysis
{
    public bool NeedsGroupDefault { get; set; }
    public bool NeedsUnitDefault { get; set; }
    public bool NeedsVatDefault { get; set; }
    public bool NeedsWeightedDefault { get; set; }
    public bool NeedsExciseDefault { get; set; }
}

public class ImportSummary
{
    public int TotalRows { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public int Failed { get; set; }
    public List<RowImportError> Errors { get; } = new();
}

/// <summary>
/// Turns raw spreadsheet rows into ass1/ass2/ass3/ass4 records: resolves column mappings, applies
/// fallback defaults, flags barcode problems for manual resolution, and finally writes each row inside
/// its own transaction so a bad row does not block the rest of the batch.
/// </summary>
public class ImportOrchestrator
{
    private static readonly string[] TruthyWords = { "да", "д", "yes", "y", "1", "true", "истина" };
    private static readonly string[] FalsyWords = { "нет", "н", "no", "n", "0", "false", "ложь" };

    private readonly Random _random = new();
    private readonly HashSet<string> _reservedBarcodes;
    private readonly ArticleAllocator _articleAllocator;

    public List<GroupInfo> Groups { get; }
    public List<UnitInfo> Units { get; }
    public List<VatInfo> VatRates { get; }

    public ImportOrchestrator(
        List<GroupInfo> groups,
        List<UnitInfo> units,
        List<VatInfo> vatRates,
        HashSet<long> existingArticleIds,
        HashSet<string> existingBarcodes)
    {
        Groups = groups;
        Units = units;
        VatRates = vatRates;
        _reservedBarcodes = new HashSet<string>(existingBarcodes, StringComparer.Ordinal);
        _articleAllocator = new ArticleAllocator(existingArticleIds);
    }

    public List<ImportRow> BuildRows(List<string[]> dataRows, int firstDisplayRowNumber)
    {
        var rows = new List<ImportRow>();
        for (var i = 0; i < dataRows.Count; i++)
        {
            var raw = dataRows[i];
            if (raw.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(new ImportRow
            {
                SourceRowNumber = firstDisplayRowNumber + i,
                RawValues = raw
            });
        }

        return rows;
    }

    public NeedsAnalysis Analyze(List<ImportRow> rows, ColumnMapping mapping)
    {
        var analysis = new NeedsAnalysis();
        var groupMapped = mapping.IsMapped(TargetField.Group);
        var unitMapped = mapping.IsMapped(TargetField.Unit);
        var vatMapped = mapping.IsMapped(TargetField.Vat);
        var weightedMapped = mapping.IsMapped(TargetField.Weighted);
        var exciseMapped = mapping.IsMapped(TargetField.Excise);

        analysis.NeedsGroupDefault = !groupMapped;
        analysis.NeedsUnitDefault = !unitMapped;
        analysis.NeedsVatDefault = !vatMapped;
        analysis.NeedsWeightedDefault = !weightedMapped;
        analysis.NeedsExciseDefault = !exciseMapped;

        foreach (var row in rows)
        {
            if (groupMapped && !analysis.NeedsGroupDefault)
            {
                var raw = mapping.GetValue(row.RawValues, TargetField.Group);
                if (raw == null || MatchGroup(raw) == null)
                {
                    analysis.NeedsGroupDefault = true;
                }
            }

            if (unitMapped && !analysis.NeedsUnitDefault)
            {
                var raw = mapping.GetValue(row.RawValues, TargetField.Unit);
                if (raw == null || MatchUnit(raw) == null)
                {
                    analysis.NeedsUnitDefault = true;
                }
            }

            if (vatMapped && !analysis.NeedsVatDefault)
            {
                var raw = mapping.GetValue(row.RawValues, TargetField.Vat);
                if (raw == null || MatchVat(raw) == null)
                {
                    analysis.NeedsVatDefault = true;
                }
            }

            if (weightedMapped && !analysis.NeedsWeightedDefault)
            {
                var raw = mapping.GetValue(row.RawValues, TargetField.Weighted);
                if (raw == null || ParseYesNo(raw) == null)
                {
                    analysis.NeedsWeightedDefault = true;
                }
            }

            if (exciseMapped && !analysis.NeedsExciseDefault)
            {
                var raw = mapping.GetValue(row.RawValues, TargetField.Excise);
                if (raw == null || ParseYesNo(raw) == null)
                {
                    analysis.NeedsExciseDefault = true;
                }
            }
        }

        return analysis;
    }

    /// <summary>
    /// First resolution pass: name, barcode, group, unit, vat, weighted, excise and article numbers.
    /// Rows whose barcode is missing/invalid/duplicate are left with <see cref="ImportRow.BarcodeNeedsResolution"/>
    /// set and must be resolved afterwards via <see cref="ApplyBarcodeResolution"/>.
    /// </summary>
    public void Resolve(
        List<ImportRow> rows,
        ColumnMapping mapping,
        GroupInfo? defaultGroup,
        UnitInfo? defaultUnit,
        VatInfo? defaultVat,
        bool defaultWeighted,
        bool defaultExcise)
    {
        foreach (var row in rows)
        {
            ResolveName(row, mapping);
            ResolveBarcode(row, mapping);
            ResolveGroup(row, mapping, defaultGroup);
            ResolveUnit(row, mapping, defaultUnit);
            ResolveVat(row, mapping, defaultVat);
            row.WeightedResolved = ResolveYesNo(row, mapping, TargetField.Weighted, defaultWeighted);
            row.ExciseResolved = ResolveYesNo(row, mapping, TargetField.Excise, defaultExcise);
        }

        ResolveArticles(rows, mapping);
    }

    private void ResolveName(ImportRow row, ColumnMapping mapping)
    {
        var name = mapping.GetValue(row.RawValues, TargetField.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            row.Error = "Не указано наименование товара";
            return;
        }

        row.Name = name.Trim();
    }

    private void ResolveBarcode(ImportRow row, ColumnMapping mapping)
    {
        var raw = mapping.GetValue(row.RawValues, TargetField.Barcode);
        row.RawBarcode = raw;

        if (raw != null && BarcodeGenerator.IsValidBarcode(raw))
        {
            var normalized = raw.Trim();
            if (!_reservedBarcodes.Contains(normalized))
            {
                _reservedBarcodes.Add(normalized);
                row.ResolvedBarcode = normalized;
                row.BarcodeNeedsResolution = false;
                return;
            }
        }

        row.BarcodeNeedsResolution = true;
    }

    /// <summary>Called after the user has decided what to do with rows flagged by <see cref="ResolveBarcode"/>.</summary>
    public bool ApplyBarcodeResolution(ImportRow row, BarcodeAction action, string? manualBarcode)
    {
        row.BarcodeAction = action;

        switch (action)
        {
            case BarcodeAction.Generate:
                row.ResolvedBarcode = BarcodeGenerator.GenerateUniqueEan13(_reservedBarcodes, _random);
                row.BarcodeNeedsResolution = false;
                return true;

            case BarcodeAction.Skip:
                row.BarcodeNeedsResolution = false;
                row.Error ??= "Пропущено пользователем: нет штрих-кода";
                return true;

            case BarcodeAction.Manual:
                var normalized = manualBarcode?.Trim() ?? string.Empty;
                if (!BarcodeGenerator.IsValidBarcode(normalized) || _reservedBarcodes.Contains(normalized))
                {
                    return false;
                }

                _reservedBarcodes.Add(normalized);
                row.ResolvedBarcode = normalized;
                row.BarcodeNeedsResolution = false;
                return true;

            default:
                return false;
        }
    }

    private void ResolveGroup(ImportRow row, ColumnMapping mapping, GroupInfo? defaultGroup)
    {
        var raw = mapping.GetValue(row.RawValues, TargetField.Group);
        var match = raw != null ? MatchGroup(raw) : null;
        var group = match ?? defaultGroup;

        if (group == null)
        {
            row.Error ??= "Не удалось определить группу товара";
            return;
        }

        row.GroupCode = group.Code;
    }

    private void ResolveUnit(ImportRow row, ColumnMapping mapping, UnitInfo? defaultUnit)
    {
        var raw = mapping.GetValue(row.RawValues, TargetField.Unit);
        var match = raw != null ? MatchUnit(raw) : null;
        var unit = match ?? defaultUnit;

        if (unit == null)
        {
            row.Error ??= "Не удалось определить единицу измерения";
            return;
        }

        row.UnitId = unit.Id;
    }

    private void ResolveVat(ImportRow row, ColumnMapping mapping, VatInfo? defaultVat)
    {
        var raw = mapping.GetValue(row.RawValues, TargetField.Vat);
        var match = raw != null ? MatchVat(raw) : null;
        var vat = match ?? defaultVat;

        if (vat == null)
        {
            row.Error ??= "Не удалось определить ставку НДС";
            return;
        }

        row.VatId = vat.Id;
    }

    private bool ResolveYesNo(ImportRow row, ColumnMapping mapping, TargetField field, bool defaultValue)
    {
        var raw = mapping.GetValue(row.RawValues, field);
        if (raw == null)
        {
            return defaultValue;
        }

        return ParseYesNo(raw) ?? defaultValue;
    }

    private void ResolveArticles(List<ImportRow> rows, ColumnMapping mapping)
    {
        // First reserve every explicit, valid, non-conflicting article number...
        foreach (var row in rows)
        {
            var raw = mapping.GetValue(row.RawValues, TargetField.Article);
            if (raw == null || !long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var articleId) || articleId <= 0)
            {
                continue;
            }

            if (_articleAllocator.IsTaken(articleId))
            {
                row.Error ??= $"Артикул {articleId} уже используется другим товаром";
                continue;
            }

            _articleAllocator.Reserve(articleId);
            row.Article = articleId;
        }

        // ...then auto-assign the smallest free article number to everything else.
        foreach (var row in rows)
        {
            if (row.Article == null && !row.HasError)
            {
                row.Article = _articleAllocator.Next();
            }
        }
    }

    private GroupInfo? MatchGroup(string raw)
    {
        var trimmed = raw.Trim();
        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code))
        {
            var byCode = Groups.FirstOrDefault(g => g.Code == code);
            if (byCode != null)
            {
                return byCode;
            }
        }

        return Groups.FirstOrDefault(g => string.Equals(g.Name, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private UnitInfo? MatchUnit(string raw)
    {
        var trimmed = raw.Trim();
        if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id))
        {
            var byId = Units.FirstOrDefault(u => u.Id == id);
            if (byId != null)
            {
                return byId;
            }
        }

        return Units.FirstOrDefault(u => string.Equals(u.Name, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    private VatInfo? MatchVat(string raw)
    {
        var cleaned = raw.Replace("%", string.Empty).Replace(',', '.').Trim();
        if (!decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate))
        {
            return null;
        }

        return VatRates.FirstOrDefault(v => Math.Abs(v.RatePercent - rate) < 0.01m);
    }

    private static bool? ParseYesNo(string raw)
    {
        var trimmed = raw.Trim();
        if (TruthyWords.Any(w => string.Equals(w, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (FalsyWords.Any(w => string.Equals(w, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return null;
    }

    public ImportSummary Import(SqlServerRepository repository, SqlConnection connection, List<ImportRow> rows)
    {
        var summary = new ImportSummary { TotalRows = rows.Count };

        foreach (var row in rows)
        {
            if (row.BarcodeNeedsResolution && !row.HasError)
            {
                row.Error = "Штрих-код не был разрешён перед импортом";
            }

            if (row.HasError)
            {
                summary.Failed++;
                summary.Errors.Add(new RowImportError
                {
                    SourceRowNumber = row.SourceRowNumber,
                    RawValues = row.RawValues,
                    Message = row.Error!
                });
                continue;
            }

            using var transaction = connection.BeginTransaction();
            try
            {
                var record = new ProductRecord
                {
                    ArticleId = row.Article!.Value,
                    Name = row.Name!,
                    Barcode = row.ResolvedBarcode!,
                    VatId = row.VatId!.Value,
                    UnitId = row.UnitId!.Value,
                    Weighted = row.WeightedResolved,
                    Excise = row.ExciseResolved,
                    GroupCode = row.GroupCode!.Value,
                    SourceRowNumber = row.SourceRowNumber
                };

                repository.InsertProduct(connection, transaction, record);
                transaction.Commit();
                summary.Imported++;
            }
            catch (Exception ex) when (ex is SqlException or InvalidOperationException)
            {
                transaction.Rollback();
                summary.Failed++;
                summary.Errors.Add(new RowImportError
                {
                    SourceRowNumber = row.SourceRowNumber,
                    RawValues = row.RawValues,
                    Message = $"Ошибка базы данных: {ex.Message}"
                });
            }
        }

        summary.Skipped = rows.Count(r => r.HasError && r.BarcodeAction == BarcodeAction.Skip);
        return summary;
    }
}
