using ProductsImport.Data;
using ProductsImport.Forms;
using ProductsImport.Localization;

namespace ProductsImport;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Read before any Form is constructed: forms read Strings.Current once, in field initializers.
        Strings.Current = AppSettingsStore.Load().Language;

        Application.Run(new MainForm());
    }
}
