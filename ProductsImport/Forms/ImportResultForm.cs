using ProductsImport.Localization;
using ProductsImport.Services;

namespace ProductsImport.Forms;

/// <summary>Shows the outcome of the import, offers to export failed rows back to Excel, and - when any
/// barcode was auto-generated this run - notes where the automatically archived copies were saved.</summary>
public class ImportResultForm : Form
{
    private readonly Label _lblSummary = new() { Left = 15, Top = 15, Width = 450, Height = 140 };
    private readonly DataGridView _grid = new()
    {
        Left = 15,
        Top = 160,
        Width = 450,
        Height = 180,
        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        AllowUserToAddRows = false,
        ReadOnly = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false
    };

    private readonly Button _btnExport = new() { Width = 200, Text = Strings.T("Result_BtnExport") };
    private readonly Button _btnClose = new() { Width = 90, Text = Strings.T("Common_Close"), DialogResult = DialogResult.OK };

    private readonly ImportSummary _summary;
    private readonly string[]? _headerRow;

    public ImportResultForm(ImportSummary summary, string[]? headerRow, (string FullPath, string GeneratedOnlyPath)? archivedPaths)
    {
        _summary = summary;
        _headerRow = headerRow;

        Text = Strings.T("Result_Title");
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ClientSize = new Size(480, 395);
        MinimumSize = new Size(380, 340);

        var summaryText = Strings.T("Result_Summary", summary.TotalRows, summary.Imported, summary.Skipped, summary.Errors.Count);
        if (archivedPaths is { } paths)
        {
            summaryText += Strings.T("Result_AutoSaveNote", paths.FullPath, paths.GeneratedOnlyPath);
        }

        _lblSummary.Text = summaryText;

        Controls.Add(_lblSummary);
        Controls.Add(_grid);
        Controls.Add(_btnExport);
        Controls.Add(_btnClose);

        _btnExport.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnExport.Left = 15;
        _btnExport.Top = 355;
        _btnClose.Left = ClientSize.Width - 105;
        _btnClose.Top = 355;

        _grid.Columns.Add("colRow", Strings.T("Common_Row"));
        _grid.Columns.Add("colMessage", Strings.T("Common_Error"));
        foreach (var error in summary.Errors)
        {
            _grid.Rows.Add(error.SourceRowNumber, error.Message);
        }

        _btnExport.Enabled = summary.Errors.Count > 0;
        _btnExport.Click += (_, _) => ExportErrors();

        AcceptButton = _btnClose;
    }

    private void ExportErrors()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = Strings.T("Result_SaveDialogFilter"),
            FileName = "import_errors.xlsx"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            ErrorReportService.WriteErrors(dialog.FileName, _headerRow, _summary.Errors);
            MessageBox.Show(this, Strings.T("Result_Msg_Saved"), Strings.T("Common_Done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, Strings.T("Result_Msg_SaveFailed", ex.Message), Strings.T("Common_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
