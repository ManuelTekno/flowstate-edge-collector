using FTOptix.HMIProject;
using FTOptix.ODBCStore;
using System;
using UAManagedCore;

namespace NETCode.Repositories;

public abstract class OdbcRepositoryBase<T> : OptixRepositoryBase<T>
    where T : class, new()
{
    protected ODBCStore OdbcStore;

    protected OdbcRepositoryBase(
        string tableName,
        string[] dbColumns,
        Func<object[,], int, T> mapFunc,
        string storePath)
        : base(tableName, dbColumns, mapFunc)
    {
        OdbcStore = Project.Current.Get<ODBCStore>(storePath);

        if (OdbcStore == null)
            throw new Exception($"ODBC Store not found: {storePath}");

        MyTable = OdbcStore.Tables.Get(tableName);

        if (MyTable == null)
            throw new Exception($"Table '{tableName}' not found in store: {storePath}");
    }

    public override object[,] ExecuteQuery(string query)
    {
        try
        {
            object[,] resultSet;
            string[] header;

            OdbcStore.Query(query, out header, out resultSet);

            return resultSet;
        }
        catch (Exception ex)
        {
            Log.Error($"[OdbcRepo:{TableName}] {ex.Message}");
            return new object[0, 0];
        }
    }
}
