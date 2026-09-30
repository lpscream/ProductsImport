namespace ProductsImport.Models;

/// <summary>A row from nds1 (VAT rate classifier).</summary>
public class VatInfo
{
    /// <summary>nds1001 - primary key, stored as ass1004.</summary>
    public long Id { get; set; }

    /// <summary>nds1002 - display name, shown everywhere a tax rate is picked or listed.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>nds1003 - VAT rate expressed as a percentage (e.g. 20 for 20%), used only for the
    /// legacy "НДС, %" auto-match by numeric percentage.</summary>
    public decimal RatePercent { get; set; }

    public override string ToString() => Name;
}
