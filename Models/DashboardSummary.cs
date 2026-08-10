namespace InventoryManagementSystem.Models;

public sealed class DashboardSummary
{
    public int TotalProducts { get; init; }
    public int LowStockCount { get; init; }
    public decimal TodaySales { get; init; }
    public string TopSeller { get; init; } = "No sales yet";
}
