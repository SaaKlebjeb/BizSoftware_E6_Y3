using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace InventoryManagementSystem.DataAccess;

public sealed class SqlServerDatabaseProvider : IDatabaseProvider
{
    public string Name => "SqlServer";
    public string GetLastInsertIdSql => "SELECT CAST(SCOPE_IDENTITY() AS INT);";

    public DbParameter CreateParameter(string name, object? value) =>
        CreateSqlParameter(name, value);

    public string GetPaginationSql(string baseQuery) =>
        $"{baseQuery} OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

    private static SqlParameter CreateSqlParameter(string name, object? value)
    {
        var parameter = new SqlParameter(name, value ?? DBNull.Value);
        if (value is null)
        {
            parameter.SqlDbType = name switch
            {
                "@CategoryId" or "@ProductId" or "@UserId" or "@EntityId" => System.Data.SqlDbType.Int,
                _ => System.Data.SqlDbType.NVarChar
            };
        }

        return parameter;
    }
}
