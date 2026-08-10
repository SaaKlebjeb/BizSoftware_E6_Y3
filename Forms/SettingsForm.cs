using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class SettingsForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly Session _session;
    private readonly TextBox _applicationName = new();
    private readonly NumericUpDown _lowStockDefault = new() { Minimum = 0, Maximum = 1_000 };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Color.Firebrick };

    public SettingsForm(SettingsService settingsService, Session session)
    {
        _settingsService = settingsService;
        _session = session;
        Text = "Settings";
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;
        BuildUi();
        Load += async (_, _) => await LoadSettingsAsync();
    }

    private void BuildUi()
    {
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(24) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Application name", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 0);
        layout.Controls.Add(_applicationName, 1, 0);
        layout.Controls.Add(new Label { Text = "Default low stock", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 1);
        layout.Controls.Add(_lowStockDefault, 1, 1);
        var save = new Button { Text = "Save settings", AutoSize = true };
        save.Click += async (_, _) => await SaveAsync();
        _status.Dock = DockStyle.Bottom;
        _status.Height = 34;
        Controls.Add(layout);
        Controls.Add(save);
        Controls.Add(_status);
        save.Location = new Point(24, 150);
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            _applicationName.Text = await _settingsService.GetAsync(_session, "ApplicationName") ?? "Inventory Management System";
            var threshold = await _settingsService.GetAsync(_session, "LowStockDefault");
            if (int.TryParse(threshold, out var value)) _lowStockDefault.Value = value;
        }
        catch (Exception exception) { _status.Text = UserMessageFormatter.From(exception); }
    }

    private async Task SaveAsync()
    {
        try
        {
            await _settingsService.SetAsync(_session, "ApplicationName", _applicationName.Text);
            await _settingsService.SetAsync(_session, "LowStockDefault", decimal.ToInt32(_lowStockDefault.Value).ToString());
            _status.ForeColor = Color.DarkGreen;
            _status.Text = "Settings saved.";
        }
        catch (Exception exception) { _status.Text = UserMessageFormatter.From(exception); }
    }
}
