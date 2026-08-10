using InventoryManagementSystem.Models;
using InventoryManagementSystem.Repositories;

namespace InventoryManagementSystem.Services;

public sealed class AuditLogService(IAuditLogRepository auditLogRepository, AuthorizationService authorizationService)
{
    public Task<IReadOnlyList<AuditLog>> GetRecentAsync(Session session, DateTime startDate, DateTime endDate, string? entityName = null, int? entityId = null, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        return auditLogRepository.GetRecentAsync(startDate, endDate, entityName, entityId, cancellationToken: cancellationToken);
    }
}
