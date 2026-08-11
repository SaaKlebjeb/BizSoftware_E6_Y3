namespace InventoryManagementSystem.Utils;

public static class DateTimeHelper
{
    public static DateTime ToLocalTimeSafe(DateTime value)
    {
        if (value.Kind == DateTimeKind.Unspecified)
        {
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return value.ToLocalTime();
    }

    public static DateTime ToLocalDate(DateTime value) => ToLocalTimeSafe(value).Date;

    public static string FormatForDisplay(DateTime value) => ToLocalTimeSafe(value).ToString("dd MMM yyyy HH:mm");
}
