namespace InventoryManagementSystem.Repositories;

public interface ISettingsRepository
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
    Task SetManyAsync(IReadOnlyCollection<KeyValuePair<string, string>> settings, CancellationToken cancellationToken = default);
}
