using System.Globalization;
using Microsoft.Data.SqlClient;
using ProductsImport.Data;
using ProductsImport.Localization;
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

    public NeedsAnalysis Analyze(
        List<ImportRow> rows,
        ColumnMapping mapping,
        IReadOnlyDictionary<string, UnitInfo>? unitMapping = null,
        IReadOnlyDictionary<string, VatInfo>? taxRateMapping = null,
        IReadOnlyDictionary<string, bool>? exciseMapping = null)
    {
        var analysis = new NeedsAnalysis();
        var unitMapped = mapping.IsMapped(TargetField.Unit);
        var taxRateMapped = mapping.IsMapped(TargetField.TaxRate);
        var vatMapped = taxRateMapped || mapping.IsMapped(TargetField.Vat);
        var weightedMapped = mapping.IsMapped(TargetField.Weighted);
        var exciseColumnMapped = mapping.IsMapped(TargetField.Excise);

        // A mapped group column always resolves (existing match, or a new group gets created), so it
        // only needs a default for genuinely blank cells - same condition as "not mapped at all".
        analysis.NeedsGroupDefault = false;
        analysis.NeedsUnitDefault = !unitMapped;
        analysis.NeedsVatDefault = !vatMapped;
        analysis.NeedsWeightedDefault = !weightedMapped;
        analysis.NeedsExciseDefault = !exciseColumnMapped;

        foreach (var row in rows)
        {
            if (!analysis.NeedsGroupDefault && mapping.GetValue(row.RawValues, TargetField.Group) == null)
            {
                analysis.NeedsGroupDefault = true;
            }

            if (unitMapped && !analysis.NeedsUnitDefault)
            {
                var raw = mapping.GetValue(row.RawValues, TargetField.Unit);
                var resolvedByMapping = raw != null && unitMapping != null && unitMapping.ContainsKey(raw);
                if (raw == null || (!resolvedByMapping && MatchUnit(raw) == null))
                {
                    analysis.NeedsUnitDefault = true;
                }
            }

            if (vatMapped && !analysis.NeedsVatDefault)
            {
                if (taxRateMapped)
                {
                    var raw = mapping.GetValue(row.RawValues, TargetField.TaxRate);
                    if (raw == null || taxRateMapping == null || !taxRateMapping.ContainsKey(raw))
                    {
                        analysis.NeedsVatDefault = true;
                    }
                }
                else
                {
                    var raw = mapping.GetValue(row.RawValues, TargetField.Vat);
                    if (raw == null || MatchVat(raw) == null)
                    {
                        analysis.NeedsVatDefault = true;
                    }
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

            if (exciseColumnMapped && !analysis.NeedsExciseDefault)
            {
                var raw = mapping.GetValue(row.RawValues, TargetField.Excise);
                var resolvedByMapping = raw != null && exciseMapping != null && exciseMapping.ContainsKey(raw);
                if (raw == null || (!resolvedByMapping && ParseYesNo(raw) == null))
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
        bool defaultExcise,
        IReadOnlyDictionary<string, UnitInfo>? unitMapping = null,
        IReadOnlyDictionary<string, VatInfo>? taxRateMapping = null,
        IReadOnlyDictionary<string, bool>? exciseMapping = null)
    {
        foreach (var row in rows)
        {
            ResolveName(row, mapping);
            ResolveBarcode(row, mapping);
            ResolveGroup(row, mapping, defaultGroup);
            ResolveUnit(row, mapping, defaultUnit, unitMapping);
            ResolveVat(row, mapping, defaultVat, taxRateMapping);
            row.WeightedResolved = ResolveYesNo(row, mapping, TargetField.Weighted, defaultWeighted);
            row.ExciseResolved = ResolveExcise(row, mapping, defaultExcise, exciseMapping);
            row.Uktzed = mapping.GetValue(row.RawValues, TargetField.Uktzed) ?? string.Empty;
        }

        ResolveArticles(rows, mapping);
    }

    private void ResolveName(ImportRow row, ColumnMapping mapping)
    {
        var name = mapping.GetValue(row.RawValues, TargetField.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            row.Error = Strings.T("Err_NoName");
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
                row.Error ??= Strings.T("Err_SkippedNoBarcode");
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

    /// <summary>
    /// A mapped, non-blank cell always resolves: to an existing group by name/code, or - if no group
    /// with that name exists yet - by flagging <see cref="ImportRow.PendingNewGroupName"/> so
    /// <see cref="Import"/> creates it right before the row is inserted. Only a blank cell (or an
    /// unmapped column) falls back to <paramref name="defaultGroup"/>.
    /// </summary>
    private void ResolveGroup(ImportRow row, ColumnMapping mapping, GroupInfo? defaultGroup)
    {
        var raw = mapping.GetValue(row.RawValues, TargetField.Group);
        if (raw == null)
        {
            if (defaultGroup == null)
            {
                row.Error ??= Strings.T("Err_NoGroup");
                return;
            }

            row.GroupCode = defaultGroup.Code;
            return;
        }

        var match = MatchGroup(raw);
        if (match != null)
        {
            row.GroupCode = match.Code;
            return;
        }

        row.PendingNewGroupName = raw;
    }

    /// <summary>An explicit entry in <paramref name="unitMapping"/> (set up via "Сопоставление единиц
    /// измерения") takes priority over auto-matching the raw value by name/id against cls2, which in turn
    /// takes priority over <paramref name="defaultUnit"/> for blank cells or values matched by neither.</summary>
    private void ResolveUnit(ImportRow row, ColumnMapping mapping, UnitInfo? defaultUnit, IReadOnlyDictionary<string, UnitInfo>? unitMapping)
    {
        var raw = mapping.GetValue(row.RawValues, TargetField.Unit);
        UnitInfo? unit;
        if (raw != null && unitMapping != null && unitMapping.TryGetValue(raw, out var mapped))
        {
            unit = mapped;
        }
        else
        {
            var match = raw != null ? MatchUnit(raw) : null;
            unit = match ?? defaultUnit;
        }

        if (unit == null)
        {
            row.Error ??= Strings.T("Err_NoUnit");
            return;
        }

        row.UnitId = unit.Id;
    }

    /// <summary>
    /// When the "Налоговая ставка" column is mapped, its raw value is resolved exclusively through
    /// <paramref name="taxRateMapping"/> (set up via "Сопоставление налоговых групп"), falling back to
    /// <paramref name="defaultVat"/> for blank cells or values with no entry in that mapping - it is
    /// never auto-matched as a percentage. Otherwise the legacy "НДС, %" column (if mapped) is
    /// auto-matched by numeric percentage as before.
    /// </summary>
    private void ResolveVat(ImportRow row, ColumnMapping mapping, VatInfo? defaultVat, IReadOnlyDictionary<string, VatInfo>? taxRateMapping)
    {
        VatInfo? vat;
        var taxRateRaw = mapping.GetValue(row.RawValues, TargetField.TaxRate);
        if (taxRateRaw != null)
        {
            vat = (taxRateMapping != null && taxRateMapping.TryGetValue(taxRateRaw, out var mapped)) ? mapped : defaultVat;
        }
        else
        {
            var raw = mapping.GetValue(row.RawValues, TargetField.Vat);
            var match = raw != null ? MatchVat(raw) : null;
            vat = match ?? defaultVat;
        }

        if (vat == null)
        {
            row.Error ??= Strings.T("Err_NoVat");
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

    /// <summary>An explicit entry in <paramref name="exciseMapping"/> (set up via "Сопоставление
    /// акцизности товара") takes priority over the да/нет/1/0 word-based parsing that
    /// <see cref="ResolveYesNo"/> uses for "Весовой".</summary>
    private bool ResolveExcise(ImportRow row, ColumnMapping mapping, bool defaultValue, IReadOnlyDictionary<string, bool>? exciseMapping)
    {
        var raw = mapping.GetValue(row.RawValues, TargetField.Excise);
        if (raw == null)
        {
            return defaultValue;
        }

        if (exciseMapping != null && exciseMapping.TryGetValue(raw, out var mapped))
        {
            return mapped;
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
                row.Error ??= Strings.T("Err_ArticleTaken", articleId);
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

    /// <summary>Creates one gru2 row per distinct <see cref="ImportRow.PendingNewGroupName"/>, in its
    /// own transaction (committed regardless of how individual product rows later fare), and resolves
    /// every such row's <see cref="ImportRow.GroupCode"/> from the result.</summary>
    private void CreateMissingGroups(SqlServerRepository repository, SqlConnection connection, List<ImportRow> rows)
    {
        var pendingNames = rows
            .Where(r => r.PendingNewGroupName != null)
            .Select(r => r.PendingNewGroupName!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (pendingNames.Count == 0)
        {
            return;
        }

        var createdCodes = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        using (var transaction = connection.BeginTransaction())
        {
            try
            {
                foreach (var name in pendingNames)
                {
                    var code = repository.InsertGroup(connection, transaction, name);
                    createdCodes[name] = code;
                    Groups.Add(new GroupInfo { Code = code, Name = name });
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        foreach (var row in rows)
        {
            if (row.PendingNewGroupName != null && createdCodes.TryGetValue(row.PendingNewGroupName, out var code))
            {
                row.GroupCode = code;
                row.PendingNewGroupName = null;
            }
        }
    }

    public ImportSummary Import(SqlServerRepository repository, SqlConnection connection, List<ImportRow> rows)
    {
        CreateMissingGroups(repository, connection, rows);

        var summary = new ImportSummary { TotalRows = rows.Count };

        foreach (var row in rows)
        {
            if (row.BarcodeNeedsResolution && !row.HasError)
            {
                row.Error = Strings.T("Err_BarcodeUnresolved");
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
                    Uktzed = row.Uktzed,
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
                    Message = Strings.T("Err_DbError", ex.Message)
                });
            }
        }

        summary.Skipped = rows.Count(r => r.HasError && r.BarcodeAction == BarcodeAction.Skip);
        return summary;
    }
}
