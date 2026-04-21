using System;
using System.Collections.Generic;
using NETCode.Entities;
using UAManagedCore;

namespace NETCode.Repositories;

public class ProductionEventCloudRepository : OdbcRepositoryBase<ProductionEventCloud>
{
    public ProductionEventCloudRepository(string storePath)
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
                "tekno_emitter_version"
            },
            (resultSet, row) => new ProductionEventCloud
            {
                SourceEventKey = resultSet[row, 0]?.ToString(),
                UtcTimeStamp = Convert.ToDateTime(resultSet[row, 1]),
                SourceInsertedAtUtc = resultSet[row, 2] == DBNull.Value ? null : Convert.ToDateTime(resultSet[row, 2]),

                EventType = resultSet[row, 3]?.ToString(),
                EventId = Convert.ToInt64(resultSet[row, 4]),

                PalletId = Convert.ToInt32(resultSet[row, 5]),
                PalletDestination = resultSet[row, 6]?.ToString(),

                CustomerId = resultSet[row, 7]?.ToString(),
                ServerId = resultSet[row, 8]?.ToString(),
                TeknoDeviceId = resultSet[row, 9] == DBNull.Value ? null : resultSet[row, 9].ToString(),

                PlantId = resultSet[row, 10]?.ToString(),
                DepartmentId = resultSet[row, 11]?.ToString(),
                LineId = resultSet[row, 12]?.ToString(),
                StopId = resultSet[row, 13]?.ToString(),

                BuildResult = resultSet[row, 14] == DBNull.Value ? null : resultSet[row, 14].ToString(),
                DefectStationId = resultSet[row, 15] == DBNull.Value ? null : resultSet[row, 15].ToString(),
                DefectReason = resultSet[row, 16] == DBNull.Value ? null : resultSet[row, 16].ToString(),

                OperatorId = resultSet[row, 17] == DBNull.Value ? null : resultSet[row, 17].ToString(),
                PartModel = resultSet[row, 18] == DBNull.Value ? null : resultSet[row, 18].ToString(),

                TeknoPayloadVersion = resultSet[row, 19] == DBNull.Value ? null : resultSet[row, 19].ToString(),
                TeknoEmitterVersion = resultSet[row, 20] == DBNull.Value ? null : resultSet[row, 20].ToString()
            },
            storePath
        )
    {
        Log.Info("ProductionEvent Repository instance created");
    }
    public void InsertEvent(ProductionEventCloud entity)
    {

        try
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
            entity.TeknoEmitterVersion
            };

            base.Insert(values);
        }
        catch (Exception ex)
        {
            Log.Error($"[Cloud Insert] Failed: {ex.Message}");
            throw;
        }
    }
    public void InsertBatch(List<ProductionEventLocal> events)
    {
        if (events == null || events.Count == 0)
            return;

        int rows = events.Count;
        int cols = DbColumns.Length;

        object[,] values = new object[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            var e = MapToCloud(events[i]);

            // Normalización (lo que ya hacías)
            if (string.IsNullOrEmpty(e.SourceEventKey))
                e.GenerateSourceEventKey();

            if (e.SourceInsertedAtUtc == null)
                e.SourceInsertedAtUtc = DateTime.UtcNow;

            values[i, 0] = e.SourceEventKey;
            values[i, 1] = e.UtcTimeStamp;
            values[i, 2] = e.SourceInsertedAtUtc;
            values[i, 3] = e.EventType;
            values[i, 4] = e.EventId;
            values[i, 5] = e.PalletId;
            values[i, 6] = e.PalletDestination;
            values[i, 7] = e.CustomerId;
            values[i, 8] = e.ServerId;
            values[i, 9] = e.TeknoDeviceId;
            values[i, 10] = e.PlantId;
            values[i, 11] = e.DepartmentId;
            values[i, 12] = e.LineId;
            values[i, 13] = e.StopId;
            values[i, 14] = e.BuildResult;
            values[i, 15] = e.DefectStationId;
            values[i, 16] = e.DefectReason;
            values[i, 17] = e.OperatorId;
            values[i, 18] = e.PartModel;
            values[i, 19] = e.TeknoPayloadVersion;
            values[i, 20] = e.TeknoEmitterVersion;
        }

        try
        {
            MyTable.Insert(DbColumns, values);
        }
        catch (Exception ex)
        {
            Log.Error($"[Cloud Bulk Insert] Failed: {ex.Message}");
            throw;
        }
    }
    public ProductionEventCloud GetBySourceEventKey(string sourceEventKey)
    {

        string query = $"SELECT * FROM production_events WHERE source_event_key = '{sourceEventKey}'";
        var rs = ExecuteQuery(query);

        if (rs.GetLength(0) == 0)
            return null;

        return MapFunc(rs, 0);
    }

    private ProductionEventCloud MapToCloud(ProductionEventLocal e)
    {
        return new ProductionEventCloud
        {
            SourceEventKey = e.SourceEventKey,
            UtcTimeStamp = e.UtcTimeStamp,
            SourceInsertedAtUtc = DateTime.UtcNow,

            EventType = e.EventType,
            EventId = e.EventId,

            PalletId = e.PalletId,
            PalletDestination = e.PalletDestination,

            CustomerId = e.CustomerId,
            ServerId = e.ServerId,
            TeknoDeviceId = e.TeknoDeviceId,

            PlantId = e.PlantId,
            DepartmentId = e.DepartmentId,
            LineId = e.LineId,
            StopId = e.StopId,

            BuildResult = e.BuildResult,
            DefectStationId = e.DefectStationId,
            DefectReason = e.DefectReason,

            OperatorId = e.OperatorId,
            PartModel = e.PartModel,

            TeknoPayloadVersion = e.TeknoPayloadVersion,
            TeknoEmitterVersion = e.TeknoEmitterVersion
        };
    }
}
