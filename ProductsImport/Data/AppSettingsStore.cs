using ProductsImport.Models;

namespace ProductsImport.Data;

/// <summary>Loads and saves the last-used connection/import profile, restored automatically on startup.</summary>
public static class AppSettingsStore
{
    private const string FileName = "settings.json";

    public static AppSettings Load() => JsonStore.LoadObject<AppSettings>(FileName);

    public static void Save(AppSettings settings) => JsonStore.SaveObject(FileName, settings);
}
