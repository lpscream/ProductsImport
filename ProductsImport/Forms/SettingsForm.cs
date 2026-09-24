using ProductsImport.Localization;

namespace ProductsImport.Forms;

/// <summary>Lets the user pick the interface language. Takes effect after restarting the app, since
/// every other form's text is set once, from <see cref="Strings.Current"/>, in its field initializers.</summary>
public class SettingsForm : Form
{
    // A plain record's synthesized ToString() prints "LanguageOption { Value = ..., Label = ... }",
    // which is what an unbound ComboBox.Items entry displays - override it to show just the label.
    private sealed record LanguageOption(Language Value, string Label)
    {
        public override string ToString() => Label;
    }

    private readonly ComboBox _cmbLanguage = new() { Left = 175, Top = 15, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label _lblRestartNotice = new() { Left = 15, Top = 55, Width = 360, Height = 40 };
    private readonly Button _btnOk = new() { Width = 90, DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Width = 90, DialogResult = DialogResult.Cancel };

    public Language SelectedLanguage { get; private set; }

    public SettingsForm(Language currentLanguage)
    {
        Text = Strings.T("Settings_Title");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(390, 135);

        _btnOk.Text = Strings.T("Common_OK");
        _btnCancel.Text = Strings.T("Common_Cancel");
        _lblRestartNotice.Text = Strings.T("Settings_RestartNotice");
        _lblRestartNotice.ForeColor = Color.DimGray;

        Controls.Add(new Label { Left = 15, Top = 19, Width = 155, Text = Strings.T("Settings_LblLanguage") });
        Controls.Add(_cmbLanguage);
        Controls.Add(_lblRestartNotice);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);

        _btnOk.Left = 195;
        _btnOk.Top = 100;
        _btnCancel.Left = 290;
        _btnCancel.Top = 100;
        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        var options = new[]
        {
            new LanguageOption(Language.Russian, Strings.T("Settings_LangRussian")),
            new LanguageOption(Language.Ukrainian, Strings.T("Settings_LangUkrainian")),
            new LanguageOption(Language.Romanian, Strings.T("Settings_LangRomanian"))
        };
        _cmbLanguage.Items.AddRange(options);
        _cmbLanguage.SelectedItem = options.FirstOrDefault(o => o.Value == currentLanguage) ?? options[0];

        _btnOk.Click += (_, _) =>
        {
            SelectedLanguage = ((LanguageOption)_cmbLanguage.SelectedItem!).Value;
        };
    }
}
