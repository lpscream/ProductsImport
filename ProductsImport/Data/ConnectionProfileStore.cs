using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using ProductsImport.Models;

namespace ProductsImport.Data;

/// <summary>
/// Loads and saves the list of saved SQL Server connection profiles as JSON under %AppData%.
/// Uses <see cref="DataContractJsonSerializer"/> (built into .NET Framework) instead of a NuGet
/// JSON package, to keep the deployed footprint small.
/// </summary>
public static class ConnectionProfileStore
{
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

            using var stream = File.OpenRead(FilePath);
            var serializer = new DataContractJsonSerializer(typeof(List<ConnectionProfile>));
            return serializer.ReadObject(stream) as List<ConnectionProfile> ?? new List<ConnectionProfile>();
        }
        catch (Exception ex) when (ex is IOException or SerializationException or UnauthorizedAccessException)
        {
            return new List<ConnectionProfile>();
        }
    }

    public static void Save(List<ConnectionProfile> profiles)
    {
        Directory.CreateDirectory(FolderPath);
        using var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(List<ConnectionProfile>));
        serializer.WriteObject(stream, profiles);
        File.WriteAllBytes(FilePath, stream.ToArray());
    }
}
