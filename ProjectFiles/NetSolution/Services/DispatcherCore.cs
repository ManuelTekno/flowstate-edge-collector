using System;
using System.Linq;
using System.Threading.Tasks;
using UAManagedCore;

public class DispatcherCore
{
    private readonly ProductionEventLocalRepository _repo;
    private readonly IEventSender _sender;
    private readonly int _batchSize;

    private const int MAX_RETRIES = 5;

    public DispatcherCore(
        ProductionEventLocalRepository repo,
        IEventSender sender,
        int batchSize)
    {
        _repo = repo;
        _sender = sender;
        _batchSize = batchSize;
    }

    public async Task TickAsync()
    {
        try
        {
            var events = _repo.GetPending(_batchSize);

            if (events == null || events.Count == 0)
                return;

            Log.Info($"[Dispatcher] Sending batch: {events.Count}");

            var keys = events.Select(e => e.SourceEventKey).ToList();

            bool success = await _sender.SendAsync(events);

            if (success)
            {
                _repo.MarkAsSent(keys);

                Log.Info($"[Dispatcher] Batch sent OK: {events.Count}");
            }
            else
            {
                _repo.IncrementRetry(keys);

                var criticalEvents = events
                    .Where(e => (e.RetryCount + 1) >= MAX_RETRIES)
                    .Select(e => e.SourceEventKey)
                    .ToList();

                if (criticalEvents.Any())
                {
                    Log.Error($"[ALARM] Max retries reached for {criticalEvents.Count} events");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Dispatcher] Tick error: {ex.Message}");
        }
    }
}
