using InventoryManagementSystem.DataAccess;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Utils;

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
        await using var command = CreateCommand(connection, $"""
            SELECT s.SaleDate, s.TotalAmount
            FROM Sales s
            WHERE s.SaleDate >= @StartDate AND s.SaleDate < @EndDate
            ORDER BY s.SaleDate;
            """);
        AddParameter(command, "@StartDate", start);
        AddParameter(command, "@EndDate", end);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var groupedRows = new Dictionary<DateTime, (int Count, decimal Total)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var saleDate = DateTimeHelper.ToLocalDate(reader.GetDateTime(0));
            var totalAmount = reader.GetDecimal(1);
            groupedRows.TryGetValue(saleDate, out var current);
            groupedRows[saleDate] = (current.Count + 1, current.Total + totalAmount);
        }

        rows.AddRange(groupedRows
            .OrderBy(row => row.Key)
            .Select(row => new DailySalesRow { Date = row.Key, NumberOfSales = row.Value.Count, TotalSales = row.Value.Total }));

        return rows;
    }
}
