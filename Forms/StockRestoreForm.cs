using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class StockRestoreForm : Form
{
    private readonly ProductService _productService;
    private readonly Session _session;
    private readonly Product _product;
    private readonly NumericUpDown _quantityInput = new()
    {
        Minimum = 1,
        Maximum = 10_000_000,
        Value = 1,
        ThousandsSeparator = true
    };
    private readonly TextBox _reasonInput = new() { Multiline = true, ScrollBars = ScrollBars.Vertical };
    private readonly Label _errorLabel = new() { AutoSize = true, ForeColor = Color.Firebrick };

    public StockRestoreForm(ProductService productService, Session session, Product product)
    {
        _productService = productService;
        _session = session;
        _product = product;
        Text = "Restore Stock";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(480, 300);
        ClientSize = new Size(520, 320);
        BuildUi();
    }

    private void BuildUi()
    {
        var saveButton = new Button { Text = "Restore", AutoSize = true, Name = "restoreStockButton" };
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        saveButton.Click += async (_, _) => await SaveAsync();
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(24)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(new Label { Text = "Product", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 0);
        layout.Controls.Add(new Label { Text = $"{_product.Name} (current: {_product.Quantity:N0})", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 1, 0);
        layout.Controls.Add(new Label { Text = "Quantity to restore", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 1);
        layout.Controls.Add(_quantityInput, 1, 1);
        layout.Controls.Add(new Label { Text = "Reason", AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, 2);
        _reasonInput.Dock = DockStyle.Top;
        _reasonInput.Height = 70;
        layout.Controls.Add(_reasonInput, 1, 2);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(24, 8, 24, 8) };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);
        _errorLabel.Dock = DockStyle.Bottom;
        _errorLabel.Height = 34;
        _errorLabel.Padding = new Padding(24, 6, 24, 0);
        Controls.Add(layout);
        Controls.Add(buttons);
        Controls.Add(_errorLabel);
    }

    private async Task SaveAsync()
    {
        try
        {
            await _productService.RestoreStockAsync(
                _session,
                _product.ProductId,
                decimal.ToInt32(_quantityInput.Value),
                _reasonInput.Text);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            _errorLabel.Text = UserMessageFormatter.From(exception);
        }
    }
}
