namespace ProductsImport.Models;

/// <summary>Remembers the last-used connection and import profile so the app can restore them on startup.</summary>
public class AppSettings
{
    public string? LastConnectionName { get; set; }
    public string? LastConnectionServer { get; set; }
    public string? LastImportProfileName { get; set; }
}
