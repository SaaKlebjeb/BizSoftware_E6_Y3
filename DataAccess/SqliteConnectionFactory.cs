using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace InventoryManagementSystem.DataAccess;

public sealed class SqliteConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public DbConnection CreateConnection() => new SqliteConnection(connectionString);
}
