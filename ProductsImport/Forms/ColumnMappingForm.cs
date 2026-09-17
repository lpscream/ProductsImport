using ProductsImport.Models;

namespace ProductsImport.Forms;

/// <summary>Lets the user map each spreadsheet column to a database target field.</summary>
public class ColumnMappingForm : Form
{
    private sealed record FieldOption(TargetField Field, string Label);

    private static readonly FieldOption[] FieldOptions =
    {
        new(TargetField.None, "— не использовать —"),
        new(TargetField.Name, "Наименование (обязательно)"),
        new(TargetField.Barcode, "Штрих-код"),
        new(TargetField.Group, "Группа"),
        new(TargetField.Article, "Артикул"),
        new(TargetField.Weighted, "Весовой (да/нет)"),
        new(TargetField.Unit, "Единица измерения"),
        new(TargetField.Vat, "НДС, %"),
        new(TargetField.Excise, "Подакцизный (да/нет)"),
        new(TargetField.Uktzed, "УКТЗЕД")
    };

    private readonly DataGridView _grid = new()
    {
        Left = 12,
        Top = 12,
        Width = 660,
        Height = 380,
        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.CellSelect
    };

    private readonly Label _lblHint = new()
    {
        Left = 12,
        Top = 400,
        Width = 660,
        Height = 40,
        Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        Text = "Наименование и штрих-код являются основными колонками. Остальные характеристики можно " +
               "не сопоставлять — для них будет предложено указать значение по умолчанию на следующем шаге."
    };

    private readonly Button _btnOk = new() { Width = 90, Text = "Далее", DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Width = 90, Text = "Отмена", DialogResult = DialogResult.Cancel };

    public ColumnMapping Mapping { get; } = new();

    /// <param name="initialMapping">
    /// Pre-fills the grid, e.g. from a saved <see cref="ImportProfile"/>. Column indices missing
    /// from it are left as "не использовать".
    /// </param>
    public ColumnMappingForm(string[] headerRow, List<string[]> sampleRows, int columnCount,
        Dictionary<int, TargetField>? initialMapping = null)
    {
        Text = "Сопоставление колонок";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ClientSize = new Size(684, 480);
        MinimumSize = new Size(500, 350);

        _btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnOk.Top = 445;
        _btnCancel.Top = 445;
        _btnOk.Left = ClientSize.Width - 200;
        _btnCancel.Left = ClientSize.Width - 100;

        Controls.Add(_grid);
        Controls.Add(_lblHint);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        BuildGrid(headerRow, sampleRows, columnCount, initialMapping);

        _btnOk.Click += (_, e) =>
        {
            if (!TryBuildMapping())
            {
                DialogResult = DialogResult.None;
            }
        };
    }

    private void BuildGrid(string[] headerRow, List<string[]> sampleRows, int columnCount,
        Dictionary<int, TargetField>? initialMapping)
    {
        _grid.Columns.Add("colIndex", "Колонка");
        _grid.Columns.Add("colHeader", "Заголовок в документе");
        _grid.Columns.Add("colSample", "Пример значения");

        var targetColumn = new DataGridViewComboBoxColumn
        {
            Name = "colTarget",
            HeaderText = "Назначение",
            DataSource = FieldOptions,
            DisplayMember = "Label",
            ValueMember = "Field",
            FlatStyle = FlatStyle.Flat
        };
        _grid.Columns.Add(targetColumn);

        for (var c = 0; c < columnCount; c++)
        {
            var header = c < headerRow.Length ? headerRow[c] : string.Empty;
            var sample = sampleRows.Select(r => c < r.Length ? r[c] : string.Empty)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
            var field = initialMapping != null && initialMapping.TryGetValue(c, out var mapped) ? mapped : TargetField.None;

            _grid.Rows.Add(ExcelColumnName(c), header, sample, field);
        }
    }

    private static string ExcelColumnName(int columnIndex)
    {
        var dividend = columnIndex + 1;
        var name = string.Empty;
        while (dividend > 0)
        {
            var modulo = (dividend - 1) % 26;
            name = Convert.ToChar('A' + modulo) + name;
            dividend = (dividend - modulo - 1) / 26;
        }

        return name;
    }

    private bool TryBuildMapping()
    {
        _grid.EndEdit();

        var used = new Dictionary<TargetField, int>();
        for (var r = 0; r < _grid.Rows.Count; r++)
        {
            var value = _grid.Rows[r].Cells["colTarget"].Value;
            var field = value is TargetField tf ? tf : TargetField.None;
            if (field == TargetField.None)
            {
                continue;
            }

            if (used.TryGetValue(field, out var firstRow))
            {
                MessageBox.Show(this,
                    $"Поле \"{FieldOptions.First(o => o.Field == field).Label}\" сопоставлено сразу нескольким колонкам " +
                    $"({ExcelColumnName(firstRow)} и {ExcelColumnName(r)}). Каждое поле можно сопоставить только один раз.",
                    "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            used[field] = r;
            Mapping.Set(r, field);
        }

        if (!used.ContainsKey(TargetField.Name))
        {
            MessageBox.Show(this, "Необходимо сопоставить колонку с наименованием товара.", "Проверка",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (!used.ContainsKey(TargetField.Barcode))
        {
            var proceed = MessageBox.Show(this,
                "Колонка со штрих-кодом не указана. Штрих-код нужно будет сгенерировать, ввести вручную " +
                "или пропустить для каждого товара на следующих шагах. Продолжить?",
                "Штрих-код не сопоставлен", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (proceed != DialogResult.Yes)
            {
                return false;
            }
        }

        return true;
    }
}
