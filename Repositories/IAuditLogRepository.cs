using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
}
