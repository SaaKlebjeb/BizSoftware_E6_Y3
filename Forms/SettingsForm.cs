using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class SettingsForm : Form
{
    private readonly SettingsService _settingsService;
    private readonly Session _session;
    private readonly TextBox _applicationName = new();
    private readonly NumericUpDown _lowStockDefault = new() { Minimum = 0, Maximum = 1_000 };
    private readonly NumericUpDown _defaultPageSize = new() { Minimum = 1, Maximum = 500, Value = 25 };
    private readonly TextBox _currencySymbol = new() { Width = 120 };
    private readonly TextBox _receiptFooter = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, Height = 72, Width = 320 };
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
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Padding = new Padding(24) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowCount = 6;
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.Controls.Add(new Label { Text = "Application name", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 0);
        layout.Controls.Add(_applicationName, 1, 0);
        layout.Controls.Add(new Label { Text = "Default low stock", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 1);
        layout.Controls.Add(_lowStockDefault, 1, 1);
        layout.Controls.Add(new Label { Text = "Default page size", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 2);
        layout.Controls.Add(_defaultPageSize, 1, 2);
        layout.Controls.Add(new Label { Text = "Currency symbol", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 3);
        layout.Controls.Add(_currencySymbol, 1, 3);
        layout.Controls.Add(new Label { Text = "Receipt footer", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 4);
        layout.Controls.Add(_receiptFooter, 1, 4);
        layout.Controls.Add(new Label
        {
            Text = "Optional text printed at the bottom of the invoice, such as a thank-you note or contact line.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            MaximumSize = new Size(320, 0),
            Padding = new Padding(0, 4, 0, 0)
        }, 1, 5);
        var save = new Button { Text = "Save settings", AutoSize = true };
        save.Click += async (_, _) => await SaveAsync();
        layout.Controls.Add(save, 1, 6);
        _status.Dock = DockStyle.Bottom;
        _status.Height = 34;
        Controls.Add(layout);
        Controls.Add(_status);
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            _applicationName.Text = await _settingsService.GetAsync(_session, "ApplicationName") ?? "Inventory Management System";
            var threshold = await _settingsService.GetAsync(_session, "LowStockDefault");
            if (int.TryParse(threshold, out var value)) _lowStockDefault.Value = value;

            var pageSize = await _settingsService.GetAsync(_session, "DefaultPageSize");
            if (int.TryParse(pageSize, out var parsedPageSize) && parsedPageSize >= _defaultPageSize.Minimum && parsedPageSize <= _defaultPageSize.Maximum)
            {
                _defaultPageSize.Value = parsedPageSize;
            }

            _currencySymbol.Text = await _settingsService.GetAsync(_session, "CurrencySymbol") ?? "$";
            _receiptFooter.Text = await _settingsService.GetAsync(_session, "ReceiptFooter") ?? string.Empty;
        }
        catch (Exception exception) { _status.Text = UserMessageFormatter.From(exception); }
    }

    private async Task SaveAsync()
    {
        try
        {
            await _settingsService.SaveManyAsync(_session, new[]
            {
                ("ApplicationName", _applicationName.Text, false),
                ("LowStockDefault", decimal.ToInt32(_lowStockDefault.Value).ToString(), false),
                ("DefaultPageSize", decimal.ToInt32(_defaultPageSize.Value).ToString(), false),
                ("CurrencySymbol", _currencySymbol.Text, false),
                ("ReceiptFooter", _receiptFooter.Text, true)
            });
            _status.ForeColor = Color.DarkGreen;
            _status.Text = "Settings saved.";
        }
        catch (Exception exception) { _status.Text = UserMessageFormatter.From(exception); }
    }
}
