using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace ProductsImport.Data;

/// <summary>
/// Loads and saves a list of items as JSON under %AppData%\ProductsImport\&lt;fileName&gt;. Used for both
/// saved SQL Server connections and saved import (column-mapping) profiles. Uses
/// <see cref="DataContractJsonSerializer"/> (built into .NET Framework) instead of a NuGet JSON
/// package, to keep the deployed footprint small.
/// </summary>
internal static class JsonFileStore<T>
{
    private static string FolderPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ProductsImport");

    public static List<T> Load(string fileName)
    {
        var filePath = Path.Combine(FolderPath, fileName);
        try
        {
            if (!File.Exists(filePath))
            {
                return new List<T>();
            }

            using var stream = File.OpenRead(filePath);
            var serializer = new DataContractJsonSerializer(typeof(List<T>));
            return serializer.ReadObject(stream) as List<T> ?? new List<T>();
        }
        catch (Exception ex) when (ex is IOException or SerializationException or UnauthorizedAccessException)
        {
            return new List<T>();
        }
    }

    public static void Save(string fileName, List<T> items)
    {
        Directory.CreateDirectory(FolderPath);
        var filePath = Path.Combine(FolderPath, fileName);
        using var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(List<T>));
        serializer.WriteObject(stream, items);
        File.WriteAllBytes(filePath, stream.ToArray());
    }
}
