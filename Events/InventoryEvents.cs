namespace InventoryManagementSystem.Events;

public static class InventoryEvents
{
    public static event EventHandler? ProductChanged;
    public static event EventHandler? SaleRecorded;
    public static event EventHandler? UserChanged;
    public static event EventHandler? CategoryChanged;
    public static event EventHandler? SettingsChanged;

    public static void RaiseProductChanged() => ProductChanged?.Invoke(null, EventArgs.Empty);
    public static void RaiseSaleRecorded() => SaleRecorded?.Invoke(null, EventArgs.Empty);
    public static void RaiseUserChanged() => UserChanged?.Invoke(null, EventArgs.Empty);
    public static void RaiseCategoryChanged() => CategoryChanged?.Invoke(null, EventArgs.Empty);
    public static void RaiseSettingsChanged() => SettingsChanged?.Invoke(null, EventArgs.Empty);
}
