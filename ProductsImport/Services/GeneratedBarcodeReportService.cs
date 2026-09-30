using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using ProductsImport.Localization;
using ProductsImport.Models;

namespace ProductsImport.Services;

/// <summary>
/// Writes copies of the original document with the barcode column's value replaced by the
/// auto-generated barcode for every row where one was actually generated during import.
/// </summary>
public static class GeneratedBarcodeReportService
{
    /// <summary>The full document - identical row for row, column for column, including any rows above
    /// or below the header - only with generated-barcode cells patched.</summary>
    public static void WriteDocument(string filePath, List<string[]> originalRows, int barcodeColumnIndex, List<ImportRow> rows)
    {
        var generatedByRowNumber = BuildGeneratedByRowNumber(rows);

        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(Strings.T("GeneratedBarcodes_SheetName"));

        for (var r = 0; r < originalRows.Count; r++)
        {
            // originalRows is 0-based; ImportRow.SourceRowNumber is the same sheet's 1-based row number.
            generatedByRowNumber.TryGetValue(r + 1, out var generatedBarcode);
            WriteRow(sheet, r, originalRows[r], barcodeColumnIndex, generatedBarcode);
        }

        Save(workbook, filePath);
    }

    /// <summary>Same column structure and header rows as the original document, but only the product
    /// rows whose barcode was actually generated - for a standalone "what got a new barcode" list.</summary>
    public static void WriteGeneratedOnlyDocument(string filePath, List<string[]> originalRows, int headerRowIndex,
        int barcodeColumnIndex, List<ImportRow> rows)
    {
        var generatedByRowNumber = BuildGeneratedByRowNumber(rows);

        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(Strings.T("GeneratedBarcodes_SheetName"));

        var outIndex = 0;
        for (var r = 0; r <= headerRowIndex && r < originalRows.Count; r++)
        {
            WriteRow(sheet, outIndex++, originalRows[r], -1, null);
        }

        foreach (var row in rows.Where(r => r.BarcodeWasGenerated && r.ResolvedBarcode != null))
        {
            var sourceIndex = row.SourceRowNumber - 1;
            if (sourceIndex < 0 || sourceIndex >= originalRows.Count)
            {
                continue;
            }

            WriteRow(sheet, outIndex++, originalRows[sourceIndex], barcodeColumnIndex, row.ResolvedBarcode);
        }

        Save(workbook, filePath);
    }

    private static Dictionary<int, string> BuildGeneratedByRowNumber(List<ImportRow> rows) =>
        rows.Where(r => r.BarcodeWasGenerated && r.ResolvedBarcode != null)
            .ToDictionary(r => r.SourceRowNumber, r => r.ResolvedBarcode!);

    private static void WriteRow(ISheet sheet, int rowIndex, string[] source, int substituteColumnIndex, string? substituteValue)
    {
        var outRow = sheet.CreateRow(rowIndex);
        for (var c = 0; c < source.Length; c++)
        {
            var value = c == substituteColumnIndex && substituteValue != null ? substituteValue : source[c] ?? string.Empty;
            outRow.CreateCell(c).SetCellValue(value);
        }
    }

    private static void Save(XSSFWorkbook workbook, string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        workbook.Write(stream);
    }
}
