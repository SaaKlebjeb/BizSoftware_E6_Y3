using InventoryManagementSystem.Events;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Repositories;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Services;

public sealed class ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository, AuthorizationService authorizationService)
{
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
        Validate(product);
        var productId = await productRepository.CreateAsync(product, cancellationToken);
        InventoryEvents.RaiseProductChanged();
        return productId;
    }

    public async Task UpdateAsync(Session session, Product product, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        Validate(product);
        await productRepository.UpdateAsync(product, cancellationToken);
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

    private static void Validate(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Sku) || product.Sku.Length > 50)
        {
            throw new ArgumentException("SKU is required and must be no longer than 50 characters.");
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
}
