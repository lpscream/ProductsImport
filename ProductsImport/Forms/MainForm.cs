using Microsoft.Data.SqlClient;
using ProductsImport.Data;
using ProductsImport.Models;
using ProductsImport.Services;

namespace ProductsImport.Forms;

/// <summary>
/// The application's single window: connect, open a document, map its columns, choose fallback
/// defaults, resolve barcode problems and run the import - all in one place instead of a chain of
/// modal dialogs, so every choice can be saved together as one import profile.
/// </summary>
public class MainForm : Form
{
    private sealed record FieldOption(TargetField Field, string Label);

    private static readonly FieldOption[] MappingFieldOptions =
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

    private static readonly string[] BarcodeActionLabels = { "Сгенерировать штрих-код", "Не импортировать", "Ввести вручную" };

    // --- Connection / settings ---
    private readonly Button _btnConnection = new() { Left = 12, Top = 12, Width = 200, Text = "Подключение к базе данных..." };
    private readonly Label _lblConnection = new() { Left = 220, Top = 17, Width = 540, Text = "Подключение не выбрано" };
    private readonly Button _btnSettings = new() { Left = 770, Top = 12, Width = 118, Text = "Настройки..." };

    // --- Document / sheet / header row ---
    private readonly Button _btnOpenFile = new() { Left = 12, Top = 45, Width = 200, Text = "Открыть документ..." };
    private readonly Label _lblFile = new() { Left = 220, Top = 50, Width = 650, Text = "Файл не открыт" };
    private readonly Label _lblSheet = new() { Left = 12, Top = 82, Width = 80, Text = "Лист:" };
    private readonly ComboBox _cmbSheets = new() { Left = 95, Top = 78, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _lblHeaderRow = new() { Left = 310, Top = 82, Width = 150, Text = "Строка с заголовками:" };
    private readonly NumericUpDown _numHeaderRow = new() { Left = 460, Top = 78, Width = 60, Minimum = 1, Maximum = 1, Value = 1 };

    // --- Import profile ---
    private readonly Label _lblImportProfile = new() { Left = 12, Top = 116, Width = 90, Text = "Профиль импорта:" };
    private readonly ComboBox _cmbImportProfile = new() { Left = 105, Top = 112, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _btnSaveImportProfile = new() { Left = 333, Top = 111, Width = 190, Text = "Сохранить как профиль..." };
    private readonly Button _btnDeleteImportProfile = new() { Left = 529, Top = 111, Width = 140, Text = "Удалить профиль" };

    // --- Document preview (click a row to set the header row) ---
    private readonly DataGridView _grid = new()
    {
        Left = 12,
        Top = 148,
        Width = 860,
        Height = 200,
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        AllowUserToAddRows = false,
        ReadOnly = true,
        RowHeadersVisible = false
    };

    // --- Column mapping section ---
    private readonly GroupBox _mappingGroupBox = new()
    {
        Left = 12,
        Width = 860,
        Height = 268,
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        Text = "Сопоставление колонок",
        Visible = false
    };

    private readonly DataGridView _mappingGrid = new()
    {
        Left = 10,
        Top = 20,
        Width = 838,
        Height = 195,
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.CellSelect
    };

    // --- Defaults section ---
    private readonly GroupBox _defaultsGroupBox = new()
    {
        Left = 12,
        Width = 860,
        Height = 180,
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        Text = "Значения по умолчанию",
        Visible = false
    };

    private readonly ComboBox _cmbDefaultGroup = new() { Left = 235, Top = 20, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _cmbDefaultUnit = new() { Left = 235, Top = 50, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _cmbDefaultVat = new() { Left = 235, Top = 80, Width = 300, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly RadioButton _rbWeightedYes = new() { Left = 235, Top = 111, Width = 60, Text = "Да", AutoSize = true };
    private readonly RadioButton _rbWeightedNo = new() { Left = 300, Top = 111, Width = 60, Text = "Нет", AutoSize = true, Checked = true };
    private readonly RadioButton _rbExciseYes = new() { Left = 235, Top = 141, Width = 60, Text = "Да", AutoSize = true };
    private readonly RadioButton _rbExciseNo = new() { Left = 300, Top = 141, Width = 60, Text = "Нет", AutoSize = true, Checked = true };

    // --- Barcode resolution section ---
    private readonly GroupBox _barcodeGroupBox = new()
    {
        Left = 12,
        Width = 860,
        Height = 300,
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        Text = "Требуется решение по штрих-коду",
        Visible = false
    };

    private readonly ComboBox _cmbBarcodeApplyAll = new() { Left = 165, Top = 20, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _btnBarcodeApplyAll = new() { Left = 395, Top = 19, Width = 160, Text = "Применить ко всем" };

    private readonly DataGridView _barcodeGrid = new()
    {
        Left = 10,
        Top = 52,
        Width = 838,
        Height = 230,
        Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        EditMode = DataGridViewEditMode.EditOnEnter
    };

    private readonly Dictionary<ImportRow, string> _barcodeManualValues = new();
    private List<ImportRow>? _barcodeIssueRows;

    private readonly Button _btnStartImport = new() { Width = 220, Height = 32, Text = "Начать импорт..." };

    private ConnectionProfile? _activeProfile;
    private SpreadsheetDocument? _document;
    private List<ImportProfile> _importProfiles;
    private bool _showBarcodeIssuesList = true;
    private bool _suppressMappingRefresh;

    private List<GroupInfo>? _groups;
    private List<UnitInfo>? _units;
    private List<VatInfo>? _vatRates;
    private HashSet<long>? _existingArticleIds;
    private HashSet<string>? _existingBarcodes;

    private List<ImportRow>? _pendingRows;
    private ImportOrchestrator? _pendingOrchestrator;
    private string[]? _pendingHeaderRow;

    public MainForm()
    {
        Text = "Импорт товаров в справочник";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(900, 700);
        MinimumSize = new Size(760, 500);
        AutoScroll = true;

        Controls.Add(_btnConnection);
        Controls.Add(_lblConnection);
        Controls.Add(_btnSettings);
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
        Controls.Add(_mappingGroupBox);
        Controls.Add(_defaultsGroupBox);
        Controls.Add(_barcodeGroupBox);
        Controls.Add(_btnStartImport);

        BuildMappingGroupBox();
        BuildDefaultsGroupBox();
        BuildBarcodeGroupBox();

        _importProfiles = ImportProfileStore.Load();
        RefreshImportProfileList(null);
        ApplyLastUsedSettings();
        RelayoutSections();

        _btnConnection.Click += async (_, _) => await ChooseConnectionAsync();
        _btnSettings.Click += (_, _) => OpenSettings();
        _btnOpenFile.Click += (_, _) => OpenDocument();
        _cmbSheets.SelectedIndexChanged += (_, _) => LoadSheetIntoGrid();
        _grid.CellClick += (_, e) =>
        {
            if (e.RowIndex >= 0)
            {
                SetHeaderRow(e.RowIndex + 1);
            }
        };
        _numHeaderRow.ValueChanged += (_, _) =>
        {
            if (!_suppressMappingRefresh)
            {
                RefreshMappingSection(useProfileMapping: false);
            }
        };
        _cmbImportProfile.SelectedIndexChanged += (_, _) =>
        {
            ApplyHeaderRowFromProfile();
            ApplyDefaultsFromProfile();
            RefreshMappingSection(useProfileMapping: true);
            PersistLastUsedSettings();
        };
        _btnSaveImportProfile.Click += (_, _) => SaveCurrentAsImportProfile();
        _btnDeleteImportProfile.Click += (_, _) => DeleteSelectedImportProfile();
        _btnStartImport.Click += async (_, _) => await StartImportAsync();

        Shown += async (_, _) => await OnShownAsync();
    }

    private async Task OnShownAsync()
    {
        if (_activeProfile != null)
        {
            await RefreshReferenceDataAsync();
        }
    }

    // ===================== Connection =====================

    private async Task ChooseConnectionAsync()
    {
        using var form = new ConnectionSettingsForm(_activeProfile);
        if (form.ShowDialog(this) == DialogResult.OK && form.SelectedProfile != null)
        {
            _activeProfile = form.SelectedProfile;
            _lblConnection.Text = _activeProfile.ToString();
            PersistLastUsedSettings();
            await RefreshReferenceDataAsync();
        }
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_showBarcodeIssuesList);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _showBarcodeIssuesList = form.ShowBarcodeIssuesList;
            PersistLastUsedSettings();
        }
    }

    private void ApplyLastUsedSettings()
    {
        var settings = AppSettingsStore.Load();
        _showBarcodeIssuesList = settings.ShowBarcodeIssuesList;

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
            LastImportProfileName = (_cmbImportProfile.SelectedItem as ImportProfile)?.Name,
            ShowBarcodeIssuesList = _showBarcodeIssuesList
        });
    }

    private async Task RefreshReferenceDataAsync()
    {
        if (_activeProfile == null)
        {
            return;
        }

        try
        {
            SetBusy(true);

            var repository = new SqlServerRepository(_activeProfile);
            using var connection = await Task.Run(() => repository.OpenConnection());

            _groups = await Task.Run(() => repository.GetGroups(connection));
            _units = await Task.Run(() => repository.GetUnits(connection));
            _vatRates = await Task.Run(() => repository.GetVatRates(connection));
            _existingArticleIds = await Task.Run(() => repository.GetExistingArticleIds(connection));
            _existingBarcodes = await Task.Run(() => repository.GetExistingBarcodes(connection));

            _cmbDefaultGroup.Items.Clear();
            _cmbDefaultGroup.Items.AddRange(_groups.Cast<object>().ToArray());
            _cmbDefaultUnit.Items.Clear();
            _cmbDefaultUnit.Items.AddRange(_units.Cast<object>().ToArray());
            _cmbDefaultVat.Items.Clear();
            _cmbDefaultVat.Items.AddRange(_vatRates.Cast<object>().ToArray());

            ApplyDefaultsFromProfile();
            SetDefaultsSectionVisible(true);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Не удалось получить справочные данные из базы: " + ex.Message, "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetDefaultsSectionVisible(false);
        }
        finally
        {
            SetBusy(false);
        }
    }

    // ===================== Document / sheet / header row =====================

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

        SetMappingSectionVisible(false);
        ResetPendingImport();

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

        RefreshMappingSection(useProfileMapping: true);
    }

    private void SetHeaderRow(int rowNumber)
    {
        var clamped = Math.Min(Math.Max(rowNumber, (int)_numHeaderRow.Minimum), (int)_numHeaderRow.Maximum);
        _numHeaderRow.Value = clamped;
    }

    /// <summary>Applies the selected import profile's header row (clamped to what the currently
    /// loaded sheet allows), or resets to 1 when no profile is selected. Doesn't trigger a mapping
    /// refresh on its own - callers decide whether to keep the current mapping or reload the profile's.</summary>
    private void ApplyHeaderRowFromProfile()
    {
        _suppressMappingRefresh = true;
        try
        {
            var headerRowNumber = _cmbImportProfile.SelectedItem is ImportProfile profile ? profile.HeaderRowNumber : 1;
            _numHeaderRow.Value = Math.Min(Math.Max(headerRowNumber, (int)_numHeaderRow.Minimum), (int)_numHeaderRow.Maximum);
        }
        finally
        {
            _suppressMappingRefresh = false;
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

    /// <summary>Reads the header row and data rows for the open document at the currently chosen
    /// sheet/header-row. Returns null (optionally showing a validation message) if that isn't possible yet.</summary>
    private (string[] HeaderRow, List<string[]> DataRows, int ColumnCount)? TryGetDocumentLayout(bool showMessages)
    {
        if (_document == null || _cmbSheets.SelectedItem is not string sheetName)
        {
            if (showMessages)
            {
                MessageBox.Show(this, "Сначала откройте документ с товарами.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return null;
        }

        var allRows = _document.GetRows(sheetName);
        var headerRowIndex = (int)_numHeaderRow.Value - 1;
        if (headerRowIndex < 0 || headerRowIndex >= allRows.Count)
        {
            if (showMessages)
            {
                MessageBox.Show(this, "Некорректно указана строка с заголовками.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return null;
        }

        var headerRow = allRows[headerRowIndex];
        var dataRows = allRows.Skip(headerRowIndex + 1).ToList();
        if (dataRows.Count == 0)
        {
            if (showMessages)
            {
                MessageBox.Show(this, "После указанной строки заголовков нет данных.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            return null;
        }

        var columnCount = Math.Max(headerRow.Length, dataRows.Max(r => r.Length));
        return (headerRow, dataRows, columnCount);
    }

    // ===================== Import profiles =====================

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

    private void SaveCurrentAsImportProfile()
    {
        if (TryGetDocumentLayout(showMessages: true) is null)
        {
            return;
        }

        var mapping = TryBuildMappingFromGrid();
        if (mapping == null)
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
        profile.Columns = mapping.ToDictionary();
        profile.DefaultGroupCode = (_cmbDefaultGroup.SelectedItem as GroupInfo)?.Code;
        profile.DefaultUnitId = (_cmbDefaultUnit.SelectedItem as UnitInfo)?.Id;
        profile.DefaultVatId = (_cmbDefaultVat.SelectedItem as VatInfo)?.Id;
        profile.DefaultWeighted = _rbWeightedYes.Checked;
        profile.DefaultExcise = _rbExciseYes.Checked;

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

    // ===================== Column mapping section =====================

    private void BuildMappingGroupBox()
    {
        _mappingGroupBox.Controls.Add(_mappingGrid);
        _mappingGroupBox.Controls.Add(new Label
        {
            Left = 10,
            Top = 220,
            Width = 838,
            Height = 34,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            Text = "Наименование и штрих-код являются основными колонками. Остальные характеристики можно " +
                   "не сопоставлять — для них ниже можно указать значение по умолчанию."
        });

        _mappingGrid.Columns.Add("colIndex", "Колонка");
        _mappingGrid.Columns.Add("colHeader", "Заголовок в документе");
        _mappingGrid.Columns.Add("colSample", "Пример значения");
        _mappingGrid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "colTarget",
            HeaderText = "Назначение",
            DataSource = MappingFieldOptions,
            DisplayMember = "Label",
            ValueMember = "Field",
            FlatStyle = FlatStyle.Flat
        });

        _mappingGrid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_mappingGrid.IsCurrentCellDirty)
            {
                _mappingGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };
        _mappingGrid.CellValueChanged += (_, _) => ResetPendingImport();
    }

    /// <param name="useProfileMapping">
    /// True to pre-fill from the selected import profile (a deliberate "apply this profile" moment:
    /// switching sheet or profile). False to keep whatever is currently in the grid (a header-row
    /// tweak shouldn't discard mapping the user already set).
    /// </param>
    private void RefreshMappingSection(bool useProfileMapping)
    {
        var layout = TryGetDocumentLayout(showMessages: false);
        if (layout is null)
        {
            SetMappingSectionVisible(false);
            return;
        }

        var (headerRow, dataRows, columnCount) = layout.Value;
        var sampleRows = dataRows.Take(5).ToList();
        var initialMapping = useProfileMapping
            ? (_cmbImportProfile.SelectedItem as ImportProfile)?.Columns
            : CaptureMappingGridState();

        _mappingGrid.Rows.Clear();
        for (var c = 0; c < columnCount; c++)
        {
            var header = c < headerRow.Length ? headerRow[c] : string.Empty;
            var sample = sampleRows.Select(r => c < r.Length ? r[c] : string.Empty)
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
            var field = initialMapping != null && initialMapping.TryGetValue(c, out var mapped) ? mapped : TargetField.None;

            _mappingGrid.Rows.Add(ExcelColumnName(c), header, sample, field);
        }

        SetMappingSectionVisible(true);
        ResetPendingImport();
    }

    private Dictionary<int, TargetField> CaptureMappingGridState()
    {
        var result = new Dictionary<int, TargetField>();
        for (var r = 0; r < _mappingGrid.Rows.Count; r++)
        {
            var value = _mappingGrid.Rows[r].Cells["colTarget"].Value;
            var field = value is TargetField tf ? tf : TargetField.None;
            if (field != TargetField.None)
            {
                result[r] = field;
            }
        }

        return result;
    }

    private ColumnMapping? TryBuildMappingFromGrid()
    {
        _mappingGrid.EndEdit();

        var mapping = new ColumnMapping();
        var used = new Dictionary<TargetField, int>();
        for (var r = 0; r < _mappingGrid.Rows.Count; r++)
        {
            var value = _mappingGrid.Rows[r].Cells["colTarget"].Value;
            var field = value is TargetField tf ? tf : TargetField.None;
            if (field == TargetField.None)
            {
                continue;
            }

            if (used.TryGetValue(field, out var firstRow))
            {
                MessageBox.Show(this,
                    $"Поле \"{MappingFieldOptions.First(o => o.Field == field).Label}\" сопоставлено сразу нескольким колонкам " +
                    $"({ExcelColumnName(firstRow)} и {ExcelColumnName(r)}). Каждое поле можно сопоставить только один раз.",
                    "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            used[field] = r;
            mapping.Set(r, field);
        }

        if (!used.ContainsKey(TargetField.Name))
        {
            MessageBox.Show(this, "Необходимо сопоставить колонку с наименованием товара.", "Проверка",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        if (!used.ContainsKey(TargetField.Barcode))
        {
            var proceed = MessageBox.Show(this,
                "Колонка со штрих-кодом не указана. Штрих-код нужно будет сгенерировать, ввести вручную " +
                "или пропустить для каждого товара. Продолжить?",
                "Штрих-код не сопоставлен", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (proceed != DialogResult.Yes)
            {
                return null;
            }
        }

        return mapping;
    }

    // ===================== Defaults section =====================

    private void BuildDefaultsGroupBox()
    {
        _defaultsGroupBox.Controls.Add(new Label { Left = 10, Top = 22, Width = 220, Text = "Группа товаров по умолчанию:" });
        _defaultsGroupBox.Controls.Add(_cmbDefaultGroup);
        _defaultsGroupBox.Controls.Add(new Label { Left = 10, Top = 52, Width = 220, Text = "Единица измерения по умолчанию:" });
        _defaultsGroupBox.Controls.Add(_cmbDefaultUnit);
        _defaultsGroupBox.Controls.Add(new Label { Left = 10, Top = 82, Width = 220, Text = "Ставка НДС по умолчанию:" });
        _defaultsGroupBox.Controls.Add(_cmbDefaultVat);
        _defaultsGroupBox.Controls.Add(new Label { Left = 10, Top = 113, Width = 220, Text = "Товар весовой:" });
        _defaultsGroupBox.Controls.Add(_rbWeightedYes);
        _defaultsGroupBox.Controls.Add(_rbWeightedNo);
        _defaultsGroupBox.Controls.Add(new Label { Left = 10, Top = 143, Width = 220, Text = "Товар подакцизный:" });
        _defaultsGroupBox.Controls.Add(_rbExciseYes);
        _defaultsGroupBox.Controls.Add(_rbExciseNo);

        _cmbDefaultGroup.SelectedIndexChanged += (_, _) => ResetPendingImport();
        _cmbDefaultUnit.SelectedIndexChanged += (_, _) => ResetPendingImport();
        _cmbDefaultVat.SelectedIndexChanged += (_, _) => ResetPendingImport();
        _rbWeightedYes.CheckedChanged += (_, _) => ResetPendingImport();
        _rbExciseYes.CheckedChanged += (_, _) => ResetPendingImport();
    }

    private void ApplyDefaultsFromProfile()
    {
        var profile = _cmbImportProfile.SelectedItem as ImportProfile;

        _cmbDefaultGroup.SelectedItem = profile != null ? _groups?.FirstOrDefault(g => g.Code == profile.DefaultGroupCode) : null;
        _cmbDefaultUnit.SelectedItem = profile != null ? _units?.FirstOrDefault(u => u.Id == profile.DefaultUnitId) : null;
        _cmbDefaultVat.SelectedItem = profile != null ? _vatRates?.FirstOrDefault(v => v.Id == profile.DefaultVatId) : null;

        var weighted = profile?.DefaultWeighted ?? false;
        _rbWeightedYes.Checked = weighted;
        _rbWeightedNo.Checked = !weighted;

        var excise = profile?.DefaultExcise ?? false;
        _rbExciseYes.Checked = excise;
        _rbExciseNo.Checked = !excise;
    }

    private bool TryCollectDefaults(NeedsAnalysis needs, out GroupInfo? defaultGroup, out UnitInfo? defaultUnit,
        out VatInfo? defaultVat, out bool defaultWeighted, out bool defaultExcise)
    {
        defaultGroup = _cmbDefaultGroup.SelectedItem as GroupInfo;
        defaultUnit = _cmbDefaultUnit.SelectedItem as UnitInfo;
        defaultVat = _cmbDefaultVat.SelectedItem as VatInfo;
        defaultWeighted = _rbWeightedYes.Checked;
        defaultExcise = _rbExciseYes.Checked;

        if (needs.NeedsGroupDefault && defaultGroup == null &&
            !Confirm("Не выбрана группа по умолчанию. Товары без определённой группы не будут импортированы. Продолжить?"))
        {
            return false;
        }

        if (needs.NeedsUnitDefault && defaultUnit == null &&
            !Confirm("Не выбрана единица измерения по умолчанию. Товары без определённой единицы измерения не будут импортированы. Продолжить?"))
        {
            return false;
        }

        if (needs.NeedsVatDefault && defaultVat == null &&
            !Confirm("Не выбрана ставка НДС по умолчанию. Товары без определённой ставки НДС не будут импортированы. Продолжить?"))
        {
            return false;
        }

        return true;
    }

    private bool Confirm(string message) =>
        MessageBox.Show(this, message, "Проверка", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;

    // ===================== Barcode resolution section =====================

    private void BuildBarcodeGroupBox()
    {
        _barcodeGroupBox.Controls.Add(new Label { Left = 10, Top = 24, Width = 150, Text = "Для выделенных строк:" });
        _barcodeGroupBox.Controls.Add(_cmbBarcodeApplyAll);
        _barcodeGroupBox.Controls.Add(_btnBarcodeApplyAll);
        _barcodeGroupBox.Controls.Add(_barcodeGrid);

        _cmbBarcodeApplyAll.Items.AddRange(BarcodeActionLabels);
        _cmbBarcodeApplyAll.SelectedIndex = 0;
        _btnBarcodeApplyAll.Click += (_, _) => ApplyBarcodeActionToAllRows();

        _barcodeGrid.Columns.Add("colRow", "Строка");
        _barcodeGrid.Columns.Add("colName", "Наименование");
        _barcodeGrid.Columns.Add("colRaw", "Штрих-код в документе");
        _barcodeGrid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "colAction",
            HeaderText = "Действие",
            DataSource = BarcodeActionLabels.ToList(),
            FlatStyle = FlatStyle.Flat
        });
        _barcodeGrid.Columns.Add("colManual", "Штрих-код вручную");
        _barcodeGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "colPick",
            HeaderText = "",
            Text = "...",
            UseColumnTextForButtonValue = true
        });

        _barcodeGrid.Columns["colRow"].ReadOnly = true;
        _barcodeGrid.Columns["colName"].ReadOnly = true;
        _barcodeGrid.Columns["colRaw"].ReadOnly = true;
        _barcodeGrid.Columns["colManual"].ReadOnly = true;

        _barcodeGrid.CellContentClick += BarcodeGrid_CellContentClick;
    }

    private void BuildBarcodeSection(List<ImportRow> issueRows)
    {
        _barcodeIssueRows = issueRows;
        _barcodeManualValues.Clear();
        _barcodeGrid.Rows.Clear();

        foreach (var row in issueRows)
        {
            _barcodeGrid.Rows.Add(row.SourceRowNumber, row.Name ?? "(без наименования)", row.RawBarcode ?? "(нет)",
                BarcodeActionLabels[0], string.Empty, "...");
        }

        _barcodeGroupBox.Text = $"Требуется решение по штрих-коду ({issueRows.Count})";
    }

    private void BarcodeGrid_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (_barcodeIssueRows == null || e.RowIndex < 0 || _barcodeGrid.Columns[e.ColumnIndex].Name != "colPick")
        {
            return;
        }

        var row = _barcodeIssueRows[e.RowIndex];
        _barcodeManualValues.TryGetValue(row, out var current);

        using var dialog = new ManualBarcodeForm(row.Name ?? string.Empty, current);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _barcodeManualValues[row] = dialog.Barcode;
            _barcodeGrid.Rows[e.RowIndex].Cells["colManual"].Value = dialog.Barcode;
            _barcodeGrid.Rows[e.RowIndex].Cells["colAction"].Value = BarcodeActionLabels[2];
        }
    }

    private void ApplyBarcodeActionToAllRows()
    {
        var label = (string)_cmbBarcodeApplyAll.SelectedItem!;
        foreach (DataGridViewRow row in _barcodeGrid.Rows)
        {
            row.Cells["colAction"].Value = label;
        }
    }

    private bool TryApplyBarcodeSection(ImportOrchestrator orchestrator)
    {
        if (_barcodeIssueRows == null)
        {
            return true;
        }

        _barcodeGrid.EndEdit();

        for (var i = 0; i < _barcodeIssueRows.Count; i++)
        {
            var row = _barcodeIssueRows[i];
            var actionLabel = (string?)_barcodeGrid.Rows[i].Cells["colAction"].Value ?? BarcodeActionLabels[0];
            var action = actionLabel switch
            {
                _ when actionLabel == BarcodeActionLabels[0] => BarcodeAction.Generate,
                _ when actionLabel == BarcodeActionLabels[1] => BarcodeAction.Skip,
                _ => BarcodeAction.Manual
            };

            _barcodeManualValues.TryGetValue(row, out var manualValue);

            if (action == BarcodeAction.Manual && string.IsNullOrWhiteSpace(manualValue))
            {
                MessageBox.Show(this, $"Строка {row.SourceRowNumber}: не введён штрих-код. Нажмите \"...\", чтобы ввести его.",
                    "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!orchestrator.ApplyBarcodeResolution(row, action, manualValue))
            {
                MessageBox.Show(this,
                    $"Строка {row.SourceRowNumber}: указанный штрих-код уже используется другим товаром. Введите другой.",
                    "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        return true;
    }

    // ===================== Import pipeline =====================

    private async Task StartImportAsync()
    {
        if (_pendingRows != null)
        {
            await CommitImportAsync();
            return;
        }

        await PrepareImportAsync();
    }

    private async Task PrepareImportAsync()
    {
        if (_activeProfile == null)
        {
            MessageBox.Show(this, "Сначала выберите подключение к базе данных.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var layout = TryGetDocumentLayout(showMessages: true);
        if (layout is null)
        {
            return;
        }

        var (headerRow, dataRows, _) = layout.Value;
        var headerRowIndex = (int)_numHeaderRow.Value - 1;

        var mapping = TryBuildMappingFromGrid();
        if (mapping == null)
        {
            return;
        }

        if (_groups == null || _units == null || _vatRates == null || _existingArticleIds == null || _existingBarcodes == null)
        {
            await RefreshReferenceDataAsync();
            if (_groups == null || _units == null || _vatRates == null || _existingArticleIds == null || _existingBarcodes == null)
            {
                return;
            }
        }

        var orchestrator = new ImportOrchestrator(_groups!, _units!, _vatRates!, _existingArticleIds!, _existingBarcodes!);
        var rows = orchestrator.BuildRows(dataRows, headerRowIndex + 2);
        if (rows.Count == 0)
        {
            MessageBox.Show(this, "Не найдено ни одной строки с данными.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var needs = orchestrator.Analyze(rows, mapping);
        if (!TryCollectDefaults(needs, out var defaultGroup, out var defaultUnit, out var defaultVat, out var defaultWeighted, out var defaultExcise))
        {
            return;
        }

        orchestrator.Resolve(rows, mapping, defaultGroup, defaultUnit, defaultVat, defaultWeighted, defaultExcise);

        var issueRows = rows.Where(r => r.BarcodeNeedsResolution).ToList();
        if (issueRows.Count > 0 && _showBarcodeIssuesList)
        {
            BuildBarcodeSection(issueRows);
            SetBarcodeSectionVisible(true);
            _pendingRows = rows;
            _pendingOrchestrator = orchestrator;
            _pendingHeaderRow = headerRow;
            _btnStartImport.Text = "Импортировать";
            return;
        }

        foreach (var row in issueRows)
        {
            orchestrator.ApplyBarcodeResolution(row, BarcodeAction.Generate, null);
        }

        await RunImportAsync(orchestrator, rows, headerRow);
    }

    private async Task CommitImportAsync()
    {
        if (_pendingRows == null || _pendingOrchestrator == null || _pendingHeaderRow == null)
        {
            return;
        }

        var rows = _pendingRows;
        var orchestrator = _pendingOrchestrator;
        var headerRow = _pendingHeaderRow;

        if (!TryApplyBarcodeSection(orchestrator))
        {
            return;
        }

        ResetPendingImport();

        await RunImportAsync(orchestrator, rows, headerRow);
    }

    private async Task RunImportAsync(ImportOrchestrator orchestrator, List<ImportRow> rows, string[] headerRow)
    {
        SqlConnection? connection = null;
        try
        {
            SetBusy(true);

            var repository = new SqlServerRepository(_activeProfile!);
            connection = await Task.Run(() => repository.OpenConnection());

            var summary = await Task.Run(() => orchestrator.Import(repository, connection, rows));

            using var resultForm = new ImportResultForm(summary, headerRow);
            resultForm.ShowDialog(this);

            connection.Dispose();
            connection = null;
            await RefreshReferenceDataAsync();
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

    private void ResetPendingImport()
    {
        _pendingRows = null;
        _pendingOrchestrator = null;
        _pendingHeaderRow = null;
        _barcodeIssueRows = null;
        SetBarcodeSectionVisible(false);
        _btnStartImport.Text = "Начать импорт...";
    }

    // ===================== Layout =====================

    private void SetMappingSectionVisible(bool visible)
    {
        _mappingGroupBox.Visible = visible;
        RelayoutSections();
    }

    private void SetDefaultsSectionVisible(bool visible)
    {
        _defaultsGroupBox.Visible = visible;
        RelayoutSections();
    }

    private void SetBarcodeSectionVisible(bool visible)
    {
        _barcodeGroupBox.Visible = visible;
        RelayoutSections();
    }

    private void RelayoutSections()
    {
        var y = _grid.Top + _grid.Height + 12;

        _mappingGroupBox.Top = y;
        if (_mappingGroupBox.Visible)
        {
            y += _mappingGroupBox.Height + 12;
        }

        _defaultsGroupBox.Top = y;
        if (_defaultsGroupBox.Visible)
        {
            y += _defaultsGroupBox.Height + 12;
        }

        _barcodeGroupBox.Top = y;
        if (_barcodeGroupBox.Visible)
        {
            y += _barcodeGroupBox.Height + 12;
        }

        _btnStartImport.Left = 12;
        _btnStartImport.Top = y;
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
