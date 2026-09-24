using ProductsImport.Localization;

namespace ProductsImport.Forms;

/// <summary>
/// Generic "map each unique raw document value to a database value" dialog, used for both
/// "Сопоставление налоговых групп" (raw value -> VatInfo) and "Сопоставление акцизности товара"
/// (raw value -> "Да"/"Нет"). A row left unset means "use the default rule".
/// </summary>
public class ValueMappingForm : Form
{
    // DataGridViewComboBoxCell backed by the unbound Items collection (no DataSource/DisplayMember/
    // ValueMember) is unreliable for arbitrary objects: committing a selection can throw
    // "DataGridViewComboBoxCell value is not valid". Binding through DataSource + DisplayMember +
    // ValueMember - the same pattern MainForm's column-mapping grid already uses successfully - routes
    // selection through WinForms' actual data-binding machinery instead of its unbound-combo fallback
    // path, so this wrapper gives every option a uniform (Display, Value) shape to bind to.
    private sealed record OptionItem(object Value, string Display);

    private static readonly object NotSet = Strings.T("Common_NotSelectedValue");

    private readonly DataGridView _grid = new()
    {
        Left = 12,
        Top = 12,
        Width = 460,
        Height = 380,
        Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        RowHeadersVisible = false,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.CellSelect
    };

    private readonly Button _btnOk = new() { Width = 90, Text = Strings.T("Common_OK"), DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Width = 90, Text = Strings.T("Common_Cancel"), DialogResult = DialogResult.Cancel };

    /// <summary>Raw value -> chosen database value. Rows left unset are omitted.</summary>
    public Dictionary<string, object> Mapping { get; } = new();

    public ValueMappingForm(string title, string rawColumnHeader, string targetColumnHeader,
        IReadOnlyList<string> rawValues, IReadOnlyList<object> options,
        IReadOnlyDictionary<string, object>? initialMapping = null)
    {
        Text = title;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        ClientSize = new Size(484, 445);
        MinimumSize = new Size(400, 300);

        _btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        _btnOk.Top = 410;
        _btnCancel.Top = 410;
        _btnOk.Left = ClientSize.Width - 200;
        _btnCancel.Left = ClientSize.Width - 100;

        Controls.Add(_grid);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        _grid.Columns.Add("colRaw", rawColumnHeader);

        var optionItems = new List<OptionItem> { new(NotSet, (string)NotSet) };
        optionItems.AddRange(options.Select(o => new OptionItem(o, o.ToString() ?? string.Empty)));

        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "colTarget",
            HeaderText = targetColumnHeader,
            DataSource = optionItems,
            DisplayMember = "Display",
            ValueMember = "Value",
            FlatStyle = FlatStyle.Flat
        });
        _grid.Columns["colRaw"].ReadOnly = true;

        foreach (var raw in rawValues)
        {
            var initial = initialMapping != null && initialMapping.TryGetValue(raw, out var value) ? value : NotSet;
            _grid.Rows.Add(raw, initial);
        }

        _btnOk.Click += (_, e) =>
        {
            _grid.EndEdit();
            Mapping.Clear();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                var raw = (string)row.Cells["colRaw"].Value!;
                var value = row.Cells["colTarget"].Value;
                if (value != null && !Equals(value, NotSet))
                {
                    Mapping[raw] = value;
                }
            }
        };
    }
}
