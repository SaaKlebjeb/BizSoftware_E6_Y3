using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace InventoryManagementSystem.DataAccess;

public sealed class SqlServerConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public DbConnection CreateConnection() => new SqlConnection(connectionString);
}
