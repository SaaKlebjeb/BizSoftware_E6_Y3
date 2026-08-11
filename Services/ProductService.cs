using InventoryManagementSystem.Events;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Repositories;
using InventoryManagementSystem.Utils;
using System.Globalization;

namespace InventoryManagementSystem.Services;

public sealed class ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository, AuthorizationService authorizationService)
{
    private static readonly string[] ProductImportHeaders = ["SKU", "Product Name", "Category", "Price", "Quantity", "Low Stock Threshold"];

    public Task<IReadOnlyList<Product>> GetPageAsync(Session session, string? search, int? categoryId, int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (offset < 0 || pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be between 1 and 100.");
        }

        return productRepository.GetPageAsync(search, categoryId, offset, pageSize, cancellationToken);
    }

    public Task<int> CountAsync(Session session, string? search, int? categoryId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        return productRepository.CountAsync(search, categoryId, cancellationToken);
    }

    public Task<IReadOnlyList<Category>> GetCategoriesAsync(Session session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        return categoryRepository.GetAllAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(Session session, Product product, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        Validate(product, isNew: true);
        var productId = await productRepository.CreateAsync(product, cancellationToken);
        InventoryEvents.RaiseProductChanged();
        return productId;
    }

    public void ExportImportTemplate(Session session, string filePath)
    {
        authorizationService.EnsureAdmin(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        var categories = categoryRepository.GetAllAsync().GetAwaiter().GetResult();
        var categoryRows = categories
            .Select(category => new object?[] { category.Name })
            .ToList();

        IReadOnlyList<SpreadsheetExporter.SpreadsheetDataValidation> dataValidations = categoryRows.Count == 0
            ? []
            : [new SpreadsheetExporter.SpreadsheetDataValidation("C4:C1048576", $"'Categories'!$A$2:$A${categoryRows.Count + 1}")];

        SpreadsheetExporter.ExportWorkbook(
            filePath,
            [
                new SpreadsheetExporter.SpreadsheetSheet(
                    "Products",
                    ProductImportHeaders,
                    [],
                    "Product Import Template",
                    "Enter one product per row. SKU may be blank for automatic generation. Select a category from the dropdown in the Category column.",
                    dataValidations),
                new SpreadsheetExporter.SpreadsheetSheet(
                    "Categories",
                    ["Category"],
                    categoryRows)
            ]);
    }

    public async Task<ProductImportResult> ImportFromExcelAsync(Session session, string filePath, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var rows = SpreadsheetImporter.ReadFirstSheet(filePath);
        var headerRowIndex = FindHeaderRow(rows);
        if (headerRowIndex < 0)
        {
            return new ProductImportResult(0, ["The Excel file must contain these columns: SKU, Product Name, Category, Price, Quantity, Low Stock Threshold."]);
        }

        var headerMap = BuildHeaderMap(rows[headerRowIndex]);
        var categories = await categoryRepository.GetAllAsync(cancellationToken);
        var categoryLookup = BuildCategoryLookup(categories);
        var products = new List<(int RowNumber, Product Product)>();
        var errors = new List<string>();

        for (var rowIndex = headerRowIndex + 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            try
            {
                var sku = GetCell(row, headerMap, "sku").Trim();
                var name = GetCell(row, headerMap, "name").Trim();
                var categoryName = GetCell(row, headerMap, "category").Trim();
                var price = ParseDecimal(GetCell(row, headerMap, "price"), "Price");
                var quantity = ParseWholeNumber(GetCell(row, headerMap, "quantity"), "Quantity");
                var lowStockThreshold = ParseWholeNumber(GetCell(row, headerMap, "lowStockThreshold"), "Low Stock Threshold");

                var category = TryFindCategory(categoryLookup, categoryName);
                if (category is null)
                {
                    throw new ArgumentException($"Category '{categoryName}' does not exist.");
                }

                var product = new Product
                {
                    Sku = sku,
                    Name = name,
                    CategoryId = category.CategoryId,
                    Price = price,
                    Quantity = quantity,
                    LowStockThreshold = lowStockThreshold
                };
                Validate(product, isNew: true);
                products.Add((rowIndex + 1, product));
            }
            catch (Exception exception) when (exception is ArgumentException or FormatException or OverflowException)
            {
                errors.Add($"Row {rowIndex + 1}: {exception.Message}");
            }
        }

        var duplicateSkus = products
            .Where(item => !string.IsNullOrWhiteSpace(item.Product.Sku))
            .GroupBy(item => item.Product.Sku.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();
        if (duplicateSkus.Count > 0)
        {
            errors.Add($"Duplicate SKU in import file: {string.Join(", ", duplicateSkus)}.");
        }

        var existingSkuSet = new HashSet<string>(
            await productRepository.FindExistingSkusAsync(
                products.Select(item => item.Product.Sku),
                cancellationToken),
            StringComparer.OrdinalIgnoreCase);
        foreach (var (rowNumber, product) in products
                     .Where(item => !string.IsNullOrWhiteSpace(item.Product.Sku) && existingSkuSet.Contains(item.Product.Sku.Trim())))
        {
            errors.Add($"Row {rowNumber}: SKU '{product.Sku.Trim()}' already exists in the system.");
        }

        if (errors.Count > 0)
        {
            return new ProductImportResult(0, errors);
        }

        var importedCount = await productRepository.CreateManyAsync(products.Select(item => item.Product).ToList(), cancellationToken);
        if (importedCount > 0)
        {
            InventoryEvents.RaiseProductChanged();
        }

        return new ProductImportResult(importedCount, []);
    }

    public async Task UpdateAsync(Session session, Product product, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        Validate(product, isNew: false);
        await productRepository.UpdateAsync(product, cancellationToken);
        InventoryEvents.RaiseProductChanged();
    }

    public async Task RestoreStockAsync(Session session, int productId, int quantity, string reason, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }

        if (quantity <= 0)
        {
            throw new ArgumentException("The restore quantity must be greater than zero.", nameof(quantity));
        }

        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 1_000)
        {
            throw new ArgumentException("A reason is required and must be no longer than 1,000 characters.", nameof(reason));
        }

        await productRepository.RestoreStockAsync(productId, quantity, session.UserId, reason.Trim(), cancellationToken);
        InventoryEvents.RaiseProductChanged();
    }

    public async Task DeleteAsync(Session session, int productId, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        if (await productRepository.HasSalesAsync(productId, cancellationToken))
        {
            throw new InvalidOperationException("This product has historical sales and cannot be deleted. Archive it instead.");
        }

        await productRepository.DeleteAsync(productId, cancellationToken);
        InventoryEvents.RaiseProductChanged();
    }

    private static void Validate(Product product, bool isNew)
    {
        if ((!isNew && string.IsNullOrWhiteSpace(product.Sku)) || product.Sku.Length > 50)
        {
            throw new ArgumentException("SKU must be no longer than 50 characters. Leave it blank only when creating a product to generate it automatically.");
        }

        if (string.IsNullOrWhiteSpace(product.Name) || product.Name.Length > 200)
        {
            throw new ArgumentException("Product name is required and must be no longer than 200 characters.");
        }

        if (product.CategoryId <= 0 || product.Price < 0 || product.Quantity < 0 || product.LowStockThreshold < 0)
        {
            throw new ArgumentException("Category, price, quantity, and low-stock threshold must contain valid non-negative values.");
        }
    }

    private static int FindHeaderRow(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        for (var index = 0; index < rows.Count; index++)
        {
            var headerMap = BuildHeaderMap(rows[index]);
            if (headerMap.ContainsKey("name") &&
                headerMap.ContainsKey("category") &&
                headerMap.ContainsKey("price") &&
                headerMap.ContainsKey("quantity") &&
                headerMap.ContainsKey("lowStockThreshold"))
            {
                return index;
            }
        }

        return -1;
    }

    private static Dictionary<string, int> BuildHeaderMap(IReadOnlyList<string> headers)
    {
        var map = new Dictionary<string, int>();
        for (var index = 0; index < headers.Count; index++)
        {
            var normalizedHeader = NormalizeHeader(headers[index]);
            var field = normalizedHeader switch
            {
                "sku" => "sku",
                "name" or "product" or "productname" => "name",
                "category" or "categoryname" => "category",
                "price" or "unitprice" => "price",
                "quantity" or "quantities" or "qty" => "quantity",
                "lowstock" or "lowstockthreshold" or "lowstockthreshole" or "threshold" => "lowStockThreshold",
                _ => string.Empty
            };

            if (!string.IsNullOrEmpty(field) && !map.ContainsKey(field))
            {
                map[field] = index;
            }
        }

        return map;
    }

    private static string GetCell(IReadOnlyList<string> row, IReadOnlyDictionary<string, int> headerMap, string field)
    {
        if (!headerMap.TryGetValue(field, out var index) || index >= row.Count)
        {
            return string.Empty;
        }

        return row[index];
    }

    private static decimal ParseDecimal(string value, string fieldName)
    {
        if (decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.CurrentCulture, out var currentValue) ||
            decimal.TryParse(value, NumberStyles.Number | NumberStyles.AllowCurrencySymbol, CultureInfo.InvariantCulture, out currentValue))
        {
            return currentValue;
        }

        throw new FormatException($"{fieldName} must be a valid number.");
    }

