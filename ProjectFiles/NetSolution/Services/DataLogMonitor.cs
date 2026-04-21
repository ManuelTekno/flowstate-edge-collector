using System;
using System.Collections.Generic;
using System.Threading;
using NETCode.Core;
using NETCode.Entities;
using UAManagedCore;
using System.Linq;

namespace NETCode.Services;

public class DataLogMonitor
{
    private readonly string _stationName;
    private readonly PlcDataLogService _plc;
    private readonly ProductionEventLocalRepository _repo;
    private readonly FlowStateConfig _config;

    private readonly List<string> _triggerNames;
    private readonly Dictionary<string, bool> _lastState = new();
    private readonly Dictionary<string, int> _highCycles = new();

    private const int STUCK_THRESHOLD = 5;

    private readonly Queue<EventSnapshot> _eventQueue = new();
    private readonly object _queueLock = new();

    private const int MAX_QUEUE_SIZE = 10;
    private const int MAX_RETRIES = 3;

    private long _eventCounter = 0;

    private static readonly SemaphoreSlim _plcSemaphore = new(1);

    private static readonly TimeZoneInfo _centralZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    private static readonly List<string> _tagNames = new()
    {
        "PalletID","StopID","BuildResult","DefectStationID",
        "DefectReason","OperatorID","PartModel","LocalTimeStamp","PalletDestination"
    };

    private static readonly Dictionary<DataLogTrigger, int> _stageOrder = new()
    {
        { DataLogTrigger.Ready_to_Receive, 1 },
        { DataLogTrigger.Pallet_Arrived, 2 },
        { DataLogTrigger.PalletID_Read, 3 },
        { DataLogTrigger.Ready_to_Send, 4 },
        { DataLogTrigger.Released, 5 },
        { DataLogTrigger.Pallet_Departed, 6 }
    };

    private int GetStageOrder(DataLogTrigger trigger)
    {
        return _stageOrder.TryGetValue(trigger, out var order)
            ? order
            : int.MaxValue;
    }

    public DataLogMonitor(
        string stationName,
        PlcDataLogService plc,
        FlowStateConfig config,
        ProductionEventLocalRepository repo)
    {
        _stationName = stationName;
        _plc = plc;
        _config = config;
        _repo = repo;

        _triggerNames = new List<string>(Enum.GetNames(typeof(DataLogTrigger)));

        foreach (var t in _triggerNames)
        {
            _lastState[t] = false;
            _highCycles[t] = 0;
        }
    }

    public void CheckTriggers()
    {
        var states = _plc.ReadMultipleTriggers(_triggerNames);

        if (states == null || states.Count == 0)
            return;

        var plcData = ReadPLCData();

        foreach (var kvp in states)
        {
            string name = kvp.Key;
            bool value = kvp.Value;

            if (value)
            {
                _highCycles[name]++;

                bool isFirst = !_lastState[name];
                bool isStuck = _highCycles[name] >= STUCK_THRESHOLD;

                if (isFirst || isStuck)
                {
                    if (isStuck)
                        Log.Warning($"[FlowState:{_stationName}] STUCK HIGH: {name}");

                    var trigger = Enum.Parse<DataLogTrigger>(name);

                    var snapshot = new EventSnapshot
                    {
                        Trigger = trigger,
                        PlcData = plcData,
                        CapturedAtUtc = DateTime.UtcNow
                    };

                    EnqueueSnapshot(snapshot);

                    _lastState[name] = true;
                    _highCycles[name] = 0;
                }

                _plc.ResetTrigger(name);
            }
            else
            {
                _highCycles[name] = 0;
                _lastState[name] = false;
            }
        }

        ProcessQueue();
    }

    private void EnqueueSnapshot(EventSnapshot snapshot)
    {
        lock (_queueLock)
        {
            if (_eventQueue.Count >= MAX_QUEUE_SIZE)
            {
                Log.Error($"[FlowState:{_stationName}] QUEUE OVERFLOW");
                return;
            }

            snapshot.RetryCount = 0;
            _eventQueue.Enqueue(snapshot);
        }
    }

