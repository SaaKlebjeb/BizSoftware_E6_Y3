using InventoryManagementSystem.DataAccess;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public sealed class DashboardRepository(IDbConnectionFactory connectionFactory, IDatabaseProvider databaseProvider)
    : RepositoryBase(connectionFactory, databaseProvider), IDashboardRepository
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var summary = new DashboardSummary
        {
            TotalProducts = await ExecuteIntAsync(connection, "SELECT COUNT(*) FROM Products;", cancellationToken),
            LowStockCount = await ExecuteIntAsync(connection, "SELECT COUNT(*) FROM Products WHERE Quantity < LowStockThreshold;", cancellationToken),
            TodaySales = await ExecuteDecimalAsync(connection, """
                SELECT COALESCE(SUM(TotalAmount), 0)
                FROM Sales
                WHERE SaleDate >= @StartOfDay AND SaleDate < @StartOfNextDay;
                """, new Dictionary<string, object?>
                {
                    ["@StartOfDay"] = DateTime.UtcNow.Date,
                    ["@StartOfNextDay"] = DateTime.UtcNow.Date.AddDays(1)
                }, cancellationToken),
            TopSeller = await GetTopSellerAsync(connection, cancellationToken)
        };
        return summary;
    }

    public async Task<IReadOnlyList<Product>> GetProductPreviewAsync(int limit, CancellationToken cancellationToken = default)
    {
        var products = new List<Product>();
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var query = DatabaseProvider.Name == "SqlServer"
            ? "SELECT TOP (@Limit) p.ProductId, p.SKU, p.Name, p.CategoryId, c.Name, p.Price, p.Quantity, p.LowStockThreshold, p.CreatedAt, p.UpdatedAt FROM Products p INNER JOIN Categories c ON c.CategoryId = p.CategoryId ORDER BY p.UpdatedAt DESC;"
            : "SELECT p.ProductId, p.SKU, p.Name, p.CategoryId, c.Name, p.Price, p.Quantity, p.LowStockThreshold, p.CreatedAt, p.UpdatedAt FROM Products p INNER JOIN Categories c ON c.CategoryId = p.CategoryId ORDER BY p.UpdatedAt DESC LIMIT @Limit;";
        await using var command = CreateCommand(connection, query);
        AddParameter(command, "@Limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new Product
            {
                ProductId = reader.GetInt32(0), Sku = reader.GetString(1), Name = reader.GetString(2),
                CategoryId = reader.GetInt32(3), CategoryName = reader.GetString(4), Price = reader.GetDecimal(5),
                Quantity = reader.GetInt32(6), LowStockThreshold = reader.GetInt32(7), CreatedAt = reader.GetDateTime(8), UpdatedAt = reader.GetDateTime(9)
            });
        }

        return products;
    }

    private async Task<string> GetTopSellerAsync(System.Data.Common.DbConnection connection, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, """
            SELECT p.Name
            FROM SaleItems si
            INNER JOIN Products p ON p.ProductId = si.ProductId
            GROUP BY p.ProductId, p.Name
            ORDER BY SUM(si.Quantity) DESC, p.Name
            """ + (DatabaseProvider.Name == "SqlServer" ? " OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;" : " LIMIT 1;"));
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value?.ToString() ?? "No sales yet";
    }

    private async Task<int> ExecuteIntAsync(System.Data.Common.DbConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, sql);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task<decimal> ExecuteDecimalAsync(System.Data.Common.DbConnection connection, string sql, IReadOnlyDictionary<string, object?> parameters, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, sql);
        foreach (var parameter in parameters)
        {
            AddParameter(command, parameter.Key, parameter.Value);
        }

        return Convert.ToDecimal(await command.ExecuteScalarAsync(cancellationToken));
    }
}
