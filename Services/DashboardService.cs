using InventoryManagementSystem.Models;
using InventoryManagementSystem.Repositories;

namespace InventoryManagementSystem.Services;

public sealed class DashboardService(IDashboardRepository dashboardRepository)
{
    public Task<DashboardSummary> GetSummaryAsync(Session session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        return dashboardRepository.GetSummaryAsync(cancellationToken);
    }

    public Task<IReadOnlyList<Product>> GetProductPreviewAsync(Session session, int limit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        return dashboardRepository.GetProductPreviewAsync(limit, cancellationToken);
    }
}
