using System;
using NETCode.Repositories;
using UAManagedCore;

public class ProductionEventCleanupTask
{
    private readonly ProductionEventLocalRepository _repo;
    private readonly int _cleanupBatch;

    public ProductionEventCleanupTask(
        ProductionEventLocalRepository repo,
        int cleanupBatch
    )
    {
        _repo = repo;
        _cleanupBatch = cleanupBatch;

    }
    public void Run()
    {
        try
        {
            _repo.CleanupBatch(_cleanupBatch);
        }
        catch (Exception ex)
        {
            Log.Error($"[CleanupTask] Error: {ex.Message}");
        }
    }
}
