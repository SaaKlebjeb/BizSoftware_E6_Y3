namespace InventoryManagementSystem.Utils;

public static class DateTimeHelper
{
    public static string FormatForDisplay(DateTime value) => value.ToLocalTime().ToString("dd MMM yyyy HH:mm");
}
