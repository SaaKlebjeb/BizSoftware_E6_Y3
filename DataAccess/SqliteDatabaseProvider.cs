using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace InventoryManagementSystem.DataAccess;

public sealed class SqliteDatabaseProvider : IDatabaseProvider
{
    public string Name => "SQLite";
    public string GetLastInsertIdSql => "SELECT last_insert_rowid();";

    public DbParameter CreateParameter(string name, object? value) =>
        new SqliteParameter(name, value ?? DBNull.Value);

    public string GetPaginationSql(string baseQuery) =>
        $"{baseQuery} LIMIT @PageSize OFFSET @Offset";
}
