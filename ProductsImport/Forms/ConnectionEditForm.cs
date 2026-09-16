using ProductsImport.Data;
using ProductsImport.Models;

namespace ProductsImport.Forms;

/// <summary>Add or edit a single saved SQL Server connection profile.</summary>
public class ConnectionEditForm : Form
{
    private readonly TextBox _txtName = new() { Left = 140, Top = 15, Width = 260 };
    private readonly TextBox _txtServer = new() { Left = 140, Top = 45, Width = 260 };
    private readonly TextBox _txtDatabase = new() { Left = 140, Top = 75, Width = 260 };
    private readonly CheckBox _chkWindowsAuth = new() { Left = 140, Top = 105, Width = 260, Text = "Использовать Windows-аутентификацию" };
    private readonly TextBox _txtLogin = new() { Left = 140, Top = 135, Width = 260 };
    private readonly TextBox _txtPassword = new() { Left = 140, Top = 165, Width = 260, UseSystemPasswordChar = true };
    private readonly Button _btnTest = new() { Left = 140, Top = 200, Width = 120, Text = "Проверить" };
    private readonly Label _lblTestResult = new() { Left = 270, Top = 205, Width = 260, AutoSize = false };
    private readonly Button _btnOk = new() { Left = 224, Top = 240, Width = 90, Text = "Сохранить", DialogResult = DialogResult.OK };
    private readonly Button _btnCancel = new() { Left = 320, Top = 240, Width = 80, Text = "Отмена", DialogResult = DialogResult.Cancel };

    public ConnectionProfile Profile { get; }

    public ConnectionEditForm(ConnectionProfile? existing)
    {
        Profile = existing ?? new ConnectionProfile();

        Text = existing == null ? "Новое подключение" : "Изменить подключение";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 280);

        Controls.Add(new Label { Left = 15, Top = 18, Width = 120, Text = "Название:" });
        Controls.Add(new Label { Left = 15, Top = 48, Width = 120, Text = "Сервер:" });
        Controls.Add(new Label { Left = 15, Top = 78, Width = 120, Text = "База данных:" });
        Controls.Add(new Label { Left = 15, Top = 138, Width = 120, Text = "Логин:" });
        Controls.Add(new Label { Left = 15, Top = 168, Width = 120, Text = "Пароль:" });

        Controls.Add(_txtName);
        Controls.Add(_txtServer);
        Controls.Add(_txtDatabase);
        Controls.Add(_chkWindowsAuth);
        Controls.Add(_txtLogin);
        Controls.Add(_txtPassword);
        Controls.Add(_btnTest);
        Controls.Add(_lblTestResult);
        Controls.Add(_btnOk);
        Controls.Add(_btnCancel);

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        _chkWindowsAuth.CheckedChanged += (_, _) =>
        {
            _txtLogin.Enabled = !_chkWindowsAuth.Checked;
            _txtPassword.Enabled = !_chkWindowsAuth.Checked;
        };

        _btnTest.Click += (_, _) => TestConnection();
        _btnOk.Click += (_, e) =>
        {
            SaveIntoProfile();
            if (string.IsNullOrWhiteSpace(Profile.Name) || string.IsNullOrWhiteSpace(Profile.Server) || string.IsNullOrWhiteSpace(Profile.Database))
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, "Заполните название, сервер и базу данных.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        _txtName.Text = Profile.Name;
        _txtServer.Text = Profile.Server;
        _txtDatabase.Text = Profile.Database;
        _chkWindowsAuth.Checked = Profile.UseWindowsAuth;
        _txtLogin.Text = Profile.Login;
        _txtPassword.Text = Profile.Password;
        _txtLogin.Enabled = !Profile.UseWindowsAuth;
        _txtPassword.Enabled = !Profile.UseWindowsAuth;
    }

    private void SaveIntoProfile()
    {
        Profile.Name = _txtName.Text.Trim();
        Profile.Server = _txtServer.Text.Trim();
        Profile.Database = _txtDatabase.Text.Trim();
        Profile.UseWindowsAuth = _chkWindowsAuth.Checked;
        Profile.Login = _txtLogin.Text.Trim();
        Profile.Password = _txtPassword.Text;
    }

    private void TestConnection()
    {
        SaveIntoProfile();

        if (string.IsNullOrWhiteSpace(Profile.Server) || string.IsNullOrWhiteSpace(Profile.Database))
        {
            _lblTestResult.ForeColor = Color.DarkRed;
            _lblTestResult.Text = "Укажите сервер и базу данных";
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            SqlServerRepository.TestConnection(Profile);
            _lblTestResult.ForeColor = Color.DarkGreen;
            _lblTestResult.Text = "Подключение успешно";
        }
        catch (Exception ex)
        {
            _lblTestResult.ForeColor = Color.DarkRed;
            _lblTestResult.Text = "Ошибка: " + ex.Message;
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }
}
