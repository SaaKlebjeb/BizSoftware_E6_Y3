using System.Data.Common;

namespace InventoryManagementSystem.DataAccess;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
