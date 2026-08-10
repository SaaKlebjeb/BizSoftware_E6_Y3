using InventoryManagementSystem.Models;
using InventoryManagementSystem.Repositories;
using InventoryManagementSystem.Utils;

namespace InventoryManagementSystem.Services;

public sealed class AuthenticationService(IUserRepository userRepository)
{
    public async Task<Session> SignInAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            throw new ArgumentException("Username and password are required.");
        }

        var user = await userRepository.FindByUsernameAsync(username, cancellationToken);
        if (user is null || !user.IsActive || !PasswordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        return new Session(user);
    }

    public async Task<int> RegisterAsync(string username, string fullName, string password, string confirmation, CancellationToken cancellationToken = default)
    {
        username = username.Trim();
        fullName = fullName.Trim();

        if (!ValidationHelper.IsValidUsername(username))
        {
            throw new ArgumentException("Username must be 3-50 characters and contain only letters, numbers, dots, underscores, or hyphens.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException("Full name is required.");
        }

        if (!ValidationHelper.IsStrongPassword(password))
        {
            throw new ArgumentException("Password must be at least 8 characters and include uppercase, lowercase, and a number.");
        }

        if (!string.Equals(password, confirmation, StringComparison.Ordinal))
        {
            throw new ArgumentException("Password confirmation does not match.");
        }

        if (await userRepository.FindByUsernameAsync(username, cancellationToken) is not null)
        {
            throw new InvalidOperationException("That username is already registered.");
        }

        var (hash, salt) = PasswordHasher.HashPassword(password);
        return await userRepository.CreateAsync(new User
        {
            Username = username,
            FullName = fullName,
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.User,
            IsActive = true
        }, cancellationToken);
    }
}
