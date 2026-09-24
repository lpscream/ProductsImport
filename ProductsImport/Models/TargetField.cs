namespace ProductsImport.Models;

/// <summary>The database field a spreadsheet column can be mapped to.</summary>
public enum TargetField
{
    None,

    /// <summary>Required. Product name -> ass1003 / ass2007.</summary>
    Name,

    /// <summary>Barcode (EAN-8/EAN-13) -> ass3002.</summary>
    Barcode,

    /// <summary>Group name/code -> ass4003 (via gru2).</summary>
    Group,

    /// <summary>Article number -> ass1001 / ass1002.</summary>
    Article,

    /// <summary>Sold by weight, yes/no -> ass1006.</summary>
    Weighted,

    /// <summary>Unit of measure -> ass1005 (via cls2).</summary>
    Unit,

    /// <summary>VAT rate, auto-matched by numeric percentage -> ass1004 (via nds1).</summary>
    Vat,

    /// <summary>
    /// Tax rate/label, resolved via an explicit raw-value -> nds1 mapping (see "Сопоставление
    /// налоговых групп") instead of being auto-matched as a percentage -> ass1004 (via nds1).
    /// </summary>
    TaxRate,

    /// <summary>Excise (подакцизный), yes/no -> ass1007 and ass2010.</summary>
    Excise,

    /// <summary>УКТЗЕД code -> ass2031. Optional: left empty when not mapped or not filled in.</summary>
    Uktzed
}
