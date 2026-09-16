using ProductsImport.Services;

namespace ProductsImport.Forms;

/// <summary>Shows the outcome of the import and offers to export failed rows back to Excel.</summary>
public class ImportResultForm : Form
{
    private readonly Label _lblSummary = new() { Left = 15, Top = 15, Width = 420, Height = 90 };
    private readonly DataGridView _grid = new()
    {
        Left = 15,
        Top = 110,
        Width = 420,
        Height = 200,
        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        AllowUserToAddRows = false,
        ReadOnly = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false
    };

    private readonly Button _btnExport = new() { Width = 200, Text = "Сохранить ошибки в Excel..." };
    private readonly Button _btnClose = new() { Width = 90, Text = "Закрыть", DialogResult = DialogResult.OK };

    private readonly ImportSummary _summary;
    private readonly string[]? _headerRow;

    public ImportResultForm(ImportSummary summary, string[]? headerRow)
    {
        _summary = summary;
        _headerRow = headerRow;

        Text = "Результат импорта";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ClientSize = new Size(450, 355);
        MinimumSize = new Size(350, 300);

        _lblSummary.Text = $"Всего строк: {summary.TotalRows}\r\n" +
                            $"Успешно импортировано: {summary.Imported}\r\n" +
                            $"Пропущено пользователем: {summary.Skipped}\r\n" +
                            $"Не импортировано из-за ошибок: {summary.Errors.Count}";

        Controls.Add(_lblSummary);
        Controls.Add(_grid);
        Controls.Add(_btnExport);
        Controls.Add(_btnClose);

        _btnExport.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnExport.Left = 15;
        _btnExport.Top = 320;
        _btnClose.Left = ClientSize.Width - 105;
        _btnClose.Top = 320;

        _grid.Columns.Add("colRow", "Строка");
        _grid.Columns.Add("colMessage", "Ошибка");
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
            Filter = "Книга Excel (*.xlsx)|*.xlsx",
            FileName = "import_errors.xlsx"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            ErrorReportService.WriteErrors(dialog.FileName, _headerRow, _summary.Errors);
            MessageBox.Show(this, "Файл с ошибками сохранён.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Не удалось сохранить файл: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
