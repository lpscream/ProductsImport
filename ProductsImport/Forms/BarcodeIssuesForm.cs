using ProductsImport.Models;
using ProductsImport.Services;

namespace ProductsImport.Forms;

/// <summary>
/// Lets the user decide, for every row whose barcode is missing, invalid or a duplicate, whether to
/// generate a unique internal EAN-13, skip the row entirely, or type a barcode in manually.
/// </summary>
public class BarcodeIssuesForm : Form
{
    private static readonly string[] ActionLabels = { "Сгенерировать штрих-код", "Не импортировать", "Ввести вручную" };

    private readonly DataGridView _grid = new()
    {
        Left = 12,
        Top = 45,
        Width = 700,
        Height = 380,
        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        EditMode = DataGridViewEditMode.EditOnEnter
    };

    private readonly ComboBox _cmbApplyAll = new() { Left = 200, Top = 12, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _btnApplyAll = new() { Left = 430, Top = 11, Width = 150, Text = "Применить ко всем" };

    private readonly Button _btnOk = new() { Width = 90, Text = "Далее", DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Width = 90, Text = "Отмена", DialogResult = DialogResult.Cancel };

    private readonly List<ImportRow> _rows;
    private readonly ImportOrchestrator _orchestrator;
    private readonly Dictionary<ImportRow, string> _manualValues = new();

    public BarcodeIssuesForm(List<ImportRow> rows, ImportOrchestrator orchestrator)
    {
        _rows = rows;
        _orchestrator = orchestrator;

        Text = $"Требуется решение по штрих-коду ({rows.Count})";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ClientSize = new Size(724, 480);
        MinimumSize = new Size(500, 350);

        Controls.Add(new Label { Left = 12, Top = 16, Width = 180, Text = "Для выделенных строк:" });
        Controls.Add(_cmbApplyAll);
        Controls.Add(_btnApplyAll);
        Controls.Add(_grid);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);

        _btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnOk.Top = 445;
        _btnCancel.Top = 445;
        _btnOk.Left = ClientSize.Width - 200;
        _btnCancel.Left = ClientSize.Width - 100;
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        _cmbApplyAll.Items.AddRange(ActionLabels);
        _cmbApplyAll.SelectedIndex = 0;
        _btnApplyAll.Click += (_, _) => ApplyToAllRows();

        BuildGrid();

        _grid.CellContentClick += Grid_CellContentClick;

        _btnOk.Click += (_, e) =>
        {
            if (!TryApplyAll())
            {
                DialogResult = DialogResult.None;
            }
        };
    }

    private void BuildGrid()
    {
        _grid.Columns.Add("colRow", "Строка");
        _grid.Columns.Add("colName", "Наименование");
        _grid.Columns.Add("colRaw", "Штрих-код в документе");

        var actionColumn = new DataGridViewComboBoxColumn
        {
            Name = "colAction",
            HeaderText = "Действие",
            DataSource = ActionLabels.ToList(),
            FlatStyle = FlatStyle.Flat
        };
        _grid.Columns.Add(actionColumn);

        _grid.Columns.Add("colManual", "Штрих-код вручную");
        _grid.Columns["colManual"].ReadOnly = true;

        var buttonColumn = new DataGridViewButtonColumn { Name = "colPick", HeaderText = "", Text = "...", UseColumnTextForButtonValue = true };
        _grid.Columns.Add(buttonColumn);

        foreach (var row in _rows)
        {
            _grid.Rows.Add(row.SourceRowNumber, row.Name ?? "(без наименования)", row.RawBarcode ?? "(нет)", ActionLabels[0], string.Empty, "...");
        }

        _grid.Columns["colRow"].ReadOnly = true;
        _grid.Columns["colName"].ReadOnly = true;
        _grid.Columns["colRaw"].ReadOnly = true;
    }

    private void Grid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Columns[e.ColumnIndex].Name != "colPick")
        {
            return;
        }

        var row = _rows[e.RowIndex];
        _manualValues.TryGetValue(row, out var current);

        using var dialog = new ManualBarcodeForm(row.Name ?? string.Empty, current);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _manualValues[row] = dialog.Barcode;
            _grid.Rows[e.RowIndex].Cells["colManual"].Value = dialog.Barcode;
            _grid.Rows[e.RowIndex].Cells["colAction"].Value = ActionLabels[2];
        }
    }

    private void ApplyToAllRows()
    {
        var label = (string)_cmbApplyAll.SelectedItem!;
        foreach (DataGridViewRow row in _grid.Rows)
        {
            row.Cells["colAction"].Value = label;
        }
    }

    private bool TryApplyAll()
    {
        _grid.EndEdit();

        for (var i = 0; i < _rows.Count; i++)
        {
            var row = _rows[i];
            var actionLabel = (string?)_grid.Rows[i].Cells["colAction"].Value ?? ActionLabels[0];
            var action = actionLabel switch
            {
                _ when actionLabel == ActionLabels[0] => BarcodeAction.Generate,
                _ when actionLabel == ActionLabels[1] => BarcodeAction.Skip,
                _ => BarcodeAction.Manual
            };

            _manualValues.TryGetValue(row, out var manualValue);

            if (action == BarcodeAction.Manual && string.IsNullOrWhiteSpace(manualValue))
            {
                MessageBox.Show(this, $"Строка {row.SourceRowNumber}: не введён штрих-код. Нажмите \"...\", чтобы ввести его.",
                    "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!_orchestrator.ApplyBarcodeResolution(row, action, manualValue))
            {
                MessageBox.Show(this,
                    $"Строка {row.SourceRowNumber}: указанный штрих-код уже используется другим товаром. Введите другой.",
                    "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        return true;
    }
}
