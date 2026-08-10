using InventoryManagementSystem.Events;
using InventoryManagementSystem.Repositories;

namespace InventoryManagementSystem.Services;

public sealed class SettingsService(ISettingsRepository settingsRepository, AuthorizationService authorizationService)
{
    public async Task<string?> GetAsync(Session session, string key, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        return await settingsRepository.GetAsync(key, cancellationToken);
    }

    public async Task SetAsync(Session session, string key, string value, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Setting key and value are required.");
        }

        await settingsRepository.SetAsync(key.Trim(), value.Trim(), cancellationToken);
        InventoryEvents.RaiseCategoryChanged();
    }
}
