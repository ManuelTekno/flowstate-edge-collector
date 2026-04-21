using System;

namespace NETCode.Entities;

public class ProductionEventLocal
{

    // Idempotency
    public string SourceEventKey { get; set; }

    // Timestamps
    public DateTime UtcTimeStamp { get; set; }
    public DateTime? SourceInsertedAtUtc { get; set; }

    // Event core
    public string EventType { get; set; }
    public long EventId { get; set; }

    // Pallet
    public int PalletId { get; set; }
    public string PalletDestination { get; set; }

    // Context
    public string CustomerId { get; set; }
    public string ServerId { get; set; }
    public string TeknoDeviceId { get; set; }

    public string PlantId { get; set; }
    public string DepartmentId { get; set; }
    public string LineId { get; set; }
    public string StopId { get; set; }

    // Quality
    public string BuildResult { get; set; }
    public string DefectStationId { get; set; }
    public string DefectReason { get; set; }

    // Operator / Product
    public string OperatorId { get; set; }
    public string PartModel { get; set; }

    // Versioning
    public string TeknoPayloadVersion { get; set; }
    public string TeknoEmitterVersion { get; set; }

    // Trazability

    public int SentStatus { get; set; }
    public int RetryCount { get; set; }

    public void GenerateSourceEventKey()
    {
    const string EMULATED_PREFIX = "EMULATED_";

    SourceEventKey = $"{EMULATED_PREFIX}{CustomerId}|{PlantId}|{DepartmentId}|{LineId}|{StopId}|{EventType}|{UtcTimeStamp:yyyy-MM-ddTHH:mm:ss.fffZ}|{PalletId}|{EventId}";
    }
}
