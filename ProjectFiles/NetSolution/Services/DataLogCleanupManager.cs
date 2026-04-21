using NETCode.Repositories;
using System.Collections.Generic;
using UAManagedCore;

public class DataLogCleanupManager
{
    private readonly IUANode _owner;
    private readonly ProductionEventLocalRepository _repo;

    private readonly int _cleanupBatch;
    private readonly int _cleanupInterval;

    private readonly List<PeriodicTask> _tasks = new();
    private bool _running = false;

    public DataLogCleanupManager(
        IUANode owner,
        string localStorePath,
        int cleanupBatch,
        int cleanupInterval)
    {
        _owner = owner;
        _repo = new ProductionEventLocalRepository(localStorePath);

        _cleanupBatch = cleanupBatch;
        _cleanupInterval = cleanupInterval;

        Log.Info($"[CleanupManager] Created. Retention={_cleanupBatch}h | Interval={_cleanupInterval}ms");
    }

    public void Initialize()
    {
        var task = new PeriodicTask(
            RunCleanup,
            _cleanupInterval,
            _owner
        );

        _tasks.Add(task);

        Log.Info("[CleanupManager] Initialized.");
    }

    private void RunCleanup()
    {
        if (_running)
            return;

        _running = true;

        try
        {

            _repo.CleanupBatch(_cleanupBatch);
        }
        catch (System.Exception ex)
        {
            Log.Error($"[Cleanup] Error: {ex.Message}");
        }
        finally
        {
            _running = false;
        }
    }

    public void Start()
    {
        foreach (var t in _tasks)
            t.Start();

        Log.Info("[CleanupManager] Started.");
    }

    public void Stop()
    {
        foreach (var t in _tasks)
            t.Dispose();

        _tasks.Clear();

        Log.Info("[CleanupManager] Stopped.");
    }
}
