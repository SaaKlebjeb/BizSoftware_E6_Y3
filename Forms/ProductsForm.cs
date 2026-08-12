using InventoryManagementSystem.Events;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;
using System.ComponentModel;
using System.Drawing.Printing;

namespace InventoryManagementSystem.Forms;

public sealed class ProductsForm : Form
{
    private readonly ProductService _productService;
    private readonly AuditLogService _auditLogService;
    private readonly SettingsService _settingsService;
    private readonly Session _session;
    private readonly TextBox _searchBox = new() { Name = "productSearchBox", PlaceholderText = "Search name, SKU, or category" };
    private readonly ComboBox _categoryFilter = new() { Name = "categoryFilter", DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _pageSize = new() { Name = "pageSize", DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView _grid = new() { Name = "productsGrid" };
    private readonly PrintDocument _printDocument = new();
    private readonly Label _pageLabel = new() { AutoSize = true, Padding = new Padding(8, 8, 8, 0) };
    private readonly Button _previousPage = new() { Text = "Previous", AutoSize = true };
    private readonly Button _nextPage = new() { Text = "Next", AutoSize = true };
    private CancellationTokenSource? _loadCancellation;
    private bool _suppressFilterEvents = true;
    private int _page;
    private int _totalRows;
    private IReadOnlyList<Product> _printProducts = [];
    private int _printIndex;

    public ProductsForm(ProductService productService, AuditLogService auditLogService, SettingsService settingsService, Session session)
    {
        _productService = productService;
        _auditLogService = auditLogService;
        _settingsService = settingsService;
        _session = session;
        Text = "Products";
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;
        BuildUi();
        InventoryEvents.ProductChanged += OnInventoryChanged;
        InventoryEvents.CategoryChanged += OnCategoryChanged;
        InventoryEvents.SettingsChanged += OnSettingsChanged;
        FormClosed += (_, _) =>
        {
            InventoryEvents.ProductChanged -= OnInventoryChanged;
            InventoryEvents.CategoryChanged -= OnCategoryChanged;
            InventoryEvents.SettingsChanged -= OnSettingsChanged;
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
        };
    }

    private void BuildUi()
    {
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, WrapContents = false, Padding = new Padding(0, 0, 0, 8) };
        _searchBox.Width = 250;
        _categoryFilter.Width = 150;
        _pageSize.Width = 70;
        _pageSize.Items.AddRange(new object[] { 25, 50, 100 });
        _pageSize.SelectedItem = 25;
        var addButton = new Button { Text = "Add Product", AutoSize = true, Visible = _session.IsAdmin };
        var restoreButton = new Button { Text = "Restore Stock", AutoSize = true, Visible = _session.IsAdmin };
        var templateButton = new Button { Text = "Download Template", AutoSize = true, Visible = _session.IsAdmin };
        var importButton = new Button { Text = "Import Excel", AutoSize = true, Visible = _session.IsAdmin };
        var exportButton = new Button { Text = "Export CSV", AutoSize = true };
        var excelButton = new Button { Text = "Export Excel", AutoSize = true };
        var printButton = new Button { Text = "Print preview", AutoSize = true };
        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(_categoryFilter);
        toolbar.Controls.Add(_pageSize);
        toolbar.Controls.Add(addButton);
        toolbar.Controls.Add(restoreButton);
        toolbar.Controls.Add(templateButton);
        toolbar.Controls.Add(importButton);
        toolbar.Controls.Add(exportButton);
        toolbar.Controls.Add(excelButton);
        toolbar.Controls.Add(printButton);
        addButton.Click += async (_, _) => await AddProductAsync();
        restoreButton.Click += async (_, _) => await RestoreSelectedAsync();
        templateButton.Click += (_, _) => DownloadImportTemplate();
        importButton.Click += async (_, _) => await ImportProductsAsync();
        exportButton.Click += async (_, _) => await ExportAsync();
        excelButton.Click += async (_, _) => await ExportExcelAsync();
        printButton.Click += async (_, _) => await PrintAsync();
        _searchBox.TextChanged += (_, _) => RefreshFilterChanged();
        _categoryFilter.SelectedIndexChanged += (_, _) => RefreshFilterChanged();
        _pageSize.SelectedIndexChanged += (_, _) => RefreshFilterChanged();

        ConfigureGrid();
        var paging = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, FlowDirection = FlowDirection.RightToLeft, WrapContents = false };
        paging.Controls.Add(_nextPage);
        paging.Controls.Add(_previousPage);
        paging.Controls.Add(_pageLabel);
        _previousPage.Click += async (_, _) => { if (_page > 0) { _page--; await LoadAsync(); } };
        _nextPage.Click += async (_, _) => { if ((_page + 1) * PageSize < _totalRows) { _page++; await LoadAsync(); } };

        Controls.Add(_grid);
        Controls.Add(paging);
        Controls.Add(toolbar);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.ReadOnly = true;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        _grid.GridColor = Color.FromArgb(210, 218, 226);
        _grid.RowHeadersVisible = false;
        _grid.RowTemplate.Height = 30;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersHeight = 34;
        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(30, 115, 190), ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Alignment = DataGridViewContentAlignment.MiddleLeft };
        _grid.DefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.White, ForeColor = Color.FromArgb(25, 25, 25), SelectionBackColor = Color.FromArgb(44, 125, 194), SelectionForeColor = Color.White, Padding = new Padding(5, 0, 5, 0) };
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 247, 250) };
        AddColumn("SKU", nameof(Product.Sku));
        AddColumn("Name", nameof(Product.Name));
        AddColumn("Category", nameof(Product.CategoryName));
        AddColumn("Price", nameof(Product.Price), "N2");
        AddColumn("Quantity", nameof(Product.Quantity));
        AddColumn("Low-stock threshold", nameof(Product.LowStockThreshold));
        _grid.CellDoubleClick += async (_, args) => { if (args.RowIndex >= 0) await EditSelectedAsync(); };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Edit", null, async (_, _) => await EditSelectedAsync()).Enabled = _session.IsAdmin;
        contextMenu.Items.Add("Delete", null, async (_, _) => await DeleteSelectedAsync()).Enabled = _session.IsAdmin;
        contextMenu.Items.Add("Restore Stock", null, async (_, _) => await RestoreSelectedAsync()).Enabled = _session.IsAdmin;
        contextMenu.Items.Add("Record Sale", null, (_, _) => MessageBox.Show("Sales workflow is being added in the next phase.", "Sales", MessageBoxButtons.OK, MessageBoxIcon.Information));
        contextMenu.Items.Add("View History", null, async (_, _) => await ShowHistoryAsync()).Enabled = _session.IsAdmin;
        _grid.ContextMenuStrip = contextMenu;
    }

    private void AddColumn(string header, string property, string? format = null)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            SortMode = DataGridViewColumnSortMode.Automatic,
            DefaultCellStyle = format is null ? null : new DataGridViewCellStyle { Format = format }
        });
    }

    private async Task LoadAsync()
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        try
        {
            _loadCancellation?.Cancel();
            _loadCancellation?.Dispose();
            _loadCancellation = new CancellationTokenSource();
            var cancellationToken = _loadCancellation.Token;
            var categoryId = _categoryFilter.SelectedValue is int selectedCategoryId && selectedCategoryId > 0 ? (int?)selectedCategoryId : null;
            _totalRows = await _productService.CountAsync(_session, _searchBox.Text, categoryId, cancellationToken);
            var products = await _productService.GetPageAsync(_session, _searchBox.Text, categoryId, _page * PageSize, PageSize, cancellationToken);
            _grid.DataSource = products.ToList();
            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.DataBoundItem is Product product && product.IsLowStock)
                {
                    row.DefaultCellStyle.BackColor = Color.MistyRose;
                }
            }

            var totalPages = Math.Max(1, (int)Math.Ceiling(_totalRows / (double)PageSize));
            _pageLabel.Text = $"Page {_page + 1} of {totalPages} ({_totalRows} products)";
            _previousPage.Enabled = _page > 0;
            _nextPage.Enabled = (_page + 1) * PageSize < _totalRows;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, UserMessageFormatter.From(exception), "Unable to load products", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        var categories = await _productService.GetCategoriesAsync(_session);
        var filterItems = new List<Category> { new() { CategoryId = 0, Name = "All categories" } };
        filterItems.AddRange(categories);
        _categoryFilter.DataSource = filterItems;
        _categoryFilter.DisplayMember = nameof(Category.Name);
        _categoryFilter.ValueMember = nameof(Category.CategoryId);
    }

    private async Task LoadDisplaySettingsAsync()
    {
        var pageSize = await _settingsService.GetAsync("DefaultPageSize");
        if (int.TryParse(pageSize, out var parsedPageSize) && parsedPageSize >= 1 && parsedPageSize <= 500)
        {
            if (!_pageSize.Items.Contains(parsedPageSize))
            {
                _pageSize.Items.Add(parsedPageSize);
            }

            _pageSize.SelectedItem = parsedPageSize;
        }
        else if (_pageSize.SelectedItem is null)
        {
            _pageSize.SelectedItem = 25;
        }
    }

    private void RefreshFilterChanged()
    {
        if (_suppressFilterEvents)
        {
            return;
        }

        _page = 0;
        _ = LoadAsync();
    }

    private async Task AddProductAsync()
    {
        using var dialog = new ProductEditForm(_productService, _session);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadAsync();
        }
    }

    private async Task EditSelectedAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Product product)
        {
            return;
        }

        using var dialog = new ProductEditForm(_productService, _session, product);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadAsync();
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Product product)
        {
            return;
        }

        if (MessageBox.Show(this, $"Delete '{product.Name}'?", "Confirm deletion", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
        {
            return;
        }

        try
        {
            await _productService.DeleteAsync(_session, product.ProductId);
            await LoadAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "Unable to delete product", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private async Task RestoreSelectedAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Product product)
        {
            MessageBox.Show(this, "Select a product first.", "Restore stock", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new StockRestoreForm(_productService, _session, product);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await LoadAsync();
        }
    }

    private async void DownloadImportTemplate()
    {
        using var dialog = new SaveFileDialog { Filter = "Excel workbook (*.xlsx)|*.xlsx", FileName = "product-import-template.xlsx" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            await _productService.ExportImportTemplateAsync(_session, dialog.FileName);
            MessageBox.Show(this, "Product import template downloaded successfully.", "Template", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, UserMessageFormatter.From(exception), "Unable to download template", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ImportProductsAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Excel workbook (*.xlsx)|*.xlsx",
            Title = "Select product import file",
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        using var previewForm = new ImportPreviewForm(_productService, _session, dialog.FileName);
        if (previewForm.ShowDialog(this) == DialogResult.OK)
        {
            await LoadAsync();
        }
    }

    private async Task ShowHistoryAsync()
    {
        if (_grid.CurrentRow?.DataBoundItem is not Product product)
        {
            MessageBox.Show(this, "Select a product first.", "History", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new AuditLogsForm(_auditLogService, _session, "Product", product.ProductId)
        {
            FormBorderStyle = FormBorderStyle.Sizable,
            StartPosition = FormStartPosition.CenterParent,
            MinimumSize = new Size(900, 420),
            ClientSize = new Size(1_100, 560)
        };
        dialog.ShowDialog(this);
        await Task.CompletedTask;
    }

    private async Task ExportAsync()
    {
        using var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "products.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var categoryId = _categoryFilter.SelectedValue is int selectedCategoryId && selectedCategoryId > 0 ? (int?)selectedCategoryId : null;
        var products = new List<Product>();
        for (var offset = 0; offset < _totalRows; offset += 100)
        {
            products.AddRange(await _productService.GetPageAsync(_session, _searchBox.Text, categoryId, offset, 100));
        }
        var rows = BuildProductExportRows(products);
        CsvExporter.Export(dialog.FileName,
            new[] { "SKU", "Name", "Category", "Price", "Quantity", "Low Stock Threshold" },
            rows);
        MessageBox.Show(this, "Products exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task ExportExcelAsync()
    {
        using var dialog = new SaveFileDialog { Filter = "Excel workbook (*.xlsx)|*.xlsx", FileName = "products.xlsx" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var products = await LoadProductsForExportAsync();
        SpreadsheetExporter.ExportXlsx(
            dialog.FileName,
            "Products",
            new[] { "SKU", "Name", "Category", "Price", "Quantity", "Low Stock Threshold" },
            BuildProductExportRows(products));
        MessageBox.Show(this, "Formatted spreadsheet exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task PrintAsync()
    {
        try
        {
            if (PrinterSettings.InstalledPrinters.Count == 0)
            {
                ShowPrinterUnavailableMessage("No printer is installed.");
                return;
            }

            _printProducts = await LoadProductsForExportAsync();
            _printIndex = 0;
            _printDocument.PrintController = new PreviewPrintController();
            _printDocument.PrintPage -= PrintDocumentOnPrintPage;
            _printDocument.PrintPage += PrintDocumentOnPrintPage;
            using var preview = new PrintPreviewDialog { Document = _printDocument, Width = 1_000, Height = 700 };
            preview.ShowDialog(this);
        }
        catch (InvalidPrinterException)
        {
            ShowPrinterUnavailableMessage("No valid printer is available.");
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1722 || exception.Message.Contains("RPC", StringComparison.OrdinalIgnoreCase))
        {
            ShowPrinterUnavailableMessage("The Windows Print Spooler service is unavailable.");
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, UserMessageFormatter.From(exception), "Unable to print products", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowPrinterUnavailableMessage(string reason) =>
        MessageBox.Show(this, $"{reason}\n\nStart the Windows Print Spooler service or install Microsoft Print to PDF, then try again.", "Print unavailable", MessageBoxButtons.OK, MessageBoxIcon.Information);

    private void PrintDocumentOnPrintPage(object? sender, PrintPageEventArgs e)
    {
        using var font = new Font("Segoe UI", 8);
        using var titleFont = new Font("Segoe UI", 13, FontStyle.Bold);
        var y = e.MarginBounds.Top;
        e.Graphics?.DrawString("Products", titleFont, Brushes.Black, e.MarginBounds.Left, y);
        y += 30;
        e.Graphics?.DrawString("SKU                 Name                         Category             Price       Quantity", font, Brushes.Black, e.MarginBounds.Left, y);
        y += 20;

        while (_printIndex < _printProducts.Count)
        {
            var product = _printProducts[_printIndex++];
            var line = $"{product.Sku,-20}{product.Name,-30}{product.CategoryName,-20}{product.Price,10:N2}{product.Quantity,12:N0}";
            e.Graphics?.DrawString(line, font, Brushes.Black, e.MarginBounds.Left, y);
            y += 18;
            if (y + 18 > e.MarginBounds.Bottom)
            {
                e.HasMorePages = _printIndex < _printProducts.Count;
                return;
            }
        }

        e.HasMorePages = false;
    }

    private async Task<List<Product>> LoadProductsForExportAsync()
    {
        var categoryId = _categoryFilter.SelectedValue is int selectedCategoryId && selectedCategoryId > 0 ? (int?)selectedCategoryId : null;
        var products = new List<Product>();
        for (var offset = 0; offset < _totalRows; offset += 100)
        {
            products.AddRange(await _productService.GetPageAsync(_session, _searchBox.Text, categoryId, offset, 100));
        }

        return products;
    }

    private static IEnumerable<object?[]> BuildProductExportRows(IEnumerable<Product> products) =>
        products.Select(product => new object?[]
        {
            product.Sku,
            product.Name,
            product.CategoryName,
            product.Price.ToString("N2"),
            product.Quantity,
            product.LowStockThreshold
        });

    private int PageSize => _pageSize.SelectedItem is int size ? size : 25;

    private async void OnInventoryChanged(object? sender, EventArgs e)
    {
        await LoadAsync();
    }

    private async void OnCategoryChanged(object? sender, EventArgs e)
    {
        try
        {
            _suppressFilterEvents = true;
            await LoadCategoriesAsync();
            _suppressFilterEvents = false;
            _page = 0;
            await LoadAsync();
        }
        catch (Exception exception)
        {
            _suppressFilterEvents = false;
            MessageBox.Show(this, UserMessageFormatter.From(exception), "Unable to refresh categories", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnSettingsChanged(object? sender, EventArgs e)
    {
        try
        {
            _suppressFilterEvents = true;
            await LoadDisplaySettingsAsync();
            _suppressFilterEvents = false;
            _page = 0;
            await LoadAsync();
        }
        catch (Exception exception)
        {
            _suppressFilterEvents = false;
            MessageBox.Show(this, UserMessageFormatter.From(exception), "Unable to refresh settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            _suppressFilterEvents = true;
            await LoadDisplaySettingsAsync();
            await LoadCategoriesAsync();
            _suppressFilterEvents = false;
            await LoadAsync();
        }
        catch (Exception exception)
        {
            _suppressFilterEvents = false;
            MessageBox.Show(this, UserMessageFormatter.From(exception), "Unable to load products", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
