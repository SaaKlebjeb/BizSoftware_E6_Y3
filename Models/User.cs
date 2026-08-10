namespace InventoryManagementSystem.Models;

public sealed class User
{
    public int UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public byte[] PasswordHash { get; init; } = [];
    public byte[] PasswordSalt { get; init; } = [];
    public string FullName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
    public bool IsActive { get; init; } = true;
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public enum UserRole
{
    Admin,
    User
}
