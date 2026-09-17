namespace ProductsImport.Models;

/// <summary>
/// A saved column mapping (spreadsheet column index -&gt; database field), reusable for documents
/// that share the same structure (e.g. the same price-list template from a supplier) but carry
/// different products.
/// </summary>
public class ImportProfile
{
    public string Name { get; set; } = string.Empty;

    /// <summary>1-based row number where the column headers are; import starts on the next row.</summary>
    public int HeaderRowNumber { get; set; } = 1;

    /// <summary>Column index (0-based) -> target field.</summary>
    public Dictionary<int, TargetField> Columns { get; set; } = new();

    public override string ToString() => Name;
}
