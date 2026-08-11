using InventoryManagementSystem.Events;
using InventoryManagementSystem.Repositories;

namespace InventoryManagementSystem.Services;

public sealed class SettingsService(ISettingsRepository settingsRepository, AuthorizationService authorizationService)
{
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        return await settingsRepository.GetAsync(key, cancellationToken);
    }

    public async Task<string?> GetAsync(Session session, string key, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        return await settingsRepository.GetAsync(key, cancellationToken);
    }

    public async Task SetAsync(Session session, string key, string value, bool allowEmptyValue = false, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Setting key is required.");
        }

        if (!allowEmptyValue && string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Setting value is required.");
        }

        await settingsRepository.SetAsync(key.Trim(), value.Trim(), cancellationToken);
        InventoryEvents.RaiseSettingsChanged();
    }

    public async Task SaveManyAsync(Session session, IReadOnlyCollection<(string Key, string Value, bool AllowEmptyValue)> settings, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        if (settings.Count == 0)
        {
            return;
        }

        var entries = new List<KeyValuePair<string, string>>();
        foreach (var setting in settings)
        {
            if (string.IsNullOrWhiteSpace(setting.Key))
            {
                throw new ArgumentException("Setting key is required.");
            }

            if (!setting.AllowEmptyValue && string.IsNullOrWhiteSpace(setting.Value))
            {
                throw new ArgumentException("Setting value is required.");
            }

            entries.Add(new KeyValuePair<string, string>(setting.Key.Trim(), setting.Value.Trim()));
        }

        await settingsRepository.SetManyAsync(entries, cancellationToken);
        InventoryEvents.RaiseSettingsChanged();
    }
}
