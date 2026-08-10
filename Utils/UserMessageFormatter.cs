using System.Data.Common;

namespace InventoryManagementSystem.Utils;

public static class UserMessageFormatter
{
    public static string From(Exception exception) => exception switch
    {
        DbException => "The database operation could not be completed. Check the database connection and data constraints.",
        UnauthorizedAccessException => exception.Message,
        ArgumentException => exception.Message,
        InvalidOperationException => exception.Message,
        _ => "The operation could not be completed. Please try again."
    };
}
