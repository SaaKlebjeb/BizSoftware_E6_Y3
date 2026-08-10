using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Repositories;

public interface ISaleRepository
{
    Task<int> RecordAsync(Sale sale, CancellationToken cancellationToken = default);
}
