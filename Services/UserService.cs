using InventoryManagementSystem.Events;
using InventoryManagementSystem.Models;
using InventoryManagementSystem.Repositories;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Services;

public sealed class UserService(IUserRepository userRepository, AuthorizationService authorizationService, AuditLogService auditLogService)
{
    public async Task<IReadOnlyList<User>> GetAllAsync(Session session, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        return await userRepository.GetAllAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(Session session, string username, string fullName, string password, string confirmation, UserRole role, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        username = username.Trim();
        fullName = fullName.Trim();
        if (!ValidationHelper.IsValidUsername(username) || string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Enter a valid username and full name.");
        }

        if (!ValidationHelper.IsStrongPassword(password) || !string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            throw new ArgumentException("Password must be strong and confirmation must match.");
        }

        if (await userRepository.FindByUsernameAsync(username, cancellationToken) is not null)
        {
            throw new InvalidOperationException("That username is already registered.");
        }

        var (hash, salt) = PasswordHasher.HashPassword(password);
        var userId = await userRepository.CreateAsync(new User { Username = username, FullName = fullName, PasswordHash = hash, PasswordSalt = salt, Role = role, IsActive = true }, cancellationToken);
        await auditLogService.LogAsync(session, "Create", "User", userId, null, $"Created user: {username} ({role})");
        InventoryEvents.RaiseUserChanged();
        return userId;
    }

    public async Task SetRoleAsync(Session session, int userId, UserRole role, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        await userRepository.SetRoleAsync(userId, role, cancellationToken);
        await auditLogService.LogAsync(session, "UpdateRole", "User", userId, null, $"Set role to: {role}");
        InventoryEvents.RaiseUserChanged();
    }

    public async Task SetActiveAsync(Session session, int userId, bool isActive, CancellationToken cancellationToken = default)
    {
        authorizationService.EnsureAdmin(session);
        if (userId == session.UserId && !isActive)
        {
            throw new InvalidOperationException("You cannot deactivate your own account.");
        }

        await userRepository.SetActiveAsync(userId, isActive, cancellationToken);
        await auditLogService.LogAsync(session, isActive ? "Unlock" : "Lock", "User", userId, null, isActive ? "Account unlocked" : "Account locked");
        InventoryEvents.RaiseUserChanged();
    }
}
