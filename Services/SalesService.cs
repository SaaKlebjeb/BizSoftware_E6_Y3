using InventoryManagementSystem.Events;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Repositories;

namespace InventoryManagementSystem.Services;

public sealed class SalesService(ISaleRepository saleRepository, IProductRepository productRepository)
{
    public async Task<int> RecordSaleAsync(Session session, IReadOnlyCollection<SaleLineRequest> requestedItems, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (requestedItems.Count == 0)
        {
            throw new ArgumentException("Add at least one product to the sale.");
        }

        var groupedItems = requestedItems
            .GroupBy(item => item.ProductId)
            .Select(group => new SaleLineRequest(group.Key, group.Sum(item => item.Quantity)))
            .ToList();
        var saleItems = new List<SaleItem>();

        foreach (var requestedItem in groupedItems)
        {
            if (requestedItem.Quantity <= 0)
            {
                throw new ArgumentException("Sale quantities must be greater than zero.");
            }

            var product = await productRepository.GetByIdAsync(requestedItem.ProductId, cancellationToken)
                ?? throw new InvalidOperationException("One of the selected products no longer exists.");
            if (requestedItem.Quantity > product.Quantity)
            {
                throw new InvalidOperationException($"Insufficient stock for '{product.Name}'. Available quantity: {product.Quantity}.");
            }

            saleItems.Add(new SaleItem
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                Quantity = requestedItem.Quantity,
                UnitPrice = product.Price,
                Subtotal = requestedItem.Quantity * product.Price
            });
        }

        var sale = new Sale
        {
            UserId = session.UserId,
            TotalAmount = InventoryCalculations.CalculateSaleTotal(saleItems),
            SaleDate = DateTime.UtcNow,
            Items = saleItems
        };
        var saleId = await saleRepository.RecordAsync(sale, cancellationToken);
        InventoryEvents.RaiseSaleRecorded();
        InventoryEvents.RaiseProductChanged();
        return saleId;
    }
}

public sealed record SaleLineRequest(int ProductId, int Quantity);
