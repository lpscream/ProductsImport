namespace ProductsImport.Models;

/// <summary>A row from gru2 (top-level product group, gru2002 = 1).</summary>
public class GroupInfo
{
    /// <summary>gru2001 - the group's own code, stored as ass4003 when a product is assigned to it.</summary>
    public long Code { get; set; }

    /// <summary>gru2004 - display name.</summary>
    public string Name { get; set; } = string.Empty;

    public override string ToString() => $"{Name} ({Code})";
}
