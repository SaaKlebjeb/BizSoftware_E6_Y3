using InventoryManagementSystem.DataAccess;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public sealed class UserRepository(IDbConnectionFactory connectionFactory, IDatabaseProvider databaseProvider)
    : RepositoryBase(connectionFactory, databaseProvider), IUserRepository
{
    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, """
            SELECT UserId, Username, PasswordHash, PasswordSalt, FullName, Role, IsActive, CreatedAt, UpdatedAt
            FROM Users
            WHERE Username = @Username;
            """);
        AddParameter(command, "@Username", username.Trim());
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, $"""
            INSERT INTO Users (Username, PasswordHash, PasswordSalt, FullName, Role, IsActive)
            VALUES (@Username, @PasswordHash, @PasswordSalt, @FullName, @Role, @IsActive);
            {DatabaseProvider.GetLastInsertIdSql}
            """);
        AddParameter(command, "@Username", user.Username.Trim());
        AddParameter(command, "@PasswordHash", user.PasswordHash);
        AddParameter(command, "@PasswordSalt", user.PasswordSalt);
        AddParameter(command, "@FullName", user.FullName.Trim());
        AddParameter(command, "@Role", user.Role.ToString());
        AddParameter(command, "@IsActive", user.IsActive);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = new List<User>();
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "SELECT UserId, Username, PasswordHash, PasswordSalt, FullName, Role, IsActive, CreatedAt, UpdatedAt FROM Users ORDER BY Username;");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(Map(reader));
        }

        return users;
    }

    public async Task SetRoleAsync(int userId, UserRole role, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "UPDATE Users SET Role = @Role, UpdatedAt = CURRENT_TIMESTAMP WHERE UserId = @UserId;");
        AddParameter(command, "@Role", role.ToString());
        AddParameter(command, "@UserId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task SetActiveAsync(int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "UPDATE Users SET IsActive = @IsActive, UpdatedAt = CURRENT_TIMESTAMP WHERE UserId = @UserId;");
        AddParameter(command, "@IsActive", isActive);
        AddParameter(command, "@UserId", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static User Map(System.Data.Common.DbDataReader reader) => new()
    {
        UserId = reader.GetInt32(0),
        Username = reader.GetString(1),
        PasswordHash = (byte[])reader[2],
        PasswordSalt = (byte[])reader[3],
        FullName = reader.GetString(4),
        Role = Enum.Parse<UserRole>(reader.GetString(5), true),
        IsActive = reader.GetBoolean(6),
        CreatedAt = reader.GetDateTime(7),
        UpdatedAt = reader.GetDateTime(8)
    };
}
