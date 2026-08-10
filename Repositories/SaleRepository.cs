using InventoryManagementSystem.DataAccess;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public sealed class SaleRepository(IDbConnectionFactory connectionFactory, IDatabaseProvider databaseProvider)
    : RepositoryBase(connectionFactory, databaseProvider), ISaleRepository
{
    public async Task<int> RecordAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        if (sale.Items.Count == 0)
        {
            throw new InvalidOperationException("A sale must contain at least one item.");
        }

        await using var connection = ConnectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var saleId = await InsertSaleAsync(connection, transaction, sale, cancellationToken);

            foreach (var item in sale.Items)
            {
                await ReduceStockAsync(connection, transaction, item, cancellationToken);
                await InsertSaleItemAsync(connection, transaction, saleId, item, cancellationToken);
            }

            await InsertAuditLogAsync(connection, transaction, sale.UserId, saleId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return saleId;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<int> InsertSaleAsync(System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction transaction, Sale sale, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, $"""
            INSERT INTO Sales (UserId, TotalAmount, SaleDate)
            VALUES (@UserId, @TotalAmount, @SaleDate);
            {DatabaseProvider.GetLastInsertIdSql}
            """, transaction);
        AddParameter(command, "@UserId", sale.UserId);
        AddParameter(command, "@TotalAmount", sale.TotalAmount);
        AddParameter(command, "@SaleDate", sale.SaleDate);
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    private async Task ReduceStockAsync(System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction transaction, SaleItem item, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, """
            UPDATE Products
            SET Quantity = Quantity - @Quantity, UpdatedAt = CURRENT_TIMESTAMP
            WHERE ProductId = @ProductId AND Quantity >= @Quantity;
            """, transaction);
        AddParameter(command, "@Quantity", item.Quantity);
        AddParameter(command, "@ProductId", item.ProductId);
        var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}.");
        }
    }

    private async Task InsertSaleItemAsync(System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction transaction, int saleId, SaleItem item, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, """
            INSERT INTO SaleItems (SaleId, ProductId, Quantity, UnitPrice)
            VALUES (@SaleId, @ProductId, @Quantity, @UnitPrice);
            """, transaction);
        AddParameter(command, "@SaleId", saleId);
        AddParameter(command, "@ProductId", item.ProductId);
        AddParameter(command, "@Quantity", item.Quantity);
        AddParameter(command, "@UnitPrice", item.UnitPrice);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task InsertAuditLogAsync(System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction transaction, int userId, int saleId, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, """
            INSERT INTO AuditLogs (UserId, Action, EntityName, EntityId, Description)
            VALUES (@UserId, 'RECORD_SALE', 'Sale', @SaleId, 'Sale recorded and stock updated atomically.');
            """, transaction);
        AddParameter(command, "@UserId", userId);
        AddParameter(command, "@SaleId", saleId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
