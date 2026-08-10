using System.Configuration;

namespace InventoryManagementSystem.Configuration;

public sealed class AppConfig
{
    public string ApplicationName { get; init; } = "Inventory Management System";
    public string DatabaseProvider { get; init; } = "SqlServer";
    public string SqlServerConnectionString { get; init; } =
        "Server=LAPTOP-TAKL7QQE\\SQLEXPRESS;Database=InventoryDB;Trusted_Connection=True;TrustServerCertificate=True;";
    public string SqliteConnectionString { get; init; } = "Data Source=inventory.db";
    public int DefaultLowStockThreshold { get; init; } = 10;
    public int DefaultPageSize { get; init; } = 25;

    public static AppConfig Load()
    {
        var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        var provider = Environment.GetEnvironmentVariable("INVENTORY_DB_PROVIDER")
            ?? config.AppSettings.Settings["DatabaseProvider"]?.Value;
        var sqlServerConnection = Environment.GetEnvironmentVariable("INVENTORY_SQLSERVER_CONNECTION")
            ?? config.ConnectionStrings.ConnectionStrings["InventorySqlServer"]?.ConnectionString;
        var sqliteConnection = Environment.GetEnvironmentVariable("INVENTORY_SQLITE_CONNECTION")
            ?? config.ConnectionStrings.ConnectionStrings["InventorySqlite"]?.ConnectionString;
        var applicationName = config.AppSettings.Settings["ApplicationName"]?.Value;
        var lowStockDefault = config.AppSettings.Settings["DefaultLowStockThreshold"]?.Value;
        var pageSize = config.AppSettings.Settings["DefaultPageSize"]?.Value;

        return new AppConfig
        {
            ApplicationName = string.IsNullOrWhiteSpace(applicationName) ? "Inventory Management System" : applicationName,
            DatabaseProvider = string.IsNullOrWhiteSpace(provider) ? "SqlServer" : provider,
            SqlServerConnectionString = string.IsNullOrWhiteSpace(sqlServerConnection)
                ? new AppConfig().SqlServerConnectionString
                : sqlServerConnection,
            SqliteConnectionString = string.IsNullOrWhiteSpace(sqliteConnection)
                ? new AppConfig().SqliteConnectionString
                : sqliteConnection,
            DefaultLowStockThreshold = int.TryParse(lowStockDefault, out var parsedThreshold) ? parsedThreshold : 10,
            DefaultPageSize = int.TryParse(pageSize, out var parsedPageSize) ? parsedPageSize : 25
        };
    }
}
