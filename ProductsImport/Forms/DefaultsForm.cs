using ProductsImport.Models;
using ProductsImport.Services;

namespace ProductsImport.Forms;

/// <summary>
/// Asks for fallback values used for rows whose spreadsheet column is missing or has a blank/unrecognized cell:
/// default group, unit of measure, VAT rate, "sold by weight" and "excise" flags.
/// </summary>
public class DefaultsForm : Form
{
    private readonly FlowLayoutPanel _panel = new()
    {
        Left = 12,
        Top = 12,
        Width = 420,
        Height = 300,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true
    };

    private readonly Button _btnOk = new() { Width = 90, Text = "Далее", DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Width = 90, Text = "Отмена", DialogResult = DialogResult.Cancel };

    private ComboBox? _cmbGroup;
    private ComboBox? _cmbUnit;
    private ComboBox? _cmbVat;
    private RadioButton? _rbWeightedYes;
    private RadioButton? _rbExciseYes;

    private readonly NeedsAnalysis _needs;

    public GroupInfo? SelectedGroup { get; private set; }
    public UnitInfo? SelectedUnit { get; private set; }
    public VatInfo? SelectedVat { get; private set; }
    public bool DefaultWeighted { get; private set; }
    public bool DefaultExcise { get; private set; }

    public DefaultsForm(NeedsAnalysis needs, List<GroupInfo> groups, List<UnitInfo> units, List<VatInfo> vatRates)
    {
        _needs = needs;

        Text = "Значения по умолчанию";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(444, 350);

        Controls.Add(_panel);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        _btnOk.Left = ClientSize.Width - 200;
        _btnOk.Top = 315;
        _btnCancel.Left = ClientSize.Width - 100;
        _btnCancel.Top = 315;
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        var intro = new Label
        {
            Width = 400,
            Height = 40,
            Text = "В документе не для всех товаров удалось определить перечисленные ниже характеристики. " +
                   "Укажите значение, которое нужно применить в таких случаях."
        };
        _panel.Controls.Add(intro);

        if (needs.NeedsGroupDefault)
        {
            _panel.Controls.Add(new Label { Width = 400, Text = "Группа товаров:" });
            _cmbGroup = new ComboBox { Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbGroup.Items.AddRange(groups.Cast<object>().ToArray());
            if (_cmbGroup.Items.Count > 0)
            {
                _cmbGroup.SelectedIndex = 0;
            }

            _panel.Controls.Add(_cmbGroup);
        }

        if (needs.NeedsUnitDefault)
        {
            _panel.Controls.Add(new Label { Width = 400, Text = "Единица измерения:" });
            _cmbUnit = new ComboBox { Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbUnit.Items.AddRange(units.Cast<object>().ToArray());
            if (_cmbUnit.Items.Count > 0)
            {
                _cmbUnit.SelectedIndex = 0;
            }

            _panel.Controls.Add(_cmbUnit);
        }

        if (needs.NeedsVatDefault)
        {
            _panel.Controls.Add(new Label { Width = 400, Text = "Ставка НДС:" });
            _cmbVat = new ComboBox { Width = 400, DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbVat.Items.AddRange(vatRates.Cast<object>().ToArray());
            if (_cmbVat.Items.Count > 0)
            {
                _cmbVat.SelectedIndex = 0;
            }

            _panel.Controls.Add(_cmbVat);
        }

        if (needs.NeedsWeightedDefault)
        {
            _panel.Controls.Add(new Label { Width = 400, Text = "Товар весовой?" });
            var flow = new FlowLayoutPanel { Width = 400, Height = 26, FlowDirection = FlowDirection.LeftToRight };
            _rbWeightedYes = new RadioButton { Text = "Да", AutoSize = true };
            var no = new RadioButton { Text = "Нет", AutoSize = true, Checked = true };
            flow.Controls.Add(_rbWeightedYes);
            flow.Controls.Add(no);
            _panel.Controls.Add(flow);
        }

        if (needs.NeedsExciseDefault)
        {
            _panel.Controls.Add(new Label { Width = 400, Text = "Товар подакцизный?" });
            var flow = new FlowLayoutPanel { Width = 400, Height = 26, FlowDirection = FlowDirection.LeftToRight };
            _rbExciseYes = new RadioButton { Text = "Да", AutoSize = true };
            var no = new RadioButton { Text = "Нет", AutoSize = true, Checked = true };
            flow.Controls.Add(_rbExciseYes);
            flow.Controls.Add(no);
            _panel.Controls.Add(flow);
        }

        _btnOk.Click += (_, e) =>
        {
            if (!ValidateSelections())
            {
                DialogResult = DialogResult.None;
                return;
            }

            SelectedGroup = _cmbGroup?.SelectedItem as GroupInfo;
            SelectedUnit = _cmbUnit?.SelectedItem as UnitInfo;
            SelectedVat = _cmbVat?.SelectedItem as VatInfo;
            DefaultWeighted = _rbWeightedYes?.Checked ?? false;
            DefaultExcise = _rbExciseYes?.Checked ?? false;
        };
    }

    private bool ValidateSelections()
    {
        if (_needs.NeedsGroupDefault && _cmbGroup?.SelectedItem == null)
        {
            return Confirm("Не выбрана группа по умолчанию. Товары без определённой группы не будут импортированы. Продолжить?");
        }

        if (_needs.NeedsUnitDefault && _cmbUnit?.SelectedItem == null)
        {
            return Confirm("Не выбрана единица измерения по умолчанию. Товары без определённой единицы измерения не будут импортированы. Продолжить?");
        }

        if (_needs.NeedsVatDefault && _cmbVat?.SelectedItem == null)
        {
            return Confirm("Не выбрана ставка НДС по умолчанию. Товары без определённой ставки НДС не будут импортированы. Продолжить?");
        }

        return true;
    }

    private bool Confirm(string message) =>
        MessageBox.Show(this, message, "Проверка", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
}
