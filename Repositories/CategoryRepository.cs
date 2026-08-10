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
}
