namespace InventoryManagementSystem.Models;

public sealed class SaleItem
{
    public int SaleItemId { get; init; }
    public int SaleId { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }
}
