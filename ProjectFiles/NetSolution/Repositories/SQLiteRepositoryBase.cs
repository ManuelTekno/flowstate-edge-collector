using FTOptix.HMIProject;
using FTOptix.SQLiteStore;
using System;
using UAManagedCore;

namespace NETCode.Repositories;

public abstract class SQLiteRepositoryBase<T> : OptixRepositoryBase<T>
    where T : class, new()
{
    protected SQLiteStore SqliteStore;

    protected SQLiteRepositoryBase(
        string tableName,
        string[] dbColumns,
        Func<object[,], int, T> mapFunc,
        string storePath)
        : base(tableName, dbColumns, mapFunc)
    {
        SqliteStore = Project.Current.Get<SQLiteStore>(storePath);

        if (SqliteStore == null)
            throw new Exception($"SQLiteStore not found: {storePath}");

        MyTable = SqliteStore.Tables.Get(tableName);

        if (MyTable == null)
            throw new Exception($"Table '{tableName}' not found in store: {storePath}");
    }

    public override object[,] ExecuteQuery(string query)
    {
        try
        {
            object[,] resultSet;
            string[] header;

            SqliteStore.Query(query, out header, out resultSet);

            return resultSet;
        }
        catch (Exception ex)
        {
            Log.Error($"[SQLiteRepo:{TableName}] {ex.Message}");
            return new object[0, 0];
        }
    }
}
