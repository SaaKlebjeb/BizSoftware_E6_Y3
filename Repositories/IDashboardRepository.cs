using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public interface IDashboardRepository
{
    Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetProductPreviewAsync(int limit, CancellationToken cancellationToken = default);
}
