namespace ProductsImport.Models;

/// <summary>A row from nds1 (VAT rate classifier).</summary>
public class VatInfo
{
    /// <summary>nds1001 - primary key, stored as ass1004.</summary>
    public long Id { get; set; }

    /// <summary>nds1003 - VAT rate expressed as a percentage (e.g. 20 for 20%).</summary>
    public decimal RatePercent { get; set; }

    public override string ToString() => $"{RatePercent:0.##}%";
}
