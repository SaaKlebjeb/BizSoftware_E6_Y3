using InventoryManagementSystem.Events;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class SalesForm : Form
{
    private readonly ProductService _productService;
    private readonly SalesService _salesService;
    private readonly Session _session;
    private readonly string _applicationName;
    private readonly ComboBox _productPicker = new() { DropDownStyle = ComboBoxStyle.DropDownList, Name = "saleProductPicker" };
    private readonly NumericUpDown _quantityPicker = new() { Minimum = 1, Maximum = 10_000, Value = 1, Name = "saleQuantityPicker" };
    private readonly DataGridView _cartGrid = new() { Name = "saleCartGrid" };
    private readonly Label _totalLabel = new() { AutoSize = true, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
    private readonly Label _statusLabel = new() { AutoSize = true, ForeColor = Color.Firebrick };
    private readonly Button _previewButton = new() { Text = "Preview invoice", AutoSize = true };
    private readonly List<SaleLineRequest> _cart = [];
    private IReadOnlyList<Product> _products = [];

    public SalesForm(ProductService productService, SalesService salesService, Session session, string applicationName = "Inventory Management System")
    {
        _productService = productService;
        _salesService = salesService;
        _session = session;
        _applicationName = applicationName;
        Text = "Sales";
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;
        BuildUi();
        InventoryEvents.ProductChanged += OnProductsChanged;
        FormClosed += (_, _) => InventoryEvents.ProductChanged -= OnProductsChanged;
    }

    private void BuildUi()
    {
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false };
        _productPicker.Width = 260;
        _quantityPicker.Width = 80;
        var addButton = new Button { Text = "Add to sale", AutoSize = true };
        _previewButton.BackColor = Color.FromArgb(30, 115, 190);
        _previewButton.ForeColor = Color.White;
        toolbar.Controls.Add(_productPicker);
        toolbar.Controls.Add(_quantityPicker);
        toolbar.Controls.Add(addButton);
        toolbar.Controls.Add(_previewButton);
        addButton.Click += (_, _) => AddToCart();
        _previewButton.Click += async (_, _) => await PreviewInvoiceAsync();

        ConfigureGrid();
        var footer = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 64, FlowDirection = FlowDirection.RightToLeft };
        footer.Controls.Add(_totalLabel);
        footer.Controls.Add(_statusLabel);
        Controls.Add(_cartGrid);
        Controls.Add(footer);
        Controls.Add(toolbar);
    }

    private void ConfigureGrid()
    {
        _cartGrid.Dock = DockStyle.Fill;
        _cartGrid.AllowUserToAddRows = false;
        _cartGrid.ReadOnly = true;
        _cartGrid.AutoGenerateColumns = false;
        _cartGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _cartGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Product", DataPropertyName = nameof(SaleCartRow.ProductName) });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Quantity", DataPropertyName = nameof(SaleCartRow.Quantity) });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Unit price", DataPropertyName = nameof(SaleCartRow.UnitPrice), DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        _cartGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Subtotal", DataPropertyName = nameof(SaleCartRow.Subtotal), DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
    }

    private async Task LoadProductsAsync()
    {
        _products = await _productService.GetPageAsync(_session, null, null, 0, 100);
        _productPicker.DataSource = _products.Where(product => product.Quantity > 0).ToList();
        _productPicker.DisplayMember = nameof(Product.Name);
        _productPicker.ValueMember = nameof(Product.ProductId);
    }

    private void AddToCart()
    {
        if (_productPicker.SelectedItem is not Product product)
        {
            _statusLabel.Text = "Select a product.";
            return;
        }

        var quantity = decimal.ToInt32(_quantityPicker.Value);
        var currentQuantity = _cart.Where(item => item.ProductId == product.ProductId).Sum(item => item.Quantity);
        if (currentQuantity + quantity > product.Quantity)
        {
            _statusLabel.Text = $"Only {product.Quantity} units of '{product.Name}' are available.";
            return;
        }

        _cart.Add(new SaleLineRequest(product.ProductId, quantity));
        RefreshCart();
        _statusLabel.Text = string.Empty;
    }

    private async Task PreviewInvoiceAsync()
    {
        try
        {
            if (_cart.Count == 0)
            {
                _statusLabel.Text = "Add at least one product before preparing an invoice.";
                return;
            }

            var preparedSale = await _salesService.PrepareSaleAsync(_session, _cart);
            using var invoice = new InvoicePreviewForm(_salesService, _session, preparedSale, _applicationName);
            if (invoice.ShowDialog(this) == DialogResult.OK && invoice.RecordedSaleId is int saleId)
            {
                _cart.Clear();
                RefreshCart();
                _statusLabel.ForeColor = Color.DarkGreen;
                _statusLabel.Text = $"Sale #{saleId} recorded successfully.";
                await LoadProductsAsync();
            }
        }
        catch (Exception exception)
        {
            _statusLabel.ForeColor = Color.Firebrick;
            _statusLabel.Text = UserMessageFormatter.From(exception);
        }
    }

    private void RefreshCart()
    {
        var rows = _cart
            .GroupBy(item => item.ProductId)
            .Select(group =>
            {
                var product = _products.FirstOrDefault(candidate => candidate.ProductId == group.Key);
                var quantity = group.Sum(item => item.Quantity);
                var price = product?.Price ?? 0;
                return new SaleCartRow(product?.Name ?? "Unknown", quantity, price, quantity * price);
            })
            .ToList();
        _cartGrid.DataSource = rows;
        _totalLabel.Text = $"Total: {rows.Sum(row => row.Subtotal):N2}";
        _previewButton.Enabled = rows.Count > 0;
    }

    private async void OnProductsChanged(object? sender, EventArgs e)
    {
        try
        {
            await LoadProductsAsync();
            RefreshCart();
        }
        catch (Exception exception)
        {
            _statusLabel.ForeColor = Color.Firebrick;
            _statusLabel.Text = UserMessageFormatter.From(exception);
        }
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            await LoadProductsAsync();
            RefreshCart();
        }
        catch (Exception exception)
        {
            _statusLabel.Text = UserMessageFormatter.From(exception);
        }
    }
}

public sealed record SaleCartRow(string ProductName, int Quantity, decimal UnitPrice, decimal Subtotal);
