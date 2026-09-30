using ProductsImport.Localization;
using ProductsImport.Models;
using ProductsImport.Services;

namespace ProductsImport.Forms;

/// <summary>Shows the outcome of the import and offers to export failed rows, or a copy of the original
/// document with generated barcodes filled in, back to Excel.</summary>
public class ImportResultForm : Form
{
    private readonly Label _lblSummary = new() { Left = 15, Top = 15, Width = 450, Height = 90 };
    private readonly DataGridView _grid = new()
    {
        Left = 15,
        Top = 110,
        Width = 450,
        Height = 195,
        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        AllowUserToAddRows = false,
        ReadOnly = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false
    };

    private readonly Button _btnExportGenerated = new() { Width = 320, Text = Strings.T("Result_BtnExportGenerated") };
    private readonly Button _btnExportErrors = new() { Width = 320, Text = Strings.T("Result_BtnExport") };
    private readonly Button _btnClose = new() { Width = 90, Text = Strings.T("Common_Close"), DialogResult = DialogResult.OK };

    private readonly ImportSummary _summary;
    private readonly string[]? _headerRow;
    private readonly List<ImportRow> _rows;
    private readonly List<string[]>? _originalRows;
    private readonly int? _barcodeColumnIndex;

    public ImportResultForm(ImportSummary summary, string[]? headerRow, List<ImportRow> rows,
        List<string[]>? originalRows, int? barcodeColumnIndex)
    {
        _summary = summary;
        _headerRow = headerRow;
        _rows = rows;
        _originalRows = originalRows;
        _barcodeColumnIndex = barcodeColumnIndex;

        Text = Strings.T("Result_Title");
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ClientSize = new Size(480, 395);
        MinimumSize = new Size(380, 340);

        _lblSummary.Text = Strings.T("Result_Summary", summary.TotalRows, summary.Imported, summary.Skipped, summary.Errors.Count);

        Controls.Add(_lblSummary);
        Controls.Add(_grid);
        Controls.Add(_btnExportGenerated);
        Controls.Add(_btnExportErrors);
        Controls.Add(_btnClose);

        _btnExportGenerated.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _btnExportErrors.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnExportGenerated.Left = 15;
        _btnExportGenerated.Top = 320;
        _btnExportErrors.Left = 15;
        _btnExportErrors.Top = 355;
        _btnClose.Left = ClientSize.Width - 105;
        _btnClose.Top = 355;

        _grid.Columns.Add("colRow", Strings.T("Common_Row"));
        _grid.Columns.Add("colMessage", Strings.T("Common_Error"));
        foreach (var error in summary.Errors)
        {
            _grid.Rows.Add(error.SourceRowNumber, error.Message);
        }

        _btnExportGenerated.Enabled = _originalRows != null && _barcodeColumnIndex.HasValue && rows.Any(r => r.BarcodeWasGenerated);
        _btnExportGenerated.Click += (_, _) => ExportGeneratedBarcodes();

        _btnExportErrors.Enabled = summary.Errors.Count > 0;
        _btnExportErrors.Click += (_, _) => ExportErrors();

        AcceptButton = _btnClose;
    }

    private void ExportGeneratedBarcodes()
    {
        if (_originalRows == null || _barcodeColumnIndex is null)
        {
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = Strings.T("Result_SaveDialogFilter"),
            FileName = "generated_barcodes.xlsx"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            GeneratedBarcodeReportService.WriteDocument(dialog.FileName, _originalRows, _barcodeColumnIndex.Value, _rows);
            MessageBox.Show(this, Strings.T("Result_Msg_GeneratedSaved"), Strings.T("Common_Done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, Strings.T("Result_Msg_SaveFailed", ex.Message), Strings.T("Common_Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
