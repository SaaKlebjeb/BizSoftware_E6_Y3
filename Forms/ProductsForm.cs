using InventoryManagementSystem.Events;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class ProductsForm : Form
{
    private readonly ProductService _productService;
    private readonly Session _session;
    private readonly TextBox _searchBox = new() { Name = "productSearchBox", PlaceholderText = "Search name, SKU, or category" };
    private readonly ComboBox _categoryFilter = new() { Name = "categoryFilter", DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _pageSize = new() { Name = "pageSize", DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DataGridView _grid = new() { Name = "productsGrid" };
    private readonly Label _pageLabel = new() { AutoSize = true, Padding = new Padding(8, 8, 8, 0) };
    private readonly Button _previousPage = new() { Text = "Previous", AutoSize = true };
    private readonly Button _nextPage = new() { Text = "Next", AutoSize = true };
    private CancellationTokenSource? _loadCancellation;
    private int _page;
    private int _totalRows;

    public ProductsForm(ProductService productService, Session session)
    {
        _productService = productService;
        _session = session;
        Text = "Products";
        FormBorderStyle = FormBorderStyle.None;
        Dock = DockStyle.Fill;
        BuildUi();
        InventoryEvents.ProductChanged += OnInventoryChanged;
        FormClosed += (_, _) =>
        {
            InventoryEvents.ProductChanged -= OnInventoryChanged;
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
        var exportButton = new Button { Text = "Export CSV", AutoSize = true };
        toolbar.Controls.Add(_searchBox);
        toolbar.Controls.Add(_categoryFilter);
        toolbar.Controls.Add(_pageSize);
        toolbar.Controls.Add(addButton);
        toolbar.Controls.Add(exportButton);
        addButton.Click += async (_, _) => await AddProductAsync();
        exportButton.Click += async (_, _) => await ExportAsync();
        _searchBox.TextChanged += (_, _) => { _page = 0; _ = LoadAsync(); };
        _categoryFilter.SelectedIndexChanged += (_, _) => { _page = 0; _ = LoadAsync(); };
        _pageSize.SelectedIndexChanged += (_, _) => { _page = 0; _ = LoadAsync(); };

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
        contextMenu.Items.Add("Record Sale", null, (_, _) => MessageBox.Show("Sales workflow is being added in the next phase.", "Sales", MessageBoxButtons.OK, MessageBoxIcon.Information));
        contextMenu.Items.Add("View History", null, (_, _) => MessageBox.Show("History workflow is being added in the next phase.", "History", MessageBoxButtons.OK, MessageBoxIcon.Information));
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
        CsvExporter.Export(dialog.FileName,
            new[] { "SKU", "Name", "Category", "Price", "Quantity", "Low Stock Threshold" },
            products.Select(product => new object?[] { product.Sku, product.Name, product.CategoryName, product.Price, product.Quantity, product.LowStockThreshold }));
        MessageBox.Show(this, "Products exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private int PageSize => _pageSize.SelectedItem is int size ? size : 25;

    private async void OnInventoryChanged(object? sender, EventArgs e)
    {
        await LoadAsync();
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        try
        {
            await LoadCategoriesAsync();
            await LoadAsync();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, UserMessageFormatter.From(exception), "Unable to load products", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
