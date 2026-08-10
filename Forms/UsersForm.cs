using InventoryManagementSystem.Events;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class UsersForm : Form
{
    private readonly UserService _userService;
    private readonly Session _session;
    private readonly DataGridView _grid = new() { AutoGenerateColumns = false, ReadOnly = true, AllowUserToAddRows = false };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.Firebrick };

    public UsersForm(UserService userService, Session session)
    {
        _userService = userService;
        _session = session;
        Text = "Users";
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;
        BuildUi();
        InventoryEvents.UserChanged += OnUserChanged;
        FormClosed += (_, _) => InventoryEvents.UserChanged -= OnUserChanged;
    }

    private void BuildUi()
    {
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48 };
        var add = new Button { Text = "Create user", AutoSize = true };
        add.Click += async (_, _) =>
        {
            using var dialog = new UserEditForm(_userService, _session);
            if (dialog.ShowDialog(this) == DialogResult.OK) await LoadAsync();
        };
        toolbar.Controls.Add(add);
        _grid.Dock = DockStyle.Fill;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Username", DataPropertyName = nameof(User.Username) });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Full name", DataPropertyName = nameof(User.FullName) });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Role", DataPropertyName = nameof(User.Role) });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Active", DataPropertyName = nameof(User.IsActive) });
        _grid.CellDoubleClick += async (_, args) => { if (args.RowIndex >= 0) await ToggleActiveAsync(); };
        _status.Dock = DockStyle.Bottom;
        _status.Height = 30;
        Controls.Add(_grid);
        Controls.Add(_status);
        Controls.Add(toolbar);
        Load += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _grid.DataSource = (await _userService.GetAllAsync(_session)).ToList();
            _status.ForeColor = Color.DarkGreen;
            _status.Text = "Double-click a user to activate or deactivate the account.";
        }
        catch (Exception exception)
        {
            _status.Text = UserMessageFormatter.From(exception);
        }
    }

    private async Task ToggleActiveAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not User user) return;
        try { await _userService.SetActiveAsync(_session, user.UserId, !user.IsActive); await LoadAsync(); }
        catch (Exception exception) { _status.Text = UserMessageFormatter.From(exception); }
    }

    private async void OnUserChanged(object? sender, EventArgs e) => await LoadAsync();
}
