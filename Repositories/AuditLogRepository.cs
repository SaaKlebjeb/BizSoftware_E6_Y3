using InventoryManagementSystem.DataAccess;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public sealed class AuditLogRepository(IDbConnectionFactory connectionFactory, IDatabaseProvider databaseProvider)
    : RepositoryBase(connectionFactory, databaseProvider), IAuditLogRepository
{
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, """
            INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, Description)
            VALUES (@UserId, @Action, @EntityName, @EntityId, @Description);
            """);
        AddParameter(command, "@UserId", auditLog.UserId);
        AddParameter(command, "@Action", auditLog.Action);
        AddParameter(command, "@EntityName", auditLog.EntityName);
        AddParameter(command, "@EntityId", auditLog.EntityId);
        AddParameter(command, "@Description", auditLog.Description);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
