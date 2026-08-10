namespace InventoryManagementSystem.Models;

public sealed class AuditLog
{
    public int AuditLogId { get; init; }
    public int? UserId { get; init; }
    public string Action { get; init; } = string.Empty;
    public string EntityName { get; init; } = string.Empty;
    public int? EntityId { get; init; }
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
