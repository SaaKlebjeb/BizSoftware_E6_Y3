using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Services;

public sealed class AuthorizationService
{
    public void EnsureAdmin(Session session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (!session.IsAdmin)
        {
            throw new UnauthorizedAccessException("Administrator permission is required for this operation.");
        }
    }

    public bool CanManageUsers(Session session) => session.IsAdmin;
    public bool CanManageSettings(Session session) => session.IsAdmin;
    public bool CanDeleteProducts(Session session) => session.IsAdmin;
}
