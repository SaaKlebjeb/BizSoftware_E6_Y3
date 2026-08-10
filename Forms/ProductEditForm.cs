using InventoryManagementSystem.Models;
using InventoryManagementSystem.Services;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Forms;

public sealed class ProductEditForm : Form
{
    private readonly ProductService _productService;
    private readonly Session _session;
    private readonly Product? _existingProduct;
    private readonly TextBox _skuTextBox = new() { Name = "skuTextBox" };
    private readonly TextBox _nameTextBox = new() { Name = "nameTextBox" };
    private readonly ComboBox _categoryComboBox = new() { Name = "categoryComboBox", DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _priceInput = CreateNumericInput(2);
    private readonly NumericUpDown _quantityInput = CreateNumericInput(0);
    private readonly NumericUpDown _thresholdInput = CreateNumericInput(0);
    private readonly Label _errorLabel = new() { AutoSize = true, ForeColor = Color.Firebrick };
    private IReadOnlyList<Category> _categories = [];

    public ProductEditForm(ProductService productService, Session session, Product? existingProduct = null)
    {
        _productService = productService;
        _session = session;
        _existingProduct = existingProduct;
        Text = existingProduct is null ? "Add Product" : "Edit Product";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(500, 420);
        ClientSize = new Size(560, 470);
        BuildUi();
        Load += async (_, _) => await LoadCategoriesAsync();
    }

    private void BuildUi()
    {
        var saveButton = new Button { Text = "Save", AutoSize = true, Name = "saveProductButton" };
        var cancelButton = new Button { Text = "Cancel", AutoSize = true, DialogResult = DialogResult.Cancel };
        saveButton.Click += async (_, _) => await SaveAsync();
        AcceptButton = saveButton;
        CancelButton = cancelButton;

        var fields = new (string Label, Control Control)[]
        {
            ("SKU", _skuTextBox),
            ("Name", _nameTextBox),
            ("Category", _categoryComboBox),
            ("Price", _priceInput),
            ("Quantity", _quantityInput),
            ("Low-stock threshold", _thresholdInput)
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(24) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var row = 0; row < fields.Length; row++)
        {
            layout.Controls.Add(new Label { Text = fields[row].Label, AutoSize = true, Padding = new Padding(0, 7, 0, 0) }, 0, row);
            fields[row].Control.Dock = DockStyle.Top;
            layout.Controls.Add(fields[row].Control, 1, row);
        }

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 50, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(24, 8, 24, 8) };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(saveButton);
        _errorLabel.Dock = DockStyle.Bottom;
        _errorLabel.Height = 34;
        _errorLabel.Padding = new Padding(24, 6, 24, 0);
        Controls.Add(layout);
        Controls.Add(buttons);
        Controls.Add(_errorLabel);

        if (_existingProduct is not null)
        {
            _skuTextBox.Text = _existingProduct.Sku;
            _nameTextBox.Text = _existingProduct.Name;
            _priceInput.Value = _existingProduct.Price;
            _quantityInput.Value = _existingProduct.Quantity;
            _thresholdInput.Value = _existingProduct.LowStockThreshold;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            _categories = await _productService.GetCategoriesAsync(_session);
            _categoryComboBox.DataSource = _categories.ToList();
            _categoryComboBox.DisplayMember = nameof(Category.Name);
            _categoryComboBox.ValueMember = nameof(Category.CategoryId);
            if (_existingProduct is not null)
            {
                _categoryComboBox.SelectedValue = _existingProduct.CategoryId;
            }
        }
        catch (Exception exception)
        {
            _errorLabel.Text = UserMessageFormatter.From(exception);
        }
    }

    private async Task SaveAsync()
    {
        try
        {
            if (_categoryComboBox.SelectedValue is not int categoryId)
            {
                throw new ArgumentException("Select a category.");
            }

            var product = new Product
            {
                ProductId = _existingProduct?.ProductId ?? 0,
                Sku = _skuTextBox.Text,
                Name = _nameTextBox.Text,
                CategoryId = categoryId,
                Price = _priceInput.Value,
                Quantity = decimal.ToInt32(_quantityInput.Value),
                LowStockThreshold = decimal.ToInt32(_thresholdInput.Value)
            };
            if (_existingProduct is null)
            {
                await _productService.CreateAsync(_session, product);
            }
            else
            {
                await _productService.UpdateAsync(_session, product);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            _errorLabel.Text = UserMessageFormatter.From(exception);
        }
    }

    private static NumericUpDown CreateNumericInput(int decimalPlaces) => new()
    {
        DecimalPlaces = decimalPlaces,
        Minimum = 0,
        Maximum = decimalPlaces == 0 ? 10_000_000 : 100_000_000,
        Increment = decimalPlaces == 0 ? 1 : 0.01m,
        ThousandsSeparator = true
    };
}
