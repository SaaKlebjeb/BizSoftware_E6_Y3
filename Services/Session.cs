using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services;

public sealed class Session(User user)
{
    public int UserId { get; } = user.UserId;
    public string Username { get; } = user.Username;
    public string FullName { get; } = user.FullName;
    public UserRole Role { get; } = user.Role;
    public bool IsAdmin => Role == UserRole.Admin;
}
