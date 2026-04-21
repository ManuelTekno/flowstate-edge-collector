using NETCode.Entities;
using NETCode.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using UAManagedCore;

public class ProductionEventLocalRepository : SQLiteRepositoryBase<ProductionEventLocal>
{
    private const int MAX_RETRIES = 5;

    public ProductionEventLocalRepository(string storePath)
        : base(
            "production_events",
            new string[]
            {
                "source_event_key",
                "utc_time_stamp",
                "source_inserted_at_utc",
                "event_type",
                "event_id",
                "pallet_id",
                "pallet_destination",
                "customer_id",
                "server_id",
                "tekno_device_id",
                "plant_id",
                "department_id",
                "line_id",
                "stop_id",
                "build_result",
                "defect_station_id",
                "defect_reason",
                "operator_id",
                "part_model",
                "tekno_payload_version",
                "tekno_emitter_version",
                "sent_status",
                "retry_count"
            },
            (rs, row) => MapSafe(rs, row),
            storePath
        )
    {
        Log.Info("[LocalRepo] ProductionEventLocalRepository created");
    }

    // =============================
    // SAFE MAP
    // =============================
    private static ProductionEventLocal MapSafe(object[,] rs, int row)
    {
        return new ProductionEventLocal
        {
            SourceEventKey = GetString(rs[row, 0]),

            UtcTimeStamp = GetDateRequired(rs[row, 1]),
            SourceInsertedAtUtc = GetDate(rs[row, 2]),

            EventType = GetString(rs[row, 3]),
            EventId = GetLong(rs[row, 4]),

            PalletId = GetInt(rs[row, 5]),
            PalletDestination = GetString(rs[row, 6]),

            CustomerId = GetString(rs[row, 7]),
            ServerId = GetString(rs[row, 8]),
            TeknoDeviceId = GetString(rs[row, 9]),

            PlantId = GetString(rs[row, 10]),
            DepartmentId = GetString(rs[row, 11]),
            LineId = GetString(rs[row, 12]),
            StopId = GetString(rs[row, 13]),

            BuildResult = GetString(rs[row, 14]),
            DefectStationId = GetString(rs[row, 15]),
            DefectReason = GetString(rs[row, 16]),

            OperatorId = GetString(rs[row, 17]),
            PartModel = GetString(rs[row, 18]),

            TeknoPayloadVersion = GetString(rs[row, 19]),
            TeknoEmitterVersion = GetString(rs[row, 20]),

            SentStatus = GetInt(rs[row, 21]),
            RetryCount = GetInt(rs[row, 22])
        };
    }

    // =============================
    // HELPERS (CLAVE)
    // =============================
    private static string GetString(object value)
        => value == null || value == DBNull.Value ? string.Empty : value.ToString();

    private static int GetInt(object value)
        => value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);

    private static long GetLong(object value)
        => value == null || value == DBNull.Value ? 0 : Convert.ToInt64(value);

    private static DateTime? GetDate(object value)
        => value == null || value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);

    // =============================
    // GET PENDING
    // =============================
    public List<ProductionEventLocal> GetPending(int batchSize)
    {
        string query =
    "SELECT " +
    "source_event_key, " +
    "utc_time_stamp, " +
    "source_inserted_at_utc, " +
    "event_type, " +
    "event_id, " +
    "pallet_id, " +
    "pallet_destination, " +
    "customer_id, " +
    "server_id, " +
    "tekno_device_id, " +
    "plant_id, " +
    "department_id, " +
    "line_id, " +
    "stop_id, " +
    "build_result, " +
    "defect_station_id, " +
    "defect_reason, " +
    "operator_id, " +
    "part_model, " +
    "tekno_payload_version, " +
    "tekno_emitter_version, " +
    "sent_status, " +
    "retry_count " +
    "FROM production_events " +
    "WHERE (sent_status <> 1 OR sent_status IS NULL) " +
    $"AND (retry_count IS NULL OR retry_count < {MAX_RETRIES}) " +
    "ORDER BY utc_time_stamp ASC, event_id ASC " + 
    $"LIMIT {batchSize}";

        var rs = ExecuteQuery(query);

        var result = new List<ProductionEventLocal>();

        if (rs == null || rs.GetLength(0) == 0)
            return result;

        for (int i = 0; i < rs.GetLength(0); i++)
        {
            var rowDebug = "";

            for (int j = 0; j < rs.GetLength(1); j++)
                rowDebug += $"{rs[i, j]} | ";

            result.Add(MapSafe(rs, i));
        }

        return result;
    }

    // =============================
    // MARK AS SENT
    // =============================
    public void MarkAsSent(List<string> keys)
    {
        if (keys == null || keys.Count == 0)
            return;

        var safeKeys = keys
            .Where(k => !string.IsNullOrEmpty(k))
            .Select(k => $"'{k.Replace("'", "''")}'");

        var inClause = string.Join(",", safeKeys);

        string query = $@"
    UPDATE production_events 
    SET sent_status = 1
    WHERE source_event_key IN ({inClause})
    ";

        ExecuteQuery(query);
    }
    // =============================
    // INSERT
    // =============================
    public void InsertEvent(ProductionEventLocal entity)
    {

        if (string.IsNullOrEmpty(entity.SourceEventKey))
            entity.GenerateSourceEventKey();

        if (entity.SourceInsertedAtUtc == null)
            entity.SourceInsertedAtUtc = DateTime.UtcNow;

        var values = new object[]
        {
            entity.SourceEventKey,
            entity.UtcTimeStamp,
            entity.SourceInsertedAtUtc,
            entity.EventType,
            entity.EventId,
            entity.PalletId,
            entity.PalletDestination,
            entity.CustomerId,
            entity.ServerId,
            entity.TeknoDeviceId,
            entity.PlantId,
            entity.DepartmentId,
            entity.LineId,
            entity.StopId,
            entity.BuildResult,
            entity.DefectStationId,
            entity.DefectReason,
            entity.OperatorId,
            entity.PartModel,
            entity.TeknoPayloadVersion,
            entity.TeknoEmitterVersion,
            entity.SentStatus,
            entity.RetryCount
        };

        base.Insert(values);
    }

    public void IncrementRetry(List<string> keys)
    {
        if (keys == null || keys.Count == 0)
            return;


        var safeKeys = keys
            .Where(k => !string.IsNullOrEmpty(k))
            .Select(k => $"'{k.Replace("'", "''")}'");

        var inClause = string.Join(",", safeKeys);

        string query = $@"
    UPDATE production_events 
    SET retry_count = COALESCE(retry_count, 0) + 1
    WHERE source_event_key IN ({inClause})
    ";

        Log.Info($"[Retry] Incrementing retry for {keys.Count} events");

        ExecuteQuery(query);
    }

    public void CleanupBatch(int batchSize)
    {
        string query = $@"
    DELETE FROM production_events
    WHERE rowid IN (
        SELECT rowid FROM production_events
        WHERE sent_status = 1
        LIMIT {batchSize}
    )";

        ExecuteQuery(query);
    }
    private static DateTime GetDateRequired(object value)
    => value == null || value == DBNull.Value
        ? DateTime.UtcNow
        : Convert.ToDateTime(value);
}
