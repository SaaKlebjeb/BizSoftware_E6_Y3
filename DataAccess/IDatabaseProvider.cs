using System.Data.Common;

namespace InventoryManagementSystem.DataAccess;

public interface IDatabaseProvider
{
    string Name { get; }
    DbParameter CreateParameter(string name, object? value);
    string GetPaginationSql(string baseQuery);
    string GetLastInsertIdSql { get; }
}
