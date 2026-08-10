using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(DateTime startDate, DateTime endDate, string? entityName = null, int? entityId = null, int limit = 500, CancellationToken cancellationToken = default);
}
