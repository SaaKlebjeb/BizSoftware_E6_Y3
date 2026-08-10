using InventoryManagementSystem.Events;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;

namespace InventoryManagementSystem.Forms;

public sealed class DashboardForm : Form
{
    private readonly DashboardService _dashboardService;
    private readonly Session _session;
    private readonly Label _productsValue = CreateValueLabel();
    private readonly Label _lowStockValue = CreateValueLabel();
    private readonly Label _salesValue = CreateValueLabel();
    private readonly Label _topSellerValue = CreateValueLabel();
    private readonly DataGridView _previewGrid = new() { Name = "dashboardPreviewGrid" };

    public DashboardForm(DashboardService dashboardService, Session session)
    {
        _dashboardService = dashboardService;
        _session = session;
        Text = "Dashboard";
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;
        BuildUi();
        InventoryEvents.ProductChanged += OnDataChanged;
        InventoryEvents.SaleRecorded += OnDataChanged;
        FormClosed += (_, _) =>
        {
            InventoryEvents.ProductChanged -= OnDataChanged;
            InventoryEvents.SaleRecorded -= OnDataChanged;
        };
    }

    private void BuildUi()
    {
        var cards = new TableLayoutPanel { Dock = DockStyle.Top, Height = 110, ColumnCount = 4, RowCount = 1 };
        for (var index = 0; index < 4; index++) cards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        cards.Controls.Add(CreateCard("Total products", _productsValue), 0, 0);
        cards.Controls.Add(CreateCard("Low stock", _lowStockValue), 1, 0);
        cards.Controls.Add(CreateCard("Today's sales", _salesValue), 2, 0);
        cards.Controls.Add(CreateCard("Top seller", _topSellerValue), 3, 0);

        _previewGrid.Dock = DockStyle.Fill;
        _previewGrid.AllowUserToAddRows = false;
        _previewGrid.ReadOnly = true;
        _previewGrid.AutoGenerateColumns = false;
        _previewGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _previewGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "SKU", DataPropertyName = nameof(Product.Sku) });
        _previewGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = nameof(Product.Name) });
        _previewGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Category", DataPropertyName = nameof(Product.CategoryName) });
        _previewGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Quantity", DataPropertyName = nameof(Product.Quantity) });
        _previewGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Price", DataPropertyName = nameof(Product.Price), DefaultCellStyle = new DataGridViewCellStyle { Format = "N2" } });
        Controls.Add(_previewGrid);
        Controls.Add(cards);
    }

    private async Task LoadAsync()
    {
        var summary = await _dashboardService.GetSummaryAsync(_session);
        _productsValue.Text = summary.TotalProducts.ToString("N0");
        _lowStockValue.Text = summary.LowStockCount.ToString("N0");
        _salesValue.Text = summary.TodaySales.ToString("N2");
        _topSellerValue.Text = summary.TopSeller;
        var products = await _dashboardService.GetProductPreviewAsync(_session, 10);
        _previewGrid.DataSource = products.ToList();
        foreach (DataGridViewRow row in _previewGrid.Rows)
        {
            if (row.DataBoundItem is Product product && product.IsLowStock)
            {
                row.DefaultCellStyle.BackColor = Color.MistyRose;
            }
        }
    }

    private async void OnDataChanged(object? sender, EventArgs e)
    {
        try { await LoadAsync(); } catch (ObjectDisposedException) { }
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try { await LoadAsync(); } catch (Exception exception) { MessageBox.Show(this, exception.Message, "Unable to load dashboard", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static Panel CreateCard(string title, Label value)
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new Padding(6), Padding = new Padding(12) };
        panel.Controls.Add(new Label { Text = title, Dock = DockStyle.Top, AutoSize = true });
        panel.Controls.Add(value);
        value.Dock = DockStyle.Fill;
        value.TextAlign = ContentAlignment.MiddleLeft;
        return panel;
    }

    private static Label CreateValueLabel() => new() { Font = new Font("Segoe UI", 18, FontStyle.Bold) };
}
