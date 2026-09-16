using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using ProductsImport.Models;

namespace ProductsImport.Services;

/// <summary>Writes rows that failed to import to a new .xlsx file so they can be fixed and re-imported.</summary>
public static class ErrorReportService
{
    public static void WriteErrors(string filePath, string[]? headerRow, IReadOnlyList<RowImportError> errors)
    {
        using var workbook = new XSSFWorkbook();
        var sheet = workbook.CreateSheet("Ошибки импорта");

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
            cell.SetCellValue(headerRow != null && c < headerRow.Length ? headerRow[c] : $"Колонка {c + 1}");
            cell.CellStyle = boldStyle;
        }

        var errorCell = outRow.CreateCell(columnCount);
        errorCell.SetCellValue("Ошибка импорта");
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

        for (var c = 0; c <= columnCount; c++)
        {
            sheet.AutoSizeColumn(c);
        }

        using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        workbook.Write(stream);
    }
}
