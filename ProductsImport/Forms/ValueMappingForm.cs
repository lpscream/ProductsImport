namespace ProductsImport.Forms;

/// <summary>
/// Generic "map each unique raw document value to a database value" dialog, used for both
/// "Сопоставление налоговых групп" (raw value -> VatInfo) and "Сопоставление акцизности товара"
/// (raw value -> "Да"/"Нет"). A row left as "— не выбрано —" means "use the default rule".
/// </summary>
public class ValueMappingForm : Form
{
    private static readonly object NotSet = "— не выбрано —";

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

    private readonly Button _btnOk = new() { Width = 90, Text = "ОК", DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Width = 90, Text = "Отмена", DialogResult = DialogResult.Cancel };

    /// <summary>Raw value -> chosen database value. Rows left as "— не выбрано —" are omitted.</summary>
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
        _grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = "colTarget",
            HeaderText = targetColumnHeader,
            DataSource = new object[] { NotSet }.Concat(options).ToArray(),
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
