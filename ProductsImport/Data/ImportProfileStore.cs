using ProductsImport.Models;

namespace ProductsImport.Data;

/// <summary>Loads and saves the list of saved import profiles (header row + column mapping).</summary>
public static class ImportProfileStore
{
    private const string FileName = "import_profiles.json";

    public static List<ImportProfile> Load() => JsonStore.LoadList<ImportProfile>(FileName);

    public static void Save(List<ImportProfile> profiles) => JsonStore.SaveList(FileName, profiles);
}
