using NETCode.Entities;
using NETCode.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UAManagedCore;

namespace FlowState_Magna_R01.Services;

public class DbEventSender : IEventSender
{
    private readonly ProductionEventCloudRepository _repo;

    public DbEventSender(ProductionEventCloudRepository repo)
    {
        _repo = repo;
    }

    public async Task<bool> SendAsync(List<ProductionEventLocal> events)
    {
        try
        {
            _repo.InsertBatch(events);
            return true;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Duplicate entry"))
            {
                Log.Warning($"[DB Sender] Duplicate detected in batch of {events.Count}");

                if (events.Count == 1)
                {
                    Log.Warning("[DB Sender] Skipping duplicate event");
                    return true;
                }

                int half = events.Count / 2;

                var first = events.Take(half).ToList();
                var second = events.Skip(half).ToList();

                bool ok1 = await SendAsync(first);
                bool ok2 = await SendAsync(second);

                return ok1 && ok2;
            }

            Log.Error($"[DB Sender] Failed: {ex.Message}");
            return false;
        }
    }
}