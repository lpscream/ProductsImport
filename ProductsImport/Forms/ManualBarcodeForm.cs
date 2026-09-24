using ProductsImport.Localization;
using ProductsImport.Services;

namespace ProductsImport.Forms;

/// <summary>Small dialog for typing a barcode in by hand.</summary>
public class ManualBarcodeForm : Form
{
    private readonly TextBox _txtBarcode = new() { Left = 15, Top = 35, Width = 260 };
    private readonly Label _lblError = new() { Left = 15, Top = 65, Width = 260, ForeColor = Color.DarkRed };
    private readonly Button _btnOk = new() { Left = 115, Top = 95, Width = 80, Text = Strings.T("Common_OK"), DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Left = 200, Top = 95, Width = 80, Text = Strings.T("Common_Cancel"), DialogResult = DialogResult.Cancel };

    public string Barcode => _txtBarcode.Text.Trim();

    public ManualBarcodeForm(string productName, string? currentValue)
    {
        Text = Strings.T("Manual_Title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(290, 135);

        Controls.Add(new Label { Left = 15, Top = 12, Width = 260, Text = Strings.T("Manual_LblProduct", productName) });
        Controls.Add(_txtBarcode);
        Controls.Add(_lblError);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        _txtBarcode.Text = currentValue ?? string.Empty;

        _btnOk.Click += (_, e) =>
        {
            if (!BarcodeGenerator.IsValidBarcode(Barcode))
            {
                _lblError.Text = Strings.T("Manual_Msg_InvalidBarcode");
                DialogResult = DialogResult.None;
            }
        };
    }
}