    private static int ParseWholeNumber(string value, string fieldName)
    {
        if ((decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out var currentValue) ||
             decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out currentValue)) &&
            currentValue == decimal.Truncate(currentValue) &&
            currentValue >= int.MinValue &&
            currentValue <= int.MaxValue)
        {
            return decimal.ToInt32(currentValue);
        }

        throw new FormatException($"{fieldName} must be a valid whole number.");
    }

    private static string NormalizeHeader(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    private static (IReadOnlyDictionary<string, Category> ByExactName, IReadOnlyDictionary<string, Category> ByNormalizedName) BuildCategoryLookup(IReadOnlyList<Category> categories)
    {
        var byExactName = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        var byNormalizedName = new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        foreach (var category in categories)
        {
            byExactName.TryAdd(category.Name.Trim(), category);
            byNormalizedName.TryAdd(NormalizeHeader(category.Name), category);
        }

        return (byExactName, byNormalizedName);
    }

    private static Category? TryFindCategory(
        (IReadOnlyDictionary<string, Category> ByExactName, IReadOnlyDictionary<string, Category> ByNormalizedName) lookup,
        string categoryName)
    {
        var trimmed = categoryName.Trim();
        if (lookup.ByExactName.TryGetValue(trimmed, out var exactMatch))
        {
            return exactMatch;
        }

        lookup.ByNormalizedName.TryGetValue(NormalizeHeader(trimmed), out var normalizedMatch);
        return normalizedMatch;
    }
}

public sealed record ProductImportResult(int ImportedCount, IReadOnlyList<string> Errors);
