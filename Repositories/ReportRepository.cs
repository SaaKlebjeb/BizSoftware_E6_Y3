using InventoryManagementSystem.DataAccess;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public sealed class ReportRepository(IDbConnectionFactory connectionFactory, IDatabaseProvider databaseProvider)
    : RepositoryBase(connectionFactory, databaseProvider), IReportRepository
{
    public Task<IReadOnlyList<DailySalesRow>> GetDailySalesAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default) =>
        QueryDailySalesAsync(start, end, cancellationToken);

    public async Task<IReadOnlyList<TopProductRow>> GetTopProductsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        var rows = new List<TopProductRow>();
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, """
            SELECT p.Name, SUM(si.Quantity) AS QuantitySold, SUM(si.Subtotal) AS Revenue
            FROM SaleItems si
            INNER JOIN Sales s ON s.SaleId = si.SaleId
            INNER JOIN Products p ON p.ProductId = si.ProductId
            WHERE s.SaleDate >= @StartDate AND s.SaleDate < @EndDate
            GROUP BY p.ProductId, p.Name
            ORDER BY QuantitySold DESC, p.Name;
            """);
        AddParameter(command, "@StartDate", start);
        AddParameter(command, "@EndDate", end);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new TopProductRow { Product = reader.GetString(0), QuantitySold = Convert.ToInt32(reader.GetValue(1)), Revenue = Convert.ToDecimal(reader.GetValue(2)) });
        }

        return rows;
    }

    private async Task<IReadOnlyList<DailySalesRow>> QueryDailySalesAsync(DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var rows = new List<DailySalesRow>();
        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        var dayExpression = DatabaseProvider.Name == "SqlServer" ? "CONVERT(date, s.SaleDate)" : "date(s.SaleDate)";
        await using var command = CreateCommand(connection, $"""
            SELECT {dayExpression} AS SaleDay, COUNT(*) AS NumberOfSales, SUM(s.TotalAmount) AS TotalSales
            FROM Sales s
            WHERE s.SaleDate >= @StartDate AND s.SaleDate < @EndDate
            GROUP BY {dayExpression}
            ORDER BY SaleDay;
            """);
        AddParameter(command, "@StartDate", start);
        AddParameter(command, "@EndDate", end);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new DailySalesRow { Date = Convert.ToDateTime(reader.GetValue(0)), NumberOfSales = Convert.ToInt32(reader.GetValue(1)), TotalSales = Convert.ToDecimal(reader.GetValue(2)) });
        }

        return rows;
    }
}
