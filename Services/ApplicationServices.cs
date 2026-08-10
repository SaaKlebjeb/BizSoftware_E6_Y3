using InventoryManagementSystem.Configuration;
using InventoryManagementSystem.DataAccess;
using InventoryManagementSystem.Repositories;

namespace InventoryManagementSystem.Services;

public sealed class ApplicationServices
{
    public ApplicationServices(AppConfig configuration)
    {
        var useSqlite = string.Equals(configuration.DatabaseProvider, "SQLite", StringComparison.OrdinalIgnoreCase);
        var provider = useSqlite ? (IDatabaseProvider)new SqliteDatabaseProvider() : new SqlServerDatabaseProvider();
        var connectionFactory = useSqlite
            ? (IDbConnectionFactory)new SqliteConnectionFactory(configuration.SqliteConnectionString)
            : new SqlServerConnectionFactory(configuration.SqlServerConnectionString);

        UserRepository = new UserRepository(connectionFactory, provider);
        ProductRepository = new ProductRepository(connectionFactory, provider);
        CategoryRepository = new CategoryRepository(connectionFactory, provider);
        SaleRepository = new SaleRepository(connectionFactory, provider);
        AuditLogRepository = new AuditLogRepository(connectionFactory, provider);
        DashboardRepository = new DashboardRepository(connectionFactory, provider);
        ReportRepository = new ReportRepository(connectionFactory, provider);
        SettingsRepository = new SettingsRepository(connectionFactory, provider);
        Authentication = new AuthenticationService(UserRepository);
        Authorization = new AuthorizationService();
        Products = new ProductService(ProductRepository, CategoryRepository, Authorization);
        Sales = new SalesService(SaleRepository, ProductRepository);
        Dashboard = new DashboardService(DashboardRepository);
        Reports = new ReportService(ReportRepository);
        Users = new UserService(UserRepository, Authorization);
        Settings = new SettingsService(SettingsRepository, Authorization);
    }

    public IUserRepository UserRepository { get; }
    public IProductRepository ProductRepository { get; }
    public ICategoryRepository CategoryRepository { get; }
    public ISaleRepository SaleRepository { get; }
    public IAuditLogRepository AuditLogRepository { get; }
    public IDashboardRepository DashboardRepository { get; }
    public IReportRepository ReportRepository { get; }
    public ISettingsRepository SettingsRepository { get; }
    public AuthenticationService Authentication { get; }
    public AuthorizationService Authorization { get; }
    public ProductService Products { get; }
    public SalesService Sales { get; }
    public DashboardService Dashboard { get; }
    public ReportService Reports { get; }
    public UserService Users { get; }
    public SettingsService Settings { get; }
}
