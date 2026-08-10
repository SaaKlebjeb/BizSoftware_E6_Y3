using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public interface IReportRepository
{
    Task<IReadOnlyList<DailySalesRow>> GetDailySalesAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopProductRow>> GetTopProductsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);
}
