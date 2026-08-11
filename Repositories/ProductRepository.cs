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
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            var sku = string.IsNullOrWhiteSpace(product.Sku) ? $"TMP-{Guid.NewGuid():N}" : product.Sku.Trim();
            await using var command = CreateCommand(connection, $"""
                INSERT INTO Products (SKU, Name, CategoryId, Price, Quantity, LowStockThreshold)
                VALUES (@SKU, @Name, @CategoryId, @Price, @Quantity, @LowStockThreshold);
                {DatabaseProvider.GetLastInsertIdSql}
                """, transaction);
            AddProductParameters(command, product, sku);
            var productId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

            if (string.IsNullOrWhiteSpace(product.Sku))
            {
                await using var skuCommand = CreateCommand(connection, "UPDATE Products SET SKU = @GeneratedSku, UpdatedAt = CURRENT_TIMESTAMP WHERE ProductId = @ProductId;", transaction);
                AddParameter(skuCommand, "@GeneratedSku", $"SKU-{productId:D6}");
                AddParameter(skuCommand, "@ProductId", productId);
                await skuCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return productId;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<int> CreateManyAsync(IReadOnlyList<Product> products, CancellationToken cancellationToken = default)
    {
        if (products.Count == 0)
        {
            return 0;
        }

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var product in products)
            {
                var sku = string.IsNullOrWhiteSpace(product.Sku) ? $"TMP-{Guid.NewGuid():N}" : product.Sku.Trim();
                await using var command = CreateCommand(connection, $"""
                    INSERT INTO Products (SKU, Name, CategoryId, Price, Quantity, LowStockThreshold)
                    VALUES (@SKU, @Name, @CategoryId, @Price, @Quantity, @LowStockThreshold);
                    {DatabaseProvider.GetLastInsertIdSql}
                    """, transaction);
                AddProductParameters(command, product, sku);
                var productId = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));

                if (string.IsNullOrWhiteSpace(product.Sku))
                {
                    await using var skuCommand = CreateCommand(connection, "UPDATE Products SET SKU = @GeneratedSku, UpdatedAt = CURRENT_TIMESTAMP WHERE ProductId = @ProductId;", transaction);
                    AddParameter(skuCommand, "@GeneratedSku", $"SKU-{productId:D6}");
                    AddParameter(skuCommand, "@ProductId", productId);
                    await skuCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return products.Count;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> FindExistingSkusAsync(IEnumerable<string> skus, CancellationToken cancellationToken = default)
    {
        var skuList = skus
            .Select(sku => sku.Trim())
            .Where(sku => sku.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (skuList.Count == 0)
        {
            return [];
        }

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var placeholders = string.Join(", ", skuList.Select((_, index) => $"@SKU{index}"));
        await using var command = CreateCommand(connection, $"SELECT SKU FROM Products WHERE SKU IN ({placeholders});");
        for (var index = 0; index < skuList.Count; index++)
        {
            AddParameter(command, $"@SKU{index}", skuList[index]);
        }

        var existingSkus = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            existingSkus.Add(reader.GetString(0));
        }

        return existingSkus;
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
        AddProductParameters(command, product, product.Sku);
        AddParameter(command, "@ProductId", product.ProductId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RestoreStockAsync(int productId, int quantity, int userId, string reason, CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await using var updateCommand = CreateCommand(connection, """
                UPDATE Products
                SET Quantity = Quantity + @Quantity, UpdatedAt = CURRENT_TIMESTAMP
                WHERE ProductId = @ProductId;
                """, transaction);
            AddParameter(updateCommand, "@Quantity", quantity);
            AddParameter(updateCommand, "@ProductId", productId);
            if (await updateCommand.ExecuteNonQueryAsync(cancellationToken) != 1)
            {
                throw new InvalidOperationException("The selected product no longer exists.");
            }

            await using var auditCommand = CreateCommand(connection, """
                INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, Description)
                VALUES (@UserId, 'RESTORE_STOCK', 'Product', @ProductId, @Description);
                """, transaction);
            AddParameter(auditCommand, "@UserId", userId);
            AddParameter(auditCommand, "@ProductId", productId);
            AddParameter(auditCommand, "@Description", $"Restored {quantity:N0} unit(s). Reason: {reason}");
            await auditCommand.ExecuteNonQueryAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
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

    private void AddProductParameters(System.Data.Common.DbCommand command, Product product, string sku)
    {
        AddParameter(command, "@SKU", sku.Trim());
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
