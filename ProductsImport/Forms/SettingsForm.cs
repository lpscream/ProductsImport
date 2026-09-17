namespace ProductsImport.Forms;

/// <summary>Application-wide preferences (not tied to a connection or import profile).</summary>
public class SettingsForm : Form
{
    private readonly CheckBox _chkShowBarcodeIssuesList = new()
    {
        Left = 15,
        Top = 15,
        Width = 380,
        Height = 40,
        Text = "Отображать список товаров для введения штрих-кода"
    };

    private readonly Button _btnOk = new() { Width = 90, Text = "ОК", DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Width = 90, Text = "Отмена", DialogResult = DialogResult.Cancel };

    public bool ShowBarcodeIssuesList => _chkShowBarcodeIssuesList.Checked;

    public SettingsForm(bool showBarcodeIssuesList)
    {
        Text = "Настройки";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 100);

        Controls.Add(_chkShowBarcodeIssuesList);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        _btnOk.Left = 140;
        _btnOk.Top = 62;
        _btnCancel.Left = 235;
        _btnCancel.Top = 62;

        _chkShowBarcodeIssuesList.Checked = showBarcodeIssuesList;
    }
}
