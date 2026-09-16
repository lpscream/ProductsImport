namespace ProductsImport.Models;

/// <summary>An opened price-list/invoice workbook, all sheets read eagerly into memory as strings.</summary>
public class SpreadsheetDocument
{
    public string FilePath { get; init; } = string.Empty;
    public List<string> SheetNames { get; init; } = new();
    public Dictionary<string, List<string[]>> Sheets { get; init; } = new();

    public List<string[]> GetRows(string sheetName) => Sheets.TryGetValue(sheetName, out var rows) ? rows : new List<string[]>();
}
