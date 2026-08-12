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
            INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, Sku, Description)
            VALUES (@UserId, @Action, @EntityName, @EntityId, @Sku, @Description);
            """);
        AddParameter(command, "@UserId", auditLog.UserId);
        AddParameter(command, "@Action", auditLog.Action);
        AddParameter(command, "@EntityName", auditLog.EntityName);
        AddParameter(command, "@EntityId", auditLog.EntityId);
        AddParameter(command, "@Sku", auditLog.Sku);
        AddParameter(command, "@Description", auditLog.Description);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(DateTime startDate, DateTime endDate, string? entityName = null, int? entityId = null, int limit = 500, CancellationToken cancellationToken = default)
    {
        if (endDate <= startDate)
        {
            throw new ArgumentException("The audit-log end date must be after the start date.");
        }

        if (limit is < 1 or > 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "The audit-log limit must be between 1 and 1,000.");
        }

        var logs = new List<AuditLog>();
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var query = DatabaseProvider.Name == "SqlServer"
            ? """
              SELECT TOP (@Limit) a.AuditLogId, a.UserId, COALESCE(u.Username, ''), a.Action,
                     a.EntityName, a.EntityId, a.Sku, a.Description, a.CreatedAt
              FROM AuditLogs a
              LEFT JOIN Users u ON u.UserId = a.UserId
              WHERE a.CreatedAt >= @StartDate AND a.CreatedAt < @EndDate
                AND (@EntityName IS NULL OR a.EntityName = @EntityName)
                AND (@EntityId IS NULL OR a.EntityId = @EntityId)
              ORDER BY a.CreatedAt DESC, a.AuditLogId DESC;
              """
            : """
              SELECT a.AuditLogId, a.UserId, COALESCE(u.Username, ''), a.Action,
                     a.EntityName, a.EntityId, a.Sku, a.Description, a.CreatedAt
              FROM AuditLogs a
              LEFT JOIN Users u ON u.UserId = a.UserId
              WHERE a.CreatedAt >= @StartDate AND a.CreatedAt < @EndDate
                AND (@EntityName IS NULL OR a.EntityName = @EntityName)
                AND (@EntityId IS NULL OR a.EntityId = @EntityId)
              ORDER BY a.CreatedAt DESC, a.AuditLogId DESC
              LIMIT @Limit;
              """;
        await using var command = CreateCommand(connection, query);
        AddParameter(command, "@StartDate", startDate);
        AddParameter(command, "@EndDate", endDate);
        AddParameter(command, "@EntityName", string.IsNullOrWhiteSpace(entityName) ? null : entityName.Trim());
        AddParameter(command, "@EntityId", entityId);
        AddParameter(command, "@Limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            logs.Add(new AuditLog
            {
                AuditLogId = reader.GetInt64(0),
                UserId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                Username = reader.GetString(2),
                Action = reader.GetString(3),
                EntityName = reader.GetString(4),
                EntityId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Sku = reader.IsDBNull(6) ? null : reader.GetString(6),
                Description = reader.GetString(7),
                CreatedAt = reader.GetDateTime(8)
            });
        }

        return logs;
    }
}