    private void ProcessQueue()
    {
        try
        {
            List<EventSnapshot> batch;

            lock (_queueLock)
            {
                if (_eventQueue.Count == 0)
                    return;

                batch = _eventQueue.ToList();
                _eventQueue.Clear();
            }

            bool hasAdvanced = batch.Any(e =>
                e.Trigger == DataLogTrigger.Released ||
                e.Trigger == DataLogTrigger.Pallet_Departed);

            var ordered = batch
                .OrderBy(e =>
                {
                    if (hasAdvanced && e.Trigger == DataLogTrigger.Ready_to_Receive)
                        return int.MaxValue;

                    return GetStageOrder(e.Trigger);
                })
                .ThenBy(e => e.CapturedAtUtc)
                .ToList();

            int offsetMs = 0;

            foreach (var snapshot in ordered)
            {
                try
                {
                    ProcessSnapshot(snapshot, offsetMs);
                    offsetMs++;
                }
                catch (Exception ex)
                {
                    snapshot.RetryCount++;

                    Log.Warning($"[FlowState:{_stationName}] Retry {snapshot.RetryCount}");

                    lock (_queueLock)
                    {
                        if (snapshot.RetryCount < MAX_RETRIES)
                            _eventQueue.Enqueue(snapshot);
                        else
                            Log.Error($"[FlowState:{_stationName}] DROPPED");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[FATAL][ProcessQueue] {ex}");
        }
    }

    private DataLogFromPLC ReadPLCData()
    {
        _plcSemaphore.Wait();

        try
        {
            var values = _plc.ReadMultipleTags(_tagNames);

            return new DataLogFromPLC
            {
                PalletID = values["PalletID"]?.ToString(),
                StopID = values["StopID"]?.ToString(),
                BuildResult = values["BuildResult"]?.ToString(),
                DefectStationID = values["DefectStationID"]?.ToString(),
                DefectReason = values["DefectReason"]?.ToString(),
                OperatorID = values["OperatorID"]?.ToString(),
                PartModel = values["PartModel"]?.ToString(),
                LocalTimeStamp = values["LocalTimeStamp"]?.ToString(),
                PalletDestination = values["PalletDestination"]?.ToString()
            };
        }
        finally
        {
            _plcSemaphore.Release();
        }
    }

    private void ProcessSnapshot(EventSnapshot snapshot, int offsetMs)
    {
        var trigger = snapshot.Trigger;
        var plcData = snapshot.PlcData;

        int palletId = int.TryParse(plcData.PalletID, out int id) ? id : 0;

        DateTime utcTimestamp;

        if (DateTime.TryParse(plcData.LocalTimeStamp, out var parsed))
        {
            utcTimestamp = DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                .AddMilliseconds(offsetMs);
        }
        else
        {
            Log.Warning("Invalid PLC timestamp, using fallback");
            utcTimestamp = DateTime.UtcNow.AddMilliseconds(offsetMs);
        }

        bool isContextFreeEvent =
            trigger == DataLogTrigger.Station_Up ||
            trigger == DataLogTrigger.Station_Down ||
            trigger == DataLogTrigger.Line_Up ||
            trigger == DataLogTrigger.Line_Down ||
            trigger == DataLogTrigger.Ready_to_Receive;

        if (isContextFreeEvent)
        {
            palletId = 0;
            plcData.BuildResult = null;
            plcData.DefectStationID = null;
            plcData.DefectReason = null;
            plcData.PartModel = null;
            plcData.PalletDestination = string.Empty;
        }

        var entity = new ProductionEventLocal
        {
            EventType = trigger.ToString(),
            EventId = Interlocked.Increment(ref _eventCounter),
            PalletId = palletId,
            StopId = plcData.StopID,
            BuildResult = plcData.BuildResult,
            DefectStationId = plcData.DefectStationID,
            DefectReason = plcData.DefectReason,
            OperatorId = plcData.OperatorID,
            PartModel = plcData.PartModel,
            UtcTimeStamp = utcTimestamp,
            SourceInsertedAtUtc = DateTime.UtcNow,
            CustomerId = _config.CustomerId,
            ServerId = _config.ServerId,
            TeknoDeviceId = _config.TeknoDeviceId,
            PlantId = _config.PlantId,
            DepartmentId = _config.DepartmentId,
            LineId = _config.LineId,
            TeknoPayloadVersion = _config.TeknoPayloadVersion,
            TeknoEmitterVersion = _config.TeknoEmitterVersion,
            SentStatus = 0
        };

        entity.GenerateSourceEventKey();
        _repo.InsertEvent(entity);
    }

    private class EventSnapshot
    {
        public DataLogTrigger Trigger { get; set; }
        public DataLogFromPLC PlcData { get; set; }
        public DateTime CapturedAtUtc { get; set; }
        public int RetryCount { get; set; }
    }

    public long GetProcessedCount()
    {
        return Interlocked.Read(ref _eventCounter);
    }
}
