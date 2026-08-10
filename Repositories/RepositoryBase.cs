using System.Data.Common;
using InventoryManagementSystem.DataAccess;

namespace InventoryManagementSystem.Repositories;

public abstract class RepositoryBase(IDbConnectionFactory connectionFactory, IDatabaseProvider databaseProvider)
{
    protected readonly IDbConnectionFactory ConnectionFactory = connectionFactory;
    protected readonly IDatabaseProvider DatabaseProvider = databaseProvider;

    protected DbCommand CreateCommand(DbConnection connection, string sql, DbTransaction? transaction = null)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        return command;
    }

    protected void AddParameter(DbCommand command, string name, object? value)
    {
        command.Parameters.Add(DatabaseProvider.CreateParameter(name, value));
    }
}
