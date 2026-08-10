using System.Data.Common;

namespace InventoryManagementSystem.Utils;

public static class UserMessageFormatter
{
    public static string From(Exception exception)
    {
        if (exception is DbException databaseException)
        {
            var message = databaseException.Message;
            if (ContainsAny(message, "UNIQUE", "duplicate", "already exists"))
            {
                return "This SKU or category name is already in use. Enter a different value.";
            }

            if (ContainsAny(message, "FOREIGN KEY", "REFERENCE constraint", "constraint failed"))
            {
                return "The selected category or product is no longer available. Refresh the screen and try again.";
            }

            if (ContainsAny(message, "network", "server", "connect", "timeout", "login failed"))
            {
                return "The database server is unavailable. Check that SQL Server is running and the connection settings are correct.";
            }

            return "The database operation failed. Check the database configuration and try again.";
        }

        return exception switch
        {
            UnauthorizedAccessException => exception.Message,
            ArgumentException => exception.Message,
            InvalidOperationException => exception.Message,
            _ => "The operation could not be completed. Please try again."
        };
    }

    private static bool ContainsAny(string message, params string[] values) =>
        values.Any(value => message.Contains(value, StringComparison.OrdinalIgnoreCase));
}
