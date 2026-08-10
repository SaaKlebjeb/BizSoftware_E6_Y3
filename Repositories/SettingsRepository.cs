using InventoryManagementSystem.DataAccess;

namespace InventoryManagementSystem.Repositories;

public sealed class SettingsRepository(IDbConnectionFactory connectionFactory, IDatabaseProvider databaseProvider)
    : RepositoryBase(connectionFactory, databaseProvider), ISettingsRepository
{
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "SELECT [Value] FROM Settings WHERE [Key] = @Key;");
        AddParameter(command, "@Key", key);
        return (await command.ExecuteScalarAsync(cancellationToken))?.ToString();
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var sql = DatabaseProvider.Name == "SqlServer"
            ? "MERGE Settings AS target USING (SELECT @Key AS [Key], @Value AS [Value]) AS source ON target.[Key] = source.[Key] WHEN MATCHED THEN UPDATE SET [Value] = source.[Value] WHEN NOT MATCHED THEN INSERT ([Key], [Value]) VALUES (source.[Key], source.[Value]);"
            : "INSERT INTO Settings ([Key], [Value]) VALUES (@Key, @Value) ON CONFLICT([Key]) DO UPDATE SET [Value] = excluded.[Value];";
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "@Key", key);
        AddParameter(command, "@Value", value);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
