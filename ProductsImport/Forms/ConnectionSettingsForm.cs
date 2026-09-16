using ProductsImport.Data;
using ProductsImport.Models;

namespace ProductsImport.Forms;

/// <summary>Manages the list of saved connections and lets the user pick the active one.</summary>
public class ConnectionSettingsForm : Form
{
    private readonly ListBox _list = new() { Left = 15, Top = 15, Width = 300, Height = 240 };
    private readonly Button _btnAdd = new() { Left = 325, Top = 15, Width = 110, Text = "Добавить..." };
    private readonly Button _btnEdit = new() { Left = 325, Top = 50, Width = 110, Text = "Изменить..." };
    private readonly Button _btnDelete = new() { Left = 325, Top = 85, Width = 110, Text = "Удалить" };
    private readonly Button _btnSelect = new() { Left = 148, Top = 265, Width = 100, Text = "Выбрать", DialogResult = DialogResult.OK };
    private readonly Button _btnClose = new() { Left = 254, Top = 265, Width = 90, Text = "Закрыть", DialogResult = DialogResult.Cancel };

    private List<ConnectionProfile> _profiles;

    public ConnectionProfile? SelectedProfile { get; private set; }

    public ConnectionSettingsForm(ConnectionProfile? currentlySelected)
    {
        Text = "Подключения к базе данных";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(450, 305);

        Controls.Add(_list);
        Controls.Add(_btnAdd);
        Controls.Add(_btnEdit);
        Controls.Add(_btnDelete);
        Controls.Add(_btnSelect);
        Controls.Add(_btnClose);

        _profiles = ConnectionProfileStore.Load();
        RefreshList();

        if (currentlySelected != null)
        {
            var index = _profiles.FindIndex(p => p.Name == currentlySelected.Name && p.Server == currentlySelected.Server);
            if (index >= 0)
            {
                _list.SelectedIndex = index;
            }
        }

        _list.DoubleClick += (_, _) => EditSelected();
        _btnAdd.Click += (_, _) => AddNew();
        _btnEdit.Click += (_, _) => EditSelected();
        _btnDelete.Click += (_, _) => DeleteSelected();
        _btnSelect.Click += (_, e) =>
        {
            if (_list.SelectedItem is ConnectionProfile profile)
            {
                SelectedProfile = profile;
            }
            else
            {
                DialogResult = DialogResult.None;
                MessageBox.Show(this, "Выберите подключение из списка.", "Проверка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
    }

    private void RefreshList()
    {
        _list.Items.Clear();
        foreach (var profile in _profiles)
        {
            _list.Items.Add(profile);
        }
    }

    private void AddNew()
    {
        using var editForm = new ConnectionEditForm(null);
        if (editForm.ShowDialog(this) == DialogResult.OK)
        {
            _profiles.Add(editForm.Profile);
            ConnectionProfileStore.Save(_profiles);
            RefreshList();
            _list.SelectedIndex = _profiles.Count - 1;
        }
    }

    private void EditSelected()
    {
        if (_list.SelectedItem is not ConnectionProfile profile)
        {
            return;
        }

        using var editForm = new ConnectionEditForm(profile);
        if (editForm.ShowDialog(this) == DialogResult.OK)
        {
            ConnectionProfileStore.Save(_profiles);
            RefreshList();
        }
    }

    private void DeleteSelected()
    {
        if (_list.SelectedItem is not ConnectionProfile profile)
        {
            return;
        }

        if (MessageBox.Show(this, $"Удалить подключение \"{profile.Name}\"?", "Подтверждение",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
        {
            return;
        }

        _profiles.Remove(profile);
        ConnectionProfileStore.Save(_profiles);
        RefreshList();
    }
}
