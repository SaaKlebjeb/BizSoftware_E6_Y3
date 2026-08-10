using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace InventoryManagementSystem.DataAccess;

public sealed class SqlServerDatabaseProvider : IDatabaseProvider
{
    public string Name => "SqlServer";
    public string GetLastInsertIdSql => "SELECT CAST(SCOPE_IDENTITY() AS INT);";

    public DbParameter CreateParameter(string name, object? value) =>
        new SqlParameter(name, value ?? DBNull.Value);

    public string GetPaginationSql(string baseQuery) =>
        $"{baseQuery} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
}
