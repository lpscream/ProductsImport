using NPOI.XSSF.UserModel;
using ProductsImport.Localization;
using ProductsImport.Models;

namespace ProductsImport.Services;

/// <summary>
/// Writes a copy of the original document - identical row for row, column for column, including any
/// rows above the header - except that the barcode column's value is replaced by the auto-generated
/// barcode for every row where one was actually generated during import.
/// </summary>
public static class GeneratedBarcodeReportService
{
    public static void WriteDocument(string filePath, List<string[]> originalRows, int barcodeColumnIndex, List<ImportRow> rows)
    {
        var generatedByRowNumber = rows
            .Where(r => r.BarcodeWasGenerated && r.ResolvedBarcode != null)
            .ToDictionary(r => r.SourceRowNumber, r => r.ResolvedBarcode!);

        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(Strings.T("GeneratedBarcodes_SheetName"));

        for (var r = 0; r < originalRows.Count; r++)
        {
            var source = originalRows[r];
            var outRow = sheet.CreateRow(r);
            // originalRows is 0-based; ImportRow.SourceRowNumber is the same sheet's 1-based row number.
            var hasGenerated = generatedByRowNumber.TryGetValue(r + 1, out var generatedBarcode);

            for (var c = 0; c < source.Length; c++)
            {
                var value = c == barcodeColumnIndex && hasGenerated ? generatedBarcode! : source[c] ?? string.Empty;
                outRow.CreateCell(c).SetCellValue(value);
            }
        }

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        workbook.Write(stream);
    }
}
