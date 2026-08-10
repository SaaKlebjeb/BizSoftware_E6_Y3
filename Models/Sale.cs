namespace InventoryManagementSystem.Models;

public sealed class Sale
{
    public int SaleId { get; init; }
    public int UserId { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime SaleDate { get; init; }
    public IReadOnlyList<SaleItem> Items { get; init; } = [];
}
