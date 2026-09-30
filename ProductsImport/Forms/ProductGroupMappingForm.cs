using ProductsImport.Localization;
using ProductsImport.Models;

namespace ProductsImport.Forms;

/// <summary>
/// "Сопоставление групп товаров": lets the user override the group assigned to individual products
/// (whichever group they auto-resolved to - matched, pending-creation, or the default), picking from
/// existing database groups. Supports selecting several rows (Shift/Ctrl/Ctrl+A) and applying one
/// group to all of them at once.
/// </summary>
public class ProductGroupMappingForm : Form
{
    // See the equivalent comment in ValueMappingForm: DataGridViewComboBoxCell backed by the unbound
    // Items collection can throw "DataGridViewComboBoxCell value is not valid" when a value is picked
    // from the dropdown, so bind through DataSource + DisplayMember + ValueMember instead.
    private sealed record GroupOption(object Value, string Display);

    private static readonly object NoOverride = Strings.T("GroupMap_NoOverride");

    private readonly ComboBox _cmbApplyGroup = new() { Left = 175, Top = 12, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Button _btnApplyToSelected = new() { Left = 445, Top = 11, Width = 170, Text = Strings.T("GroupMap_BtnApply") };

    private readonly DataGridView _grid = new()
    {
        Left = 12,
        Top = 45,
        Width = 700,
        Height = 425,
        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = true,
        EditMode = DataGridViewEditMode.EditOnEnter
    };

    private readonly Button _btnOk = new() { Width = 90, Text = Strings.T("Common_OK"), DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Width = 90, Text = Strings.T("Common_Cancel"), DialogResult = DialogResult.Cancel };

    /// <summary>SourceRowNumber -> chosen group, for rows the user explicitly overrode.</summary>
    public Dictionary<int, GroupInfo> Overrides { get; } = new();

    public ProductGroupMappingForm(List<ImportRow> rows, List<GroupInfo> groups, IReadOnlyDictionary<int, GroupInfo> existingOverrides)
    {
        Text = Strings.T("GroupMap_TitleFormat", rows.Count);
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ClientSize = new Size(724, 520);
        MinimumSize = new Size(560, 380);

        Controls.Add(new Label { Left = 12, Top = 16, Width = 160, Text = Strings.T("Main_LblForSelectedRows") });
        Controls.Add(_cmbApplyGroup);
        Controls.Add(_btnApplyToSelected);
        Controls.Add(_grid);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);

        _btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnOk.Top = 485;
        _btnCancel.Top = 485;
        _btnOk.Left = ClientSize.Width - 200;
        _btnCancel.Left = ClientSize.Width - 100;
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        _cmbApplyGroup.Items.AddRange(groups.Cast<object>().ToArray());
        _btnApplyToSelected.Click += (_, _) => ApplyToSelectedRows();

        BuildGrid(rows, groups, existingOverrides);

        _btnOk.Click += (_, e) => CollectOverrides();
    }

    private void BuildGrid(List<ImportRow> rows, List<GroupInfo> groups, IReadOnlyDictionary<int, GroupInfo> existingOverrides)
    {
        _grid.Columns.Add("colRow", Strings.T("Common_Row"));
        _grid.Columns.Add("colName", Strings.T("Common_Name"));
        _grid.Columns.Add("colCurrent", Strings.T("GroupMap_ColCurrent"));

        var groupOptions = new List<GroupOption> { new(NoOverride, (string)NoOverride) };
        groupOptions.AddRange(groups.Select(g => new GroupOption(g, g.ToString() ?? string.Empty)));

        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "colGroup",
            HeaderText = Strings.T("GroupMap_ColNew"),
            DataSource = groupOptions,
            DisplayMember = "Display",
            ValueMember = "Value",
            FlatStyle = FlatStyle.Flat
        });

        foreach (var row in rows)
        {
            var currentDisplay = row.PendingNewGroupName != null
                ? Strings.T("GroupMap_PendingCreate", row.PendingNewGroupName)
                : groups.FirstOrDefault(g => g.Code == row.GroupCode)?.Name ?? "—";
            var initialOverride = existingOverrides.TryGetValue(row.SourceRowNumber, out var group) ? (object)group : NoOverride;

            _grid.Rows.Add(row.SourceRowNumber, row.Name ?? Strings.T("Common_NoName"), currentDisplay, initialOverride);
        }

        _grid.Columns["colRow"].ReadOnly = true;
        _grid.Columns["colName"].ReadOnly = true;
        _grid.Columns["colCurrent"].ReadOnly = true;
    }

    private void ApplyToSelectedRows()
    {
        if (_cmbApplyGroup.SelectedItem is not GroupInfo group)
        {
            MessageBox.Show(this, Strings.T("GroupMap_Msg_SelectGroup"), Strings.T("Common_Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_grid.SelectedRows.Count == 0)
        {
            MessageBox.Show(this, Strings.T("GroupMap_Msg_SelectRows"), Strings.T("Common_Warning"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        foreach (DataGridViewRow row in _grid.SelectedRows)
        {
            row.Cells["colGroup"].Value = group;
        }
    }

    private void CollectOverrides()
    {
        _grid.EndEdit();
        Overrides.Clear();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Cells["colGroup"].Value is GroupInfo group)
            {
                var sourceRowNumber = (int)row.Cells["colRow"].Value!;
                Overrides[sourceRowNumber] = group;
            }
        }
    }
}
