using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services;

public static class InventoryCalculations
{
    public static int CalculateLowStock(IEnumerable<Product> products) =>
        products.Count(product => product.IsLowStock);

    public static decimal CalculateSaleTotal(IEnumerable<SaleItem> items) =>
        items.Sum(item => item.Quantity * item.UnitPrice);

    public static bool HasSufficientStock(Product product, int requestedQuantity) =>
        requestedQuantity > 0 && requestedQuantity <= product.Quantity;
}
