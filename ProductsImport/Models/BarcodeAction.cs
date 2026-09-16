namespace ProductsImport.Models;

/// <summary>What to do with a row whose barcode is missing or invalid.</summary>
public enum BarcodeAction
{
    /// <summary>Generate a unique internal EAN-13 barcode.</summary>
    Generate,

    /// <summary>Do not import this row.</summary>
    Skip,

    /// <summary>The user has typed a barcode manually.</summary>
    Manual
}
