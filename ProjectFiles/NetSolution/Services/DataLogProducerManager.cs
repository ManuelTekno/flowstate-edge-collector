using NETCode.Core;
using NETCode.Services;
using System.Collections.Generic;
using System;
using System.Linq;
using UAManagedCore;
using System.Diagnostics;
using System.Threading;

public class DataLogProducerManager
{
private readonly string _basePath;
private readonly IUANode _owner;
private readonly FlowStateConfig _config;

private readonly ProductionEventLocalRepository _repo;

private readonly List<DataLogMonitor> _monitors = new();
private readonly List<List<DataLogMonitor>> _batches = new();
private readonly List<PlcDataLogService> _plcServices = new();

private readonly List<PeriodicTask> _mainTasks = new();
private PeriodicTask _heartbeatTask;

private bool _heartbeatState = false;
private PlcDataLogService _globalPlc;

private const int BatchSize = 3;

private DateTime _lastHeartbeat = DateTime.UtcNow;

private readonly List<object> _batchLocks = new();


public DataLogProducerManager(string basePath, IUANode owner, FlowStateConfig config, string storePath)
{
    _basePath = basePath;
    _owner = owner;
    _config = config;

    _repo = new ProductionEventLocalRepository(storePath);

    Log.Info($"[DataLogManager] Created.");
}

public void Initialize(List<string> stations)
{
    Log.Info($"[DataLogManager] Initializing {stations.Count} stations...");

    foreach (var station in stations)
    {
        var plcService = new PlcDataLogService(_basePath, station);

        var monitor = new DataLogMonitor(
            station,
            plcService,
            _config,
            _repo
        );

        _monitors.Add(monitor);
        _plcServices.Add(plcService);
    }

    _globalPlc = _plcServices.FirstOrDefault();

    if (_globalPlc == null)
        throw new Exception("No PLC services available for heartbeat.");

    CreateBatches();

    Log.Info($"[DataLogManager] Initialization complete. Batches: {_batches.Count}");
    Log.Info($"[DataLogManager] Locks created: {_batchLocks.Count}");
}

private void CreateBatches()
{
    _batches.Clear();
    _batchLocks.Clear();

    for (int i = 0; i < _monitors.Count; i += BatchSize)
    {
        var batch = _monitors.Skip(i).Take(BatchSize).ToList();
        _batches.Add(batch);

        _batchLocks.Add(new object());
    }
}

public void Start()
{
    Log.Info($"[DataLogManager] Starting {_batches.Count} batch tasks...");

    int batchIndex = 0;

    foreach (var batch in _batches)
    {
        int localIndex = batchIndex;

        var task = new PeriodicTask(() =>
        {
            RunBatch(batch, localIndex);
        },
        2000, 
        _owner);

        _mainTasks.Add(task);
        task.Start();

        batchIndex++;
    }

    _heartbeatTask = new PeriodicTask(Heartbeat, 1000, _owner);
    _heartbeatTask.Start();

    Log.Info($"[DataLogManager] Started.");
}

private void RunBatch(List<DataLogMonitor> batch, int batchIndex)
{
    if (batchIndex >= _batchLocks.Count)
    {
        Log.Error($"[Batch {batchIndex}] Lock index out of range!");
        return;
    }

    if (!Monitor.TryEnter(_batchLocks[batchIndex]))
    {
        Log.Warning($"[Batch {batchIndex}] SKIPPED");
        return;
    }

    var swBatch = Stopwatch.StartNew();

    long before = GetTotalProcessed();

    try
    {
        foreach (var monitor in batch)
        {
            try
            {
                monitor.CheckTriggers();
            }
            catch (Exception ex)
            {
                Log.Error($"[Batch {batchIndex}] Monitor error: {ex}");
            }
        }
    }
    finally
    {
        swBatch.Stop();

        long after = GetTotalProcessed();
        long events = after - before;

        var elapsed = swBatch.ElapsedMilliseconds;

        if (events > 0)
        {
            var perEvent = elapsed / (double)events;
        }

        if (elapsed > 2000)
        {
            Log.Error($"[Batch {batchIndex}] OVERRUN: {elapsed} ms");
        }
        else if (elapsed > 1000)
        {
            Log.Warning($"[Batch {batchIndex}] HIGH LOAD: {elapsed} ms");
        }

        Monitor.Exit(_batchLocks[batchIndex]);
    }
}
private void Heartbeat()
{
    var now = DateTime.UtcNow;
    var delay = (now - _lastHeartbeat).TotalMilliseconds;
    _lastHeartbeat = now;

    if (delay > 2000)
    {
        Log.Warning($"[Heartbeat] DELAY: {delay} ms");
    }

    _heartbeatState = !_heartbeatState;

    try
    {
        _globalPlc.SetGlobalHeartbeat(_heartbeatState);
    }
    catch (Exception ex)
    {
        Log.Error($"[Heartbeat] {ex}");
    }

}
private long GetTotalProcessed()
{
    long total = 0;

    foreach (var monitor in _monitors)
    {
        total += monitor.GetProcessedCount();
    }

    return total;
}

public void Stop()
{
    foreach (var task in _mainTasks)
    {
        task?.Dispose();
    }

    _heartbeatTask?.Dispose();

    _mainTasks.Clear();
    _batches.Clear();
    _monitors.Clear();
    _plcServices.Clear();
    _batchLocks.Clear();

    Log.Info($"[DataLogManager] Stopped.");
}

}
