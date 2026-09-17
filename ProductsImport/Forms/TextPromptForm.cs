namespace ProductsImport.Forms;

/// <summary>Generic small dialog for typing a single line of text (e.g. a profile name).</summary>
public class TextPromptForm : Form
{
    private readonly TextBox _txtValue = new() { Left = 15, Top = 35, Width = 300 };
    private readonly Button _btnOk = new() { Width = 80, Text = "ОК", DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Width = 80, Text = "Отмена", DialogResult = DialogResult.Cancel };

    public string Value => _txtValue.Text.Trim();

    public TextPromptForm(string title, string prompt, string initialValue = "")
    {
        Text = title;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(330, 105);

        Controls.Add(new Label { Left = 15, Top = 12, Width = 300, Text = prompt });
        Controls.Add(_txtValue);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        _btnOk.Left = 150;
        _btnOk.Top = 65;
        _btnCancel.Left = 235;
        _btnCancel.Top = 65;

        _txtValue.Text = initialValue;
        _txtValue.SelectAll();

        _btnOk.Click += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(Value))
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, "Введите значение.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
    }
}
