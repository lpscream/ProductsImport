using ProductsImport.Localization;

namespace ProductsImport.Models;

/// <summary>Remembers the last-used connection, import profile and interface language so the app can
/// restore them on startup.</summary>
public class AppSettings
{
    public string? LastConnectionName { get; set; }
    public string? LastConnectionServer { get; set; }
    public string? LastImportProfileName { get; set; }
    public Language Language { get; set; } = Language.Russian;
}
