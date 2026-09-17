using ProductsImport.Models;

namespace ProductsImport.Data;

/// <summary>Loads and saves the list of saved import (column-mapping) profiles.</summary>
public static class ImportProfileStore
{
    private const string FileName = "import_profiles.json";

    public static List<ImportProfile> Load() => JsonFileStore<ImportProfile>.Load(FileName);

    public static void Save(List<ImportProfile> profiles) => JsonFileStore<ImportProfile>.Save(FileName, profiles);
}
