using InventoryManagementSystem.DataAccess;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public sealed class ProductRepository(IDbConnectionFactory connectionFactory, IDatabaseProvider databaseProvider)
    : RepositoryBase(connectionFactory, databaseProvider), IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetPageAsync(string? search, int? categoryId, int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        var products = new List<Product>();
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var baseQuery = """
            SELECT p.ProductId, p.SKU, p.Name, p.CategoryId, c.Name, p.Price, p.Quantity,
                   p.LowStockThreshold, p.CreatedAt, p.UpdatedAt
            FROM Products p
            INNER JOIN Categories c ON c.CategoryId = p.CategoryId
            WHERE (@Search = '' OR p.Name LIKE @SearchPattern OR p.SKU LIKE @SearchPattern OR c.Name LIKE @SearchPattern)
              AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
            ORDER BY p.Name, p.ProductId
            """;
        await using var command = CreateCommand(connection, DatabaseProvider.GetPaginationSql(baseQuery));
        AddParameter(command, "@Search", search?.Trim() ?? string.Empty);
        AddParameter(command, "@SearchPattern", $"%{search?.Trim() ?? string.Empty}%");
        AddParameter(command, "@CategoryId", categoryId);
        AddParameter(command, "@Offset", offset);
        AddParameter(command, "@PageSize", pageSize);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(Map(reader));
        }

        return products;
    }

    public async Task<int> CountAsync(string? search, int? categoryId, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, """
            SELECT COUNT(*)
            FROM Products p
            INNER JOIN Categories c ON c.CategoryId = p.CategoryId
            WHERE (@Search = '' OR p.Name LIKE @SearchPattern OR p.SKU LIKE @SearchPattern OR c.Name LIKE @SearchPattern)
              AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId);
            """);
        AddParameter(command, "@Search", search?.Trim() ?? string.Empty);
        AddParameter(command, "@SearchPattern", $"%{search?.Trim() ?? string.Empty}%");
        AddParameter(command, "@CategoryId", categoryId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task<Product?> GetByIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, """
            SELECT p.ProductId, p.SKU, p.Name, p.CategoryId, c.Name, p.Price, p.Quantity,
                   p.LowStockThreshold, p.CreatedAt, p.UpdatedAt
            FROM Products p
            INNER JOIN Categories c ON c.CategoryId = p.CategoryId
            WHERE p.ProductId = @ProductId;
            """);
        AddParameter(command, "@ProductId", productId);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(Product product, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, $"""
            INSERT INTO Products (SKU, Name, CategoryId, Price, Quantity, LowStockThreshold)
            VALUES (@SKU, @Name, @CategoryId, @Price, @Quantity, @LowStockThreshold);
            {DatabaseProvider.GetLastInsertIdSql}
            """);
        AddProductParameters(command, product);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, """
            UPDATE Products
            SET SKU = @SKU, Name = @Name, CategoryId = @CategoryId, Price = @Price,
                Quantity = @Quantity, LowStockThreshold = @LowStockThreshold, UpdatedAt = CURRENT_TIMESTAMP
            WHERE ProductId = @ProductId;
            """);
        AddProductParameters(command, product);
        AddParameter(command, "@ProductId", product.ProductId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> HasSalesAsync(int productId, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "SELECT COUNT(*) FROM SaleItems WHERE ProductId = @ProductId;");
        AddParameter(command, "@ProductId", productId);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
    }

    public async Task DeleteAsync(int productId, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, "DELETE FROM Products WHERE ProductId = @ProductId;");
        AddParameter(command, "@ProductId", productId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private void AddProductParameters(System.Data.Common.DbCommand command, Product product)
    {
        AddParameter(command, "@SKU", product.Sku.Trim());
        AddParameter(command, "@Name", product.Name.Trim());
        AddParameter(command, "@CategoryId", product.CategoryId);
        AddParameter(command, "@Price", product.Price);
        AddParameter(command, "@Quantity", product.Quantity);
        AddParameter(command, "@LowStockThreshold", product.LowStockThreshold);
    }

    private static Product Map(System.Data.Common.DbDataReader reader) => new()
    {
        ProductId = reader.GetInt32(0),
        Sku = reader.GetString(1),
        Name = reader.GetString(2),
        CategoryId = reader.GetInt32(3),
        CategoryName = reader.GetString(4),
        Price = reader.GetDecimal(5),
        Quantity = reader.GetInt32(6),
        LowStockThreshold = reader.GetInt32(7),
        CreatedAt = reader.GetDateTime(8),
        UpdatedAt = reader.GetDateTime(9)
    };
}
