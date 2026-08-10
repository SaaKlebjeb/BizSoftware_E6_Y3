using System.Data.Common;

namespace InventoryManagementSystem.DataAccess;

public sealed class DatabaseInitializer(IDbConnectionFactory connectionFactory)
{
    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        return connection.State == System.Data.ConnectionState.Open;
    }
}
