using FlowState_Magna.Entities;
using FlowState_Magna_R01.Services;
using NETCode.Core;
using NETCode.Repositories;
using NETCode.Services;
using System.Collections.Generic;
using UAManagedCore;

public class DataLogDispatcherManager
{
    private readonly IUANode _owner;
    private readonly ProductionEventLocalRepository _localRepo;
    private readonly ProductionEventCloudRepository _forwardRepo;

    private readonly int _batchSize;
    private int _pollingTimeMs;

    private readonly List<PeriodicTask> _tasks = new();
    private readonly List<DispatcherCore> _dispatchers = new();

    public DataLogDispatcherManager(
        IUANode owner,
        string localStorePath,
        string forwardStorePath,
        int batchSize
    )
    {
        _owner = owner;

        _localRepo = new ProductionEventLocalRepository(localStorePath);
        _forwardRepo = new ProductionEventCloudRepository(forwardStorePath);

        _batchSize = batchSize;

        Log.Info($"[DispatcherManager] Created. BatchSize: {_batchSize}");
    }

    public void Initialize()
    {
        Log.Info("[DispatcherManager] Initializing...");

        _pollingTimeMs = LoadPollingTime();

        var sender = new DbEventSender(_forwardRepo);

        var dispatcher = new DispatcherCore(_localRepo, sender, _batchSize);

        _dispatchers.Add(dispatcher);

        Log.Info($"[DispatcherManager] Polling time: {_pollingTimeMs} ms");

        var task = new PeriodicTask(
            async () => await dispatcher.TickAsync(),
            _pollingTimeMs,
            _owner
        );

        _tasks.Add(task);

        Log.Info("[DispatcherManager] Initialized.");
    }

    private int LoadPollingTime()
    {
        var pollingStr = _owner.GetVariable("Forward_Polling_Time_MS")?.Value?.Value?.ToString();

        Log.Info($"[CONFIG] Forward_Polling_Time_MS={pollingStr}");

        if (!int.TryParse(pollingStr, out int polling))
        {
            Log.Warning("[CONFIG] Invalid Forward_Polling_Time_MS, using default 5000 ms");
            polling = 5000;
        }

        if (polling < 500)
        {
            Log.Warning("[CONFIG] Polling too low, forcing minimum 500 ms");
            polling = 500;
        }

        return polling;
    }

    public void Start()
    {
        foreach (var task in _tasks)
            task.Start();

        Log.Info($"[DispatcherManager] Started {_tasks.Count} tasks.");
    }

    public void Stop()
    {
        foreach (var task in _tasks)
            task.Dispose();

        _tasks.Clear();
        _dispatchers.Clear();

        Log.Info("[DispatcherManager] Stopped.");
    }
}
