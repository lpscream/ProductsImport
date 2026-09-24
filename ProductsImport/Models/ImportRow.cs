namespace ProductsImport.Models;

/// <summary>Working state of one source data row as it moves through the resolution pipeline.</summary>
public class ImportRow
{
    public int SourceRowNumber { get; set; }
    public string[] RawValues { get; set; } = Array.Empty<string>();

    // Resolved values (filled in by ImportOrchestrator).
    public string? Name { get; set; }
    public string? RawBarcode { get; set; }
    public string? ResolvedBarcode { get; set; }
    public bool BarcodeNeedsResolution { get; set; }
    public BarcodeAction BarcodeAction { get; set; } = BarcodeAction.Generate;

    public long? Article { get; set; }
    public bool WeightedResolved { get; set; }
    public bool ExciseResolved { get; set; }
    public long? UnitId { get; set; }
    public long? VatId { get; set; }
    public long? GroupCode { get; set; }

    /// <summary>Set instead of <see cref="GroupCode"/> when the document's group name doesn't exist
    /// in gru2 yet; a new group is created for it right before the row is inserted.</summary>
    public string? PendingNewGroupName { get; set; }

    public string Uktzed { get; set; } = string.Empty;

    /// <summary>Set as soon as the row is known to be unimportable; skips further resolution.</summary>
    public string? Error { get; set; }

    public bool HasError => !string.IsNullOrEmpty(Error);
}
