using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ProductsImport.Data;

/// <summary>
/// Loads and saves JSON files under a "config" folder next to the executable, so every setting
/// (saved connections, import profiles, last-used state) travels with the installation instead of
/// the per-Windows-user %AppData% folder. Uses <see cref="DataContractJsonSerializer"/> (built into
/// .NET Framework) instead of a NuGet JSON package, to keep the deployed footprint small.
/// </summary>
internal static class JsonStore
{
    private static string FolderPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");

    public static List<T> LoadList<T>(string fileName) => Load<List<T>>(fileName) ?? new List<T>();

    public static void SaveList<T>(string fileName, List<T> items) => Save(fileName, items);

    public static T LoadObject<T>(string fileName) where T : class, new() => Load<T>(fileName) ?? new T();

    public static void SaveObject<T>(string fileName, T value) => Save(fileName, value);

    private static T? Load<T>(string fileName) where T : class
    {
        var filePath = Path.Combine(FolderPath, fileName);
        try
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            using var stream = File.OpenRead(filePath);
            var serializer = new DataContractJsonSerializer(typeof(T));
            return serializer.ReadObject(stream) as T;
        }
        catch (Exception ex) when (ex is IOException or SerializationException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static void Save<T>(string fileName, T value)
    {
        Directory.CreateDirectory(FolderPath);
        var filePath = Path.Combine(FolderPath, fileName);
        using var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(T));
        serializer.WriteObject(stream, value);
        File.WriteAllBytes(filePath, stream.ToArray());
    }
}
