using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetPageAsync(string? search, int? categoryId, int offset, int pageSize, CancellationToken cancellationToken = default);
    Task<int> CountAsync(string? search, int? categoryId, CancellationToken cancellationToken = default);
    Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Product product, CancellationToken cancellationToken = default);
    Task<int> CreateManyAsync(IReadOnlyList<Product> products, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> FindExistingSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
    Task RestoreStockAsync(int productId, int quantity, int userId, string reason, CancellationToken cancellationToken = default);
    Task<bool> HasSalesAsync(int productId, CancellationToken cancellationToken = default);
    Task DeleteAsync(int productId, CancellationToken cancellationToken = default);
}
