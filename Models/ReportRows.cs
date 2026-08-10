namespace InventoryManagementSystem.Models;

public sealed class DailySalesRow
{
    public DateTime Date { get; init; }
    public int NumberOfSales { get; init; }
    public decimal TotalSales { get; init; }
}

public sealed class TopProductRow
{
    public string Product { get; init; } = string.Empty;
    public int QuantitySold { get; init; }
    public decimal Revenue { get; init; }
}
