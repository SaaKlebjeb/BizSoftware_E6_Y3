using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class UserEditForm : Form
{
    private readonly UserService _userService;
    private readonly Session _session;
    private readonly TextBox _username = new();
    private readonly TextBox _fullName = new();
    private readonly TextBox _password = new() { UseSystemPasswordChar = true };
    private readonly TextBox _confirmation = new() { UseSystemPasswordChar = true };
    private readonly ComboBox _role = new() { DropDownStyle = ComboBoxStyle.DropDownList, DataSource = Enum.GetValues<UserRole>() };
    private readonly Label _error = new() { AutoSize = true, ForeColor = Color.Firebrick };

    public UserEditForm(UserService userService, Session session)
    {
        _userService = userService;
        _session = session;
        Text = "Create User";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 390);
        BuildUi();
    }

    private void BuildUi()
    {
        var fields = new (string Label, Control Control)[] { ("Username", _username), ("Full name", _fullName), ("Password", _password), ("Confirm", _confirmation), ("Role", _role) };
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(24) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < fields.Length; row++)
        {
            layout.Controls.Add(new Label { Text = fields[row].Label, AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, row);
            fields[row].Control.Dock = DockStyle.Top;
            layout.Controls.Add(fields[row].Control, 1, row);
        }

        var save = new Button { Text = "Create", AutoSize = true };
        var cancel = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        save.Click += async (_, _) => await SaveAsync();
        AcceptButton = save;
        CancelButton = cancel;
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(24, 8, 24, 8) };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(save);
        _error.Dock = DockStyle.Bottom;
        _error.Height = 32;
        Controls.Add(layout);
        Controls.Add(buttons);
        Controls.Add(_error);
    }

    private async Task SaveAsync()
    {
        try
        {
            await _userService.CreateAsync(_session, _username.Text, _fullName.Text, _password.Text, _confirmation.Text, (UserRole)_role.SelectedItem!);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            _error.Text = UserMessageFormatter.From(exception);
        }
    }
}
