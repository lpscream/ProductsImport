namespace ProductsImport.Models;

/// <summary>A source row that could not be imported, kept so it can be exported back to Excel.</summary>
public class RowImportError
{
    public int SourceRowNumber { get; set; }
    public string[] RawValues { get; set; } = Array.Empty<string>();
    public string Message { get; set; } = string.Empty;
}
