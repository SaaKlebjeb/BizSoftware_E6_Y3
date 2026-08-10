namespace InventoryManagementSystem.Models;

public sealed class Category
{
    public int CategoryId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
