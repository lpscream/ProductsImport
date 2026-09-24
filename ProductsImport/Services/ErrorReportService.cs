using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using ProductsImport.Localization;
using ProductsImport.Models;

namespace ProductsImport.Services;

/// <summary>Writes rows that failed to import to a new .xlsx file so they can be fixed and re-imported.</summary>
public static class ErrorReportService
{
    public static void WriteErrors(string filePath, string[]? headerRow, IReadOnlyList<RowImportError> errors)
    {
        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet(Strings.T("ErrRep_SheetName"));

        var boldStyle = workbook.CreateCellStyle();
        var boldFont = workbook.CreateFont();
        boldFont.IsBold = true;
        boldStyle.SetFont(boldFont);

        var columnCount = Math.Max(headerRow?.Length ?? 0, errors.Count == 0 ? 0 : errors.Max(e => e.RawValues.Length));

        var headerRowIndex = 0;
        var outRow = sheet.CreateRow(headerRowIndex);
        for (var c = 0; c < columnCount; c++)
        {
            var cell = outRow.CreateCell(c);
            cell.SetCellValue(headerRow != null && c < headerRow.Length ? headerRow[c] : Strings.T("ErrRep_ColumnFallback", c + 1));
            cell.CellStyle = boldStyle;
        }

        var errorCell = outRow.CreateCell(columnCount);
        errorCell.SetCellValue(Strings.T("ErrRep_ColHeader"));
        errorCell.CellStyle = boldStyle;

        var rowIndex = headerRowIndex + 1;
        foreach (var error in errors)
        {
            var row = sheet.CreateRow(rowIndex++);
            for (var c = 0; c < error.RawValues.Length; c++)
            {
                row.CreateCell(c).SetCellValue(error.RawValues[c] ?? string.Empty);
            }

            row.CreateCell(columnCount).SetCellValue(error.Message);
        }

        for (var c = 0; c < columnCount; c++)
        {
            var header = headerRow != null && c < headerRow.Length ? headerRow[c] : null;
            var values = errors.Select(e => c < e.RawValues.Length ? e.RawValues[c] : null);
            SetColumnWidth(sheet, c, header, values);
        }

        SetColumnWidth(sheet, columnCount, Strings.T("ErrRep_ColHeader"), errors.Select(e => e.Message));

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        workbook.Write(stream);
    }

    /// <summary>
    /// Sizes a column from the character length of its content instead of NPOI's AutoSizeColumn,
    /// which needs SkiaSharp's native library for font metrics - a dependency that isn't reliably
    /// deployed for classic .NET Framework apps that only pull it in transitively through NPOI.
    /// </summary>
    private static void SetColumnWidth(ISheet sheet, int columnIndex, string? header, IEnumerable<string?> values)
    {
        var maxLength = header?.Length ?? 0;
        foreach (var value in values)
        {
            maxLength = Math.Max(maxLength, value?.Length ?? 0);
        }

        var characters = Math.Min(Math.Max(maxLength, 6) + 2, 60);
        sheet.SetColumnWidth(columnIndex, characters * 256);
    }
}
