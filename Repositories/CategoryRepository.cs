using InventoryManagementSystem.DataAccess;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public sealed class CategoryRepository(IDbConnectionFactory connectionFactory, IDatabaseProvider databaseProvider)
    : RepositoryBase(connectionFactory, databaseProvider), ICategoryRepository
{
    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = new List<Category>();
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "SELECT CategoryId, Name, Description, CreatedAt FROM Categories ORDER BY Name;");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(new Category
            {
                CategoryId = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            });
        }

        return categories;
    }

    public async Task<int> CreateAsync(string name, string description, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, $"""
            INSERT INTO Categories (Name, Description)
            VALUES (@Name, @Description);
            {DatabaseProvider.GetLastInsertIdSql}
            """);
        AddParameter(command, "@Name", name.Trim());
        AddParameter(command, "@Description", description.Trim());
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, """
            UPDATE Categories
            SET Name = @Name, Description = @Description
            WHERE CategoryId = @CategoryId;
            """);
        AddParameter(command, "@Name", category.Name.Trim());
        AddParameter(command, "@Description", category.Description.Trim());
        AddParameter(command, "@CategoryId", category.CategoryId);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new InvalidOperationException("The selected category no longer exists.");
        }
    }

    public async Task<bool> HasProductsAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "SELECT COUNT(*) FROM Products WHERE CategoryId = @CategoryId;");
        AddParameter(command, "@CategoryId", categoryId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task DeleteAsync(int categoryId, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "DELETE FROM Categories WHERE CategoryId = @CategoryId;");
        AddParameter(command, "@CategoryId", categoryId);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
        {
            throw new InvalidOperationException("The selected category no longer exists.");
        }
    }
}
