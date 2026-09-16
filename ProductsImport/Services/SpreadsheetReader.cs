using NPOI.SS.UserModel;
using ProductsImport.Models;

namespace ProductsImport.Services;

/// <summary>Reads price lists / invoices from .xlsx, .xls or .csv into a plain in-memory grid of strings.</summary>
public static class SpreadsheetReader
{
    public static SpreadsheetDocument Load(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".csv" => LoadCsv(filePath),
            _ => LoadWithNpoi(filePath)
        };
    }

    private static SpreadsheetDocument LoadWithNpoi(string filePath)
    {
        using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var workbook = WorkbookFactory.Create(stream);

        var document = new SpreadsheetDocument
        {
            FilePath = filePath
        };

        var evaluator = workbook.GetCreationHelper().CreateFormulaEvaluator();

        for (var sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
        {
            var sheet = workbook.GetSheetAt(sheetIndex);
            var sheetName = sheet.SheetName;
            document.SheetNames.Add(sheetName);

            var rows = new List<string[]>();
            var lastColumn = 0;
            for (var r = sheet.FirstRowNum; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row != null)
                {
                    lastColumn = Math.Max(lastColumn, row.LastCellNum);
                }
            }

            for (var r = sheet.FirstRowNum; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                var values = new string[Math.Max(lastColumn, 0)];
                if (row != null)
                {
                    for (var c = 0; c < values.Length; c++)
                    {
                        var cell = row.GetCell(c, MissingCellPolicy.RETURN_BLANK_AS_NULL);
                        values[c] = cell == null ? string.Empty : ReadCellAsString(cell, evaluator);
                    }
                }

                rows.Add(values);
            }

            document.Sheets[sheetName] = rows;
        }

        return document;
    }

    private static string ReadCellAsString(ICell cell, IFormulaEvaluator evaluator)
    {
        try
        {
            var cellType = cell.CellType == CellType.Formula ? cell.CachedFormulaResultType : cell.CellType;

            switch (cellType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    if (DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.DateCellValue?.ToString("dd.MM.yyyy") ?? string.Empty;
                    }

                    return cell.NumericCellValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case CellType.Boolean:
                    return cell.BooleanCellValue ? "1" : "0";
                case CellType.Blank:
                    return string.Empty;
                default:
                    var evaluated = evaluator.Evaluate(cell);
                    return evaluated?.FormatAsString() ?? string.Empty;
            }
        }
        catch
        {
            return cell.ToString() ?? string.Empty;
        }
    }

    private static SpreadsheetDocument LoadCsv(string filePath)
    {
        var lines = File.ReadAllLines(filePath, System.Text.Encoding.UTF8);
        var delimiter = DetectDelimiter(lines);

        var rows = lines.Select(line => ParseCsvLine(line, delimiter)).ToList();
        var sheetName = Path.GetFileNameWithoutExtension(filePath);

        var document = new SpreadsheetDocument
        {
            FilePath = filePath
        };
        document.SheetNames.Add(sheetName);
        document.Sheets[sheetName] = rows;
        return document;
    }

    private static char DetectDelimiter(string[] lines)
    {
        if (lines.Length == 0)
        {
            return ',';
        }

        var sample = lines[0];
        var commaCount = sample.Count(c => c == ',');
        var semicolonCount = sample.Count(c => c == ';');
        var tabCount = sample.Count(c => c == '\t');

        if (tabCount >= commaCount && tabCount >= semicolonCount && tabCount > 0)
        {
            return '\t';
        }

        return semicolonCount > commaCount ? ';' : ',';
    }

    private static string[] ParseCsvLine(string line, char delimiter)
    {
        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == delimiter)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        fields.Add(current.ToString());
        return fields.Select(f => f.Trim()).ToArray();
    }
}
