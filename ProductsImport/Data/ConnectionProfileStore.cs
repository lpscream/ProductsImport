using System.Text.Json;
using ProductsImport.Models;

namespace ProductsImport.Data;

/// <summary>Loads and saves the list of saved SQL Server connection profiles as JSON under %AppData%.</summary>
public static class ConnectionProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string FolderPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProductsImport");

    private static string FilePath => Path.Combine(FolderPath, "connections.json");

    public static List<ConnectionProfile> Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new List<ConnectionProfile>();
            }

            var json = File.ReadAllText(FilePath);
            var profiles = JsonSerializer.Deserialize<List<ConnectionProfile>>(json, JsonOptions);
            return profiles ?? new List<ConnectionProfile>();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new List<ConnectionProfile>();
        }
    }

    public static void Save(List<ConnectionProfile> profiles)
    {
        Directory.CreateDirectory(FolderPath);
        var json = JsonSerializer.Serialize(profiles, JsonOptions);
        File.WriteAllText(FilePath, json);
    }
}
