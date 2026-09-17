using Microsoft.Data.SqlClient;
using ProductsImport.Data;
using ProductsImport.Models;
using ProductsImport.Services;

namespace ProductsImport.Forms;

public class MainForm : Form
{
    private readonly Button _btnConnection = new() { Left = 12, Top = 12, Width = 200, Text = "Подключение к базе данных..." };
    private readonly Label _lblConnection = new() { Left = 220, Top = 17, Width = 400, Text = "Подключение не выбрано" };

    private readonly Button _btnOpenFile = new() { Left = 12, Top = 45, Width = 200, Text = "Открыть документ..." };
    private readonly Label _lblFile = new() { Left = 220, Top = 50, Width = 400, Text = "Файл не открыт" };

    private readonly Label _lblSheet = new() { Left = 12, Top = 82, Width = 80, Text = "Лист:" };
    private readonly ComboBox _cmbSheets = new() { Left = 95, Top = 78, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

    private readonly Label _lblHeaderRow = new() { Left = 310, Top = 82, Width = 150, Text = "Строка с заголовками:" };
    private readonly NumericUpDown _numHeaderRow = new() { Left = 460, Top = 78, Width = 60, Minimum = 1, Maximum = 1, Value = 1 };

    private readonly Label _lblImportProfile = new() { Left = 12, Top = 116, Width = 90, Text = "Профиль импорта:" };
    private readonly ComboBox _cmbImportProfile = new() { Left = 105, Top = 112, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _btnSaveImportProfile = new() { Left = 333, Top = 111, Width = 190, Text = "Сохранить как профиль..." };
    private readonly Button _btnDeleteImportProfile = new() { Left = 529, Top = 111, Width = 140, Text = "Удалить профиль" };

    private readonly DataGridView _grid = new()
    {
        Left = 12,
        Top = 148,
        Width = 760,
        Height = 350,
        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        AllowUserToAddRows = false,
        ReadOnly = true,
        RowHeadersVisible = false
    };

    private readonly Button _btnStartImport = new()
    {
        Width = 220,
        Height = 32,
        Text = "Начать импорт...",
        Anchor = AnchorStyles.Bottom | AnchorStyles.Left
    };

    private ConnectionProfile? _activeProfile;
    private SpreadsheetDocument? _document;
    private List<ImportProfile> _importProfiles;

    public MainForm()
    {
        Text = "Импорт товаров в справочник";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(784, 536);
        MinimumSize = new Size(650, 436);

        Controls.Add(_btnConnection);
        Controls.Add(_lblConnection);
        Controls.Add(_btnOpenFile);
        Controls.Add(_lblFile);
        Controls.Add(_lblSheet);
        Controls.Add(_cmbSheets);
        Controls.Add(_lblHeaderRow);
        Controls.Add(_numHeaderRow);
        Controls.Add(_lblImportProfile);
        Controls.Add(_cmbImportProfile);
        Controls.Add(_btnSaveImportProfile);
        Controls.Add(_btnDeleteImportProfile);
        Controls.Add(_grid);
        Controls.Add(_btnStartImport);

        _btnStartImport.Left = 12;
        _btnStartImport.Top = 501;
        _btnStartImport.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        _importProfiles = ImportProfileStore.Load();
        RefreshImportProfileList(null);
        ApplyLastUsedSettings();

        _btnConnection.Click += (_, _) => ChooseConnection();
        _btnOpenFile.Click += (_, _) => OpenDocument();
        _cmbSheets.SelectedIndexChanged += (_, _) => LoadSheetIntoGrid();
        _cmbImportProfile.SelectedIndexChanged += (_, _) =>
        {
            ApplyHeaderRowFromProfile();
            PersistLastUsedSettings();
        };
        _btnSaveImportProfile.Click += (_, _) => SaveCurrentAsImportProfile();
        _btnDeleteImportProfile.Click += (_, _) => DeleteSelectedImportProfile();
        _btnStartImport.Click += async (_, _) => await StartImportAsync();
    }

    private void ChooseConnection()
    {
        using var form = new ConnectionSettingsForm(_activeProfile);
        if (form.ShowDialog(this) == DialogResult.OK && form.SelectedProfile != null)
        {
            _activeProfile = form.SelectedProfile;
            _lblConnection.Text = _activeProfile.ToString();
            PersistLastUsedSettings();
        }
    }

    private void ApplyLastUsedSettings()
    {
        var settings = AppSettingsStore.Load();

        if (settings.LastConnectionName != null)
        {
            var match = ConnectionProfileStore.Load()
                .FirstOrDefault(p => p.Name == settings.LastConnectionName && p.Server == settings.LastConnectionServer);
            if (match != null)
            {
                _activeProfile = match;
                _lblConnection.Text = match.ToString();
            }
        }

        if (settings.LastImportProfileName != null)
        {
            var match = _cmbImportProfile.Items.Cast<object>()
                .FirstOrDefault(i => i is ImportProfile p && p.Name == settings.LastImportProfileName);
            if (match != null)
            {
                _cmbImportProfile.SelectedItem = match;
            }
        }
    }

    private void PersistLastUsedSettings()
    {
        AppSettingsStore.Save(new AppSettings
        {
            LastConnectionName = _activeProfile?.Name,
            LastConnectionServer = _activeProfile?.Server,
            LastImportProfileName = (_cmbImportProfile.SelectedItem as ImportProfile)?.Name
        });
    }

    private void OpenDocument()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Документы Excel/CSV (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|Все файлы (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            _document = SpreadsheetReader.Load(dialog.FileName);
            _lblFile.Text = Path.GetFileName(dialog.FileName);

            _cmbSheets.Items.Clear();
            _cmbSheets.Items.AddRange(_document.SheetNames.Cast<object>().ToArray());
            if (_cmbSheets.Items.Count > 0)
            {
                _cmbSheets.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Не удалось открыть файл: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _document = null;
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void LoadSheetIntoGrid()
    {
        if (_document == null || _cmbSheets.SelectedItem is not string sheetName)
        {
            return;
        }

        var rows = _document.GetRows(sheetName);

        _numHeaderRow.Maximum = Math.Max(rows.Count, 1);
        ApplyHeaderRowFromProfile();

        _grid.Columns.Clear();
        _grid.Rows.Clear();

        var columnCount = rows.Count == 0 ? 0 : rows.Max(r => r.Length);
        for (var c = 0; c < columnCount; c++)
        {
            _grid.Columns.Add($"c{c}", ExcelColumnName(c));
        }

        foreach (var row in rows.Take(500))
        {
            var padded = new object[columnCount];
            for (var c = 0; c < columnCount; c++)
            {
                padded[c] = c < row.Length ? row[c] : string.Empty;
            }

            _grid.Rows.Add(padded);
        }
    }

    /// <summary>Applies the selected import profile's header row number (clamped to what the
    /// currently loaded sheet allows), or resets to 1 when no profile is selected.</summary>
    private void ApplyHeaderRowFromProfile()
    {
        var headerRowNumber = _cmbImportProfile.SelectedItem is ImportProfile profile ? profile.HeaderRowNumber : 1;
        _numHeaderRow.Value = Math.Min(Math.Max(headerRowNumber, (int)_numHeaderRow.Minimum), (int)_numHeaderRow.Maximum);
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

    private void RefreshImportProfileList(string? selectName)
    {
        _cmbImportProfile.Items.Clear();
        _cmbImportProfile.Items.Add("— не выбран —");
        foreach (var profile in _importProfiles.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            _cmbImportProfile.Items.Add(profile);
        }

        var match = selectName == null
            ? null
            : _cmbImportProfile.Items.Cast<object>().FirstOrDefault(i => i is ImportProfile p && p.Name == selectName);
        _cmbImportProfile.SelectedItem = match ?? _cmbImportProfile.Items[0];
    }

    /// <summary>Reads the header row and data rows for the open document at the currently chosen
    /// sheet/header-row, showing a validation message and returning null if that isn't possible yet.</summary>
    private (string[] HeaderRow, List<string[]> DataRows, int ColumnCount)? TryGetDocumentLayout()
    {
        if (_document == null || _cmbSheets.SelectedItem is not string sheetName)
        {
            MessageBox.Show(this, "Сначала откройте документ с товарами.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var allRows = _document.GetRows(sheetName);
        var headerRowIndex = (int)_numHeaderRow.Value - 1;
        if (headerRowIndex < 0 || headerRowIndex >= allRows.Count)
        {
            MessageBox.Show(this, "Некорректно указана строка с заголовками.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var headerRow = allRows[headerRowIndex];
        var dataRows = allRows.Skip(headerRowIndex + 1).ToList();
        if (dataRows.Count == 0)
        {
            MessageBox.Show(this, "После указанной строки заголовков нет данных.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var columnCount = Math.Max(headerRow.Length, dataRows.Max(r => r.Length));
        return (headerRow, dataRows, columnCount);
    }

    private void SaveCurrentAsImportProfile()
    {
        var layout = TryGetDocumentLayout();
        if (layout is null)
        {
            return;
        }

        var (headerRow, dataRows, columnCount) = layout.Value;
        var sampleRows = dataRows.Take(5).ToList();
        var initialMapping = (_cmbImportProfile.SelectedItem as ImportProfile)?.Columns;

        using var mappingForm = new ColumnMappingForm(headerRow, sampleRows, columnCount, initialMapping);
        if (mappingForm.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var currentName = (_cmbImportProfile.SelectedItem as ImportProfile)?.Name ?? string.Empty;
        using var prompt = new TextPromptForm("Сохранить профиль", "Название профиля импорта:", currentName);
        if (prompt.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var name = prompt.Value;
        var existing = _importProfiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.CurrentCultureIgnoreCase));
        if (existing != null)
        {
            var overwrite = MessageBox.Show(this, $"Профиль \"{name}\" уже существует. Заменить его?",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (overwrite != DialogResult.Yes)
            {
                return;
            }
        }

        var profile = existing ?? new ImportProfile { Name = name };
        profile.HeaderRowNumber = (int)_numHeaderRow.Value;
        profile.Columns = mappingForm.Mapping.ToDictionary();
        if (existing == null)
        {
            _importProfiles.Add(profile);
        }

        ImportProfileStore.Save(_importProfiles);
        RefreshImportProfileList(name);
        MessageBox.Show(this, "Профиль сохранён.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void DeleteSelectedImportProfile()
    {
        if (_cmbImportProfile.SelectedItem is not ImportProfile profile)
        {
            MessageBox.Show(this, "Выберите профиль из списка.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show(this, $"Удалить профиль \"{profile.Name}\"?", "Подтверждение",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _importProfiles.Remove(profile);
        ImportProfileStore.Save(_importProfiles);
        RefreshImportProfileList(null);
    }

    private async Task StartImportAsync()
    {
        if (_activeProfile == null)
        {
            MessageBox.Show(this, "Сначала выберите подключение к базе данных.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var layout = TryGetDocumentLayout();
        if (layout is null)
        {
            return;
        }

        var (headerRow, dataRows, columnCount) = layout.Value;
        var headerRowIndex = (int)_numHeaderRow.Value - 1;

        SqlConnection? connection = null;
        try
        {
            SetBusy(true);

            var repository = new SqlServerRepository(_activeProfile);
            connection = await Task.Run(() => repository.OpenConnection());

            var groups = await Task.Run(() => repository.GetGroups(connection));
            var units = await Task.Run(() => repository.GetUnits(connection));
            var vatRates = await Task.Run(() => repository.GetVatRates(connection));
            var existingArticleIds = await Task.Run(() => repository.GetExistingArticleIds(connection));
            var existingBarcodes = await Task.Run(() => repository.GetExistingBarcodes(connection));

            var orchestrator = new ImportOrchestrator(groups, units, vatRates, existingArticleIds, existingBarcodes);
            var rows = orchestrator.BuildRows(dataRows, headerRowIndex + 2);
            if (rows.Count == 0)
            {
                MessageBox.Show(this, "Не найдено ни одной строки с данными.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var sampleRows = dataRows.Take(5).ToList();
            var initialMapping = (_cmbImportProfile.SelectedItem as ImportProfile)?.Columns;
            using var mappingForm = new ColumnMappingForm(headerRow, sampleRows, columnCount, initialMapping);
            if (mappingForm.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var mapping = mappingForm.Mapping;
            var needs = orchestrator.Analyze(rows, mapping);

            GroupInfo? defaultGroup = null;
            UnitInfo? defaultUnit = null;
            VatInfo? defaultVat = null;
            var defaultWeighted = false;
            var defaultExcise = false;

            if (needs.NeedsGroupDefault || needs.NeedsUnitDefault || needs.NeedsVatDefault ||
                needs.NeedsWeightedDefault || needs.NeedsExciseDefault)
            {
                using var defaultsForm = new DefaultsForm(needs, groups, units, vatRates);
                if (defaultsForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                defaultGroup = defaultsForm.SelectedGroup;
                defaultUnit = defaultsForm.SelectedUnit;
                defaultVat = defaultsForm.SelectedVat;
                defaultWeighted = defaultsForm.DefaultWeighted;
                defaultExcise = defaultsForm.DefaultExcise;
            }

            orchestrator.Resolve(rows, mapping, defaultGroup, defaultUnit, defaultVat, defaultWeighted, defaultExcise);

            var barcodeIssues = rows.Where(r => r.BarcodeNeedsResolution).ToList();
            if (barcodeIssues.Count > 0)
            {
                using var barcodeForm = new BarcodeIssuesForm(barcodeIssues, orchestrator);
                if (barcodeForm.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }
            }

            var summary = await Task.Run(() => orchestrator.Import(repository, connection, rows));

            using var resultForm = new ImportResultForm(summary, headerRow);
            resultForm.ShowDialog(this);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Ошибка импорта: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            connection?.Dispose();
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        _btnStartImport.Enabled = !busy;
        _btnConnection.Enabled = !busy;
        _btnOpenFile.Enabled = !busy;
        _cmbImportProfile.Enabled = !busy;
        _btnSaveImportProfile.Enabled = !busy;
        _btnDeleteImportProfile.Enabled = !busy;
    }
}
