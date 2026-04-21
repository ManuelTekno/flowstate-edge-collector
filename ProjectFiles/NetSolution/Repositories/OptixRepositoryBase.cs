using FTOptix.Store;
using System;
using System.Collections.Generic;
using UAManagedCore;

namespace NETCode.Repositories;

public abstract class OptixRepositoryBase<T> where T : class, new()
{
    protected Table MyTable;

    protected string TableName;
    protected string[] DbColumns;
    protected Func<object[,], int, T> MapFunc;

    protected OptixRepositoryBase(
        string tableName,
        string[] dbColumns,
        Func<object[,], int, T> mapFunc)
    {
        TableName = tableName;
        DbColumns = dbColumns;
        MapFunc = mapFunc;
    }

    // 🔹 Cada implementación decide cómo ejecutar SQL
    public abstract object[,] ExecuteQuery(string query);

    // =============================
    // 🔹 GET ALL
    // =============================
    public IEnumerable<T> GetAll()
    {
        var items = new List<T>();

        if (!IsTableReady())
            return items;

        var resultSet = ExecuteQuery($"SELECT * FROM {TableName}");

        for (int i = 0; i < resultSet.GetLength(0); i++)
            items.Add(MapFunc(resultSet, i));

        return items;
    }

    // =============================
    // 🔹 GET BY ID
    // =============================
    public T GetById(int id)
    {
        if (!IsTableReady())
            return null;

        var resultSet = ExecuteQuery($"SELECT * FROM {TableName} WHERE id = {id}");

        if (resultSet.GetLength(0) == 0)
            return null;

        return MapFunc(resultSet, 0);
    }

    // =============================
    // 🔹 INSERT
    // =============================
    public void Insert(object[] values)
    {
        if (!IsTableReady())
            return;

        var insertValues = new object[1, values.Length];

        for (int i = 0; i < values.Length; i++)
            insertValues[0, i] = values[i];

        MyTable.Insert(DbColumns, insertValues);
    }

    // =============================
    // 🔹 UPDATE
    // =============================
    public void Update(string query)
    {
        if (!IsTableReady())
            return;

        ExecuteQuery(query);
    }

    // =============================
    // 🔹 DELETE
    // =============================
    public void DeleteById(int id)
    {
        if (!IsTableReady())
            return;

        ExecuteQuery($"DELETE FROM {TableName} WHERE id = {id}");
    }

    // =============================
    // 🔹 VALIDACIÓN SIMPLE
    // =============================
    protected bool IsTableReady()
    {
        if (MyTable == null)
        {
            Log.Error($"[Repo:{TableName}] Table is NULL");
            return false;
        }

        return true;
    }
}
