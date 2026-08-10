using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken = default);
}
