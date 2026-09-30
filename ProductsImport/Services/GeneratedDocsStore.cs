namespace ProductsImport.Services;

/// <summary>
/// Where the "сгенерированные штрих-коды" exports are archived: a "generated_docs" folder next to the
/// executable, with a "full" and a "generated_only" subfolder, each pruned to a one-month retention
/// window on every import.
/// </summary>
public static class GeneratedDocsStore
{
    private const int RetentionMonths = 1;

    private static string RootFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "generated_docs");

    public static string FullDocumentsFolder => Path.Combine(RootFolder, "full");
    public static string GeneratedOnlyFolder => Path.Combine(RootFolder, "generated_only");

    /// <summary>Builds "&lt;source file name&gt;_&lt;date&gt;_&lt;time&gt;.xlsx" inside <paramref name="folder"/>,
    /// creating the folder if it doesn't exist yet.</summary>
    public static string BuildFilePath(string folder, string sourceFileName)
    {
        Directory.CreateDirectory(folder);

        var baseName = Path.GetFileNameWithoutExtension(sourceFileName);
        var now = DateTime.Now;
        var fileName = $"{baseName}_{now:yyyy-MM-dd}_{now:HH-mm-ss}.xlsx";
        return Path.Combine(folder, fileName);
    }

    /// <summary>Deletes files older than one month from both subfolders. Best-effort: a file that can't
    /// be deleted right now (e.g. open elsewhere) is left for the next cleanup instead of failing.</summary>
    public static void CleanupOldFiles()
    {
        var cutoff = DateTime.Now.AddMonths(-RetentionMonths);
        CleanupFolder(FullDocumentsFolder, cutoff);
        CleanupFolder(GeneratedOnlyFolder, cutoff);
    }

    private static void CleanupFolder(string folder, DateTime cutoff)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (var filePath in Directory.GetFiles(folder))
        {
            try
            {
                if (File.GetLastWriteTime(filePath) < cutoff)
                {
                    File.Delete(filePath);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
