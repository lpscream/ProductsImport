namespace ProductsImport.Models;

/// <summary>Remembers the last-used connection/import profile and UI preferences across runs.</summary>
public class AppSettings
{
    public string? LastConnectionName { get; set; }
    public string? LastConnectionServer { get; set; }
    public string? LastImportProfileName { get; set; }

    /// <summary>
    /// When true (default), rows whose barcode needs resolving are shown in an editable list before
    /// import; when false, they are auto-resolved (unique EAN-13 generated) without showing the list.
    /// </summary>
    public bool ShowBarcodeIssuesList { get; set; } = true;
}
