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
        new(TargetField.TaxRate, "Налоговая ставка"),
        new(TargetField.Excise, "Подакцизный (да/нет)"),
        new(TargetField.Uktzed, "УКТЗЕД")
    };

    private static readonly string[] BarcodeActionLabels = { "Сгенерировать штрих-код", "Не импортировать", "Ввести вручную" };

    // --- Connection ---
    private readonly Button _btnConnection = new() { Left = 12, Top = 12, Width = 200, Text = "Подключение к базе данных..." };
    private readonly Label _lblConnection = new() { Left = 220, Top = 17, Width = 660, Text = "Подключение не выбрано" };

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
        Height = 292,
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

    private readonly Button _btnGroupMapping = new() { Left = 10, Top = 220, Width = 260, Text = "Сопоставление групп товаров" };
    private readonly Button _btnTaxRateMapping = new() { Left = 280, Top = 220, Width = 260, Text = "Сопоставление налоговых групп", Enabled = false };
    private readonly Button _btnExciseMapping = new() { Left = 550, Top = 220, Width = 260, Text = "Сопоставление акцизности товара", Enabled = false };

    private bool _suppressMappingComboRefresh;
    private readonly Dictionary<string, VatInfo> _taxRateMapping = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _exciseMapping = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, GroupInfo> _groupOverrides = new();

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

    // Each pair needs its own container: WinForms groups RadioButtons by immediate parent, so
    // without separate panels these four would all belong to one group (only one could be checked).
    private readonly Panel _weightedPanel = new() { Left = 235, Top = 108, Width = 200, Height = 24 };
    private readonly RadioButton _rbWeightedYes = new() { Left = 0, Top = 0, Width = 60, Text = "Да", AutoSize = true };
    private readonly RadioButton _rbWeightedNo = new() { Left = 65, Top = 0, Width = 60, Text = "Нет", AutoSize = true, Checked = true };
    private readonly Panel _excisePanel = new() { Left = 235, Top = 138, Width = 200, Height = 24 };
    private readonly RadioButton _rbExciseYes = new() { Left = 0, Top = 0, Width = 60, Text = "Да", AutoSize = true };
    private readonly RadioButton _rbExciseNo = new() { Left = 65, Top = 0, Width = 60, Text = "Нет", AutoSize = true, Checked = true };

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
        _btnGroupMapping.Click += async (_, _) => await OpenGroupMappingAsync();
        _btnTaxRateMapping.Click += (_, _) => OpenTaxRateMapping();
        _btnExciseMapping.Click += (_, _) => OpenExciseMapping();
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
        _mappingGroupBox.Controls.Add(_btnGroupMapping);
        _mappingGroupBox.Controls.Add(_btnTaxRateMapping);
        _mappingGroupBox.Controls.Add(_btnExciseMapping);
        _mappingGroupBox.Controls.Add(new Label
        {
            Left = 10,
            Top = 252,
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
        _mappingGrid.CellValueChanged += (_, _) =>
        {
            if (_suppressMappingComboRefresh)
            {
                return;
            }

            ResetPendingImport();
            RefreshMappingComboOptions();
        };
    }

    /// <summary>
    /// Once a target field is picked for one column, it disappears from every other column's dropdown
    /// (until unpicked again), so the same field can't be mapped twice by accident.
    /// </summary>
    private void RefreshMappingComboOptions()
    {
        _suppressMappingComboRefresh = true;
        try
        {
            var selections = new TargetField[_mappingGrid.Rows.Count];
            for (var r = 0; r < _mappingGrid.Rows.Count; r++)
            {
                var value = _mappingGrid.Rows[r].Cells["colTarget"].Value;
                selections[r] = value is TargetField tf ? tf : TargetField.None;
            }

            for (var r = 0; r < _mappingGrid.Rows.Count; r++)
            {
                var current = selections[r];
                var usedElsewhere = selections.Where((f, i) => i != r && f != TargetField.None).ToHashSet();
                var available = MappingFieldOptions.Where(o => o.Field == current || !usedElsewhere.Contains(o.Field)).ToArray();

                var cell = (DataGridViewComboBoxCell)_mappingGrid.Rows[r].Cells["colTarget"];
                cell.DataSource = available;
                cell.DisplayMember = "Label";
                cell.ValueMember = "Field";
                cell.Value = current;
            }
        }
        finally
        {
            _suppressMappingComboRefresh = false;
        }

        UpdateMappingButtonsEnabled();
    }

    private void UpdateMappingButtonsEnabled()
    {
        _btnTaxRateMapping.Enabled = GetMappedColumnIndex(TargetField.TaxRate) != null;
        _btnExciseMapping.Enabled = GetMappedColumnIndex(TargetField.Excise) != null;
    }

    private int? GetMappedColumnIndex(TargetField field)
    {
        for (var r = 0; r < _mappingGrid.Rows.Count; r++)
        {
            if (_mappingGrid.Rows[r].Cells["colTarget"].Value is TargetField tf && tf == field)
            {
                return r;
            }
        }

        return null;
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

        RefreshMappingComboOptions();
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

    private void OpenTaxRateMapping()
    {
        var layout = TryGetDocumentLayout(showMessages: true);
        if (layout is null)
        {
            return;
        }

        var columnIndex = GetMappedColumnIndex(TargetField.TaxRate);
        if (columnIndex is null)
        {
            return;
        }

        if (_vatRates == null || _vatRates.Count == 0)
        {
            MessageBox.Show(this, "Сначала подключитесь к базе данных.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rawValues = CollectUniqueRawValues(layout.Value.DataRows, columnIndex.Value);
        if (rawValues.Count == 0)
        {
            MessageBox.Show(this, "В сопоставленной колонке нет значений для сопоставления.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var initial = rawValues.Where(v => _taxRateMapping.ContainsKey(v)).ToDictionary(v => v, object (v) => _taxRateMapping[v]);
        using var form = new ValueMappingForm("Сопоставление налоговых групп", "Значение в документе", "Ставка НДС",
            rawValues, _vatRates!.Cast<object>().ToList(), initial);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        foreach (var raw in rawValues)
        {
            if (form.Mapping.TryGetValue(raw, out var value))
            {
                _taxRateMapping[raw] = (VatInfo)value;
            }
            else
            {
                _taxRateMapping.Remove(raw);
            }
        }

        ResetPendingImport();
    }

    private void OpenExciseMapping()
    {
        var layout = TryGetDocumentLayout(showMessages: true);
        if (layout is null)
        {
            return;
        }

        var columnIndex = GetMappedColumnIndex(TargetField.Excise);
        if (columnIndex is null)
        {
            return;
        }

        var rawValues = CollectUniqueRawValues(layout.Value.DataRows, columnIndex.Value);
        if (rawValues.Count == 0)
        {
            MessageBox.Show(this, "В сопоставленной колонке нет значений для сопоставления.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var options = new object[] { "Да", "Нет" };
        var initial = rawValues.Where(v => _exciseMapping.ContainsKey(v)).ToDictionary(v => v, object (v) => _exciseMapping[v] ? "Да" : "Нет");
        using var form = new ValueMappingForm("Сопоставление акцизности товара", "Значение в документе", "Подакцизный",
            rawValues, options, initial);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        foreach (var raw in rawValues)
        {
            if (form.Mapping.TryGetValue(raw, out var value))
            {
                _exciseMapping[raw] = (string)value == "Да";
            }
            else
            {
                _exciseMapping.Remove(raw);
            }
        }

        ResetPendingImport();
    }

    private static List<string> CollectUniqueRawValues(List<string[]> dataRows, int columnIndex)
    {
        return dataRows
            .Select(r => columnIndex < r.Length ? r[columnIndex]?.Trim() : null)
            .Where(v => !string.IsNullOrEmpty(v))
            .Select(v => v!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private async Task OpenGroupMappingAsync()
    {
        var built = await BuildAndResolveRowsAsync();
        if (built is null)
        {
            return;
        }

        var (_, rows, _) = built.Value;

        using var form = new ProductGroupMappingForm(rows, _groups!, _groupOverrides);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _groupOverrides.Clear();
        foreach (var kvp in form.Overrides)
        {
            _groupOverrides[kvp.Key] = kvp.Value;
        }

        ResetPendingImport();
    }

    // ===================== Defaults section =====================

    private void BuildDefaultsGroupBox()
    {
        _defaultsGroupBox.Controls.Add(new Label { Left = 10, Top = 22, Width = 220, Text = "Группа товаров по умолчанию:" });
        _defaultsGroupBox.Controls.Add(_cmbDefaultGroup);
        _defaultsGroupBox.Controls.Add(new Label { Left = 10, Top = 52, Width = 220, Text = "Единица измерения по умолчанию:" });
        _defaultsGroupBox.Controls.Add(_cmbDefaultUnit);
        _defaultsGroupBox.Controls.Add(new Label { Left = 10, Top = 82, Width = 225, Text = "Налоговая ставка по умолчанию:" });
        _defaultsGroupBox.Controls.Add(_cmbDefaultVat);
        _defaultsGroupBox.Controls.Add(new Label { Left = 10, Top = 113, Width = 220, Text = "Товар весовой:" });
        _weightedPanel.Controls.Add(_rbWeightedYes);
        _weightedPanel.Controls.Add(_rbWeightedNo);
        _defaultsGroupBox.Controls.Add(_weightedPanel);
        _defaultsGroupBox.Controls.Add(new Label { Left = 10, Top = 143, Width = 220, Text = "Товар подакцизный:" });
        _excisePanel.Controls.Add(_rbExciseYes);
        _excisePanel.Controls.Add(_rbExciseNo);
        _defaultsGroupBox.Controls.Add(_excisePanel);

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
            !Confirm("Не выбрана налоговая ставка по умолчанию. Товары без определённой ставки не будут импортированы. Продолжить?"))
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

    /// <summary>
    /// Validates the document/mapping/reference-data preconditions, builds rows, collects defaults and
    /// fully resolves them (including group auto-creation markers and manual group overrides). Shared by
    /// the actual import and by "Сопоставление групп товаров" (which only needs the resolved rows to
    /// show and let the user tweak them, not to commit anything).
    /// </summary>
    private async Task<(ImportOrchestrator Orchestrator, List<ImportRow> Rows, string[] HeaderRow)?> BuildAndResolveRowsAsync()
    {
        if (_activeProfile == null)
        {
            MessageBox.Show(this, "Сначала выберите подключение к базе данных.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var layout = TryGetDocumentLayout(showMessages: true);
        if (layout is null)
        {
            return null;
        }

        var (headerRow, dataRows, _) = layout.Value;
        var headerRowIndex = (int)_numHeaderRow.Value - 1;

        var mapping = TryBuildMappingFromGrid();
        if (mapping == null)
        {
            return null;
        }

        if (_groups == null || _units == null || _vatRates == null || _existingArticleIds == null || _existingBarcodes == null)
        {
            await RefreshReferenceDataAsync();
            if (_groups == null || _units == null || _vatRates == null || _existingArticleIds == null || _existingBarcodes == null)
            {
                return null;
            }
        }

        var orchestrator = new ImportOrchestrator(_groups!, _units!, _vatRates!, _existingArticleIds!, _existingBarcodes!);
        var rows = orchestrator.BuildRows(dataRows, headerRowIndex + 2);
        if (rows.Count == 0)
        {
            MessageBox.Show(this, "Не найдено ни одной строки с данными.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        var needs = orchestrator.Analyze(rows, mapping, _taxRateMapping, _exciseMapping);
        if (!TryCollectDefaults(needs, out var defaultGroup, out var defaultUnit, out var defaultVat, out var defaultWeighted, out var defaultExcise))
        {
            return null;
        }

        orchestrator.Resolve(rows, mapping, defaultGroup, defaultUnit, defaultVat, defaultWeighted, defaultExcise, _taxRateMapping, _exciseMapping);
        ApplyGroupOverrides(rows);

        return (orchestrator, rows, headerRow);
    }

    /// <summary>Applies manual per-product group choices from "Сопоставление групп товаров", stomping
    /// whatever the automatic name-match/auto-create resolution produced for that row.</summary>
    private void ApplyGroupOverrides(List<ImportRow> rows)
    {
        foreach (var row in rows)
        {
            if (!_groupOverrides.TryGetValue(row.SourceRowNumber, out var group))
            {
                continue;
            }

            row.GroupCode = group.Code;
            row.PendingNewGroupName = null;
            if (row.Error == "Не удалось определить группу товара")
            {
                row.Error = null;
            }
        }
    }

    private async Task PrepareImportAsync()
    {
        var built = await BuildAndResolveRowsAsync();
        if (built is null)
        {
            return;
        }

        var (orchestrator, rows, headerRow) = built.Value;

        var issueRows = rows.Where(r => r.BarcodeNeedsResolution).ToList();
        if (issueRows.Count > 0)
        {
            BuildBarcodeSection(issueRows);
            SetBarcodeSectionVisible(true);
            _pendingRows = rows;
            _pendingOrchestrator = orchestrator;
            _pendingHeaderRow = headerRow;
            _btnStartImport.Text = "Импортировать";
            return;
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
        _btnGroupMapping.Enabled = !busy;

        if (busy)
        {
            _btnTaxRateMapping.Enabled = false;
            _btnExciseMapping.Enabled = false;
        }
        else
        {
            UpdateMappingButtonsEnabled();
        }
    }
}
