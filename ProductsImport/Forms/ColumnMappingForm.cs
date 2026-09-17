using ProductsImport.Data;
using ProductsImport.Models;

namespace ProductsImport.Forms;

/// <summary>
/// Lets the user map each spreadsheet column to a database target field, and save/reuse that
/// mapping as a named profile for documents that share the same column layout.
/// </summary>
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

    private readonly Label _lblProfile = new() { Left = 12, Top = 16, Width = 60, Text = "Профиль:" };
    private readonly ComboBox _cmbProfile = new() { Left = 76, Top = 12, Width = 230, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _btnSaveProfile = new() { Left = 312, Top = 11, Width = 190, Text = "Сохранить как профиль..." };
    private readonly Button _btnDeleteProfile = new() { Left = 508, Top = 11, Width = 140, Text = "Удалить профиль" };

    private readonly DataGridView _grid = new()
    {
        Left = 12,
        Top = 45,
        Width = 660,
        Height = 345,
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
        Top = 398,
        Width = 660,
        Height = 40,
        Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        Text = "Наименование и штрих-код являются основными колонками. Остальные характеристики можно " +
               "не сопоставлять — для них будет предложено указать значение по умолчанию на следующем шаге."
    };

    private readonly Button _btnOk = new() { Width = 90, Text = "Далее", DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Width = 90, Text = "Отмена", DialogResult = DialogResult.Cancel };

    private List<ImportProfile> _profiles;

    public ColumnMapping Mapping { get; } = new();

    public ColumnMappingForm(string[] headerRow, List<string[]> sampleRows, int columnCount)
    {
        Text = "Сопоставление колонок";
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ClientSize = new Size(684, 495);
        MinimumSize = new Size(660, 400);

        _btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnOk.Top = 453;
        _btnCancel.Top = 453;
        _btnOk.Left = ClientSize.Width - 200;
        _btnCancel.Left = ClientSize.Width - 100;

        Controls.Add(_lblProfile);
        Controls.Add(_cmbProfile);
        Controls.Add(_btnSaveProfile);
        Controls.Add(_btnDeleteProfile);
        Controls.Add(_grid);
        Controls.Add(_lblHint);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        BuildGrid(headerRow, sampleRows, columnCount);

        _profiles = ImportProfileStore.Load();
        RefreshProfileList(null);

        _cmbProfile.SelectedIndexChanged += (_, _) => ApplySelectedProfile();
        _btnSaveProfile.Click += (_, _) => SaveCurrentAsProfile();
        _btnDeleteProfile.Click += (_, _) => DeleteSelectedProfile();

        _btnOk.Click += (_, e) =>
        {
            if (!TryBuildMapping())
            {
                DialogResult = DialogResult.None;
            }
        };
    }

    private void BuildGrid(string[] headerRow, List<string[]> sampleRows, int columnCount)
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

            _grid.Rows.Add(ExcelColumnName(c), header, sample, TargetField.None);
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

    private void RefreshProfileList(string? selectName)
    {
        _cmbProfile.Items.Clear();
        _cmbProfile.Items.Add("— не выбран —");
        foreach (var profile in _profiles.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            _cmbProfile.Items.Add(profile);
        }

        var match = selectName == null
            ? null
            : _cmbProfile.Items.Cast<object>().FirstOrDefault(i => i is ImportProfile p && p.Name == selectName);
        _cmbProfile.SelectedItem = match ?? _cmbProfile.Items[0];
    }

    private void ApplySelectedProfile()
    {
        if (_cmbProfile.SelectedItem is not ImportProfile profile)
        {
            return;
        }

        for (var r = 0; r < _grid.Rows.Count; r++)
        {
            var field = profile.Columns.TryGetValue(r, out var mapped) ? mapped : TargetField.None;
            _grid.Rows[r].Cells["colTarget"].Value = field;
        }
    }

    private void SaveCurrentAsProfile()
    {
        _grid.EndEdit();

        var columns = new Dictionary<int, TargetField>();
        for (var r = 0; r < _grid.Rows.Count; r++)
        {
            var value = _grid.Rows[r].Cells["colTarget"].Value;
            var field = value is TargetField tf ? tf : TargetField.None;
            if (field != TargetField.None)
            {
                columns[r] = field;
            }
        }

        if (columns.Count == 0)
        {
            MessageBox.Show(this, "Сначала сопоставьте хотя бы одну колонку.", "Проверка",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var currentName = (_cmbProfile.SelectedItem as ImportProfile)?.Name ?? string.Empty;
        using var prompt = new TextPromptForm("Сохранить профиль", "Название профиля:", currentName);
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var name = prompt.Value;
        var existing = _profiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase));
        if (existing != null)
        {
            var overwrite = MessageBox.Show(this, $"Профиль \"{name}\" уже существует. Заменить его?",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (overwrite != DialogResult.Yes)
            {
                return;
            }

            existing.Columns = columns;
        }
        else
        {
            _profiles.Add(new ImportProfile { Name = name, Columns = columns });
        }

        ImportProfileStore.Save(_profiles);
        RefreshProfileList(name);
        MessageBox.Show(this, "Профиль сохранён.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void DeleteSelectedProfile()
    {
        if (_cmbProfile.SelectedItem is not ImportProfile profile)
        {
            MessageBox.Show(this, "Выберите профиль из списка.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show(this, $"Удалить профиль \"{profile.Name}\"?", "Подтверждение",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _profiles.Remove(profile);
        ImportProfileStore.Save(_profiles);
        RefreshProfileList(null);
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
