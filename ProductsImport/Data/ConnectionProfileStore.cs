using ProductsImport.Models;

namespace ProductsImport.Data;

/// <summary>Loads and saves the list of saved SQL Server connection profiles.</summary>
public static class ConnectionProfileStore
{
    private const string FileName = "connections.json";

    public static List<ConnectionProfile> Load() => JsonFileStore<ConnectionProfile>.Load(FileName);

    public static void Save(List<ConnectionProfile> profiles) => JsonFileStore<ConnectionProfile>.Save(FileName, profiles);
}
