namespace ProductsImport.Models;

/// <summary>A fully resolved product ready to be inserted into ass1/ass2/ass3/ass4.</summary>
public class ProductRecord
{
    public long ArticleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public long VatId { get; set; }
    public long UnitId { get; set; }
    public bool Weighted { get; set; }
    public bool Excise { get; set; }
    public long GroupCode { get; set; }

    /// <summary>УКТЗЕД code -> ass2031. Empty when not present in the document.</summary>
    public string Uktzed { get; set; } = string.Empty;

    /// <summary>1-based row number in the source document, used for error reporting.</summary>
    public int SourceRowNumber { get; set; }
}
