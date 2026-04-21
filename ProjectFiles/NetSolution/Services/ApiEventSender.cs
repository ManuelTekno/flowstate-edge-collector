using FlowState_Magna.Entities;
using NETCode.Entities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UAManagedCore;

namespace FlowState_Magna.Services;

public class ApiEventSender : IEventSender
{
    private readonly ApiConfig _config;
    private readonly HttpClient _client;

    public ApiEventSender(ApiConfig config)
    {
        _config = config;

        _client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(_config.TimeoutSeconds)
        };
    }

    public async Task<bool> SendAsync(List<ProductionEventLocal> events)
    {
        if (events == null || events.Count == 0)
            return true;

        var url = _config.GetFullUrl();

        if (string.IsNullOrEmpty(url))
        {
            Log.Error("[API] URL is empty");
            return false;
        }

        var json = System.Text.Json.JsonSerializer.Serialize(events);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        int maxRetries = _config.RetryCount;
        int delayMs = _config.RetryDelayMs;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                Log.Warning($"[API] Failed ({response.StatusCode}) attempt {attempt}");

                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                {
                    return false;
                }
            }
            catch (TaskCanceledException)
            {
                Log.Warning($"[API] Timeout attempt {attempt}");
            }
            catch (HttpRequestException ex)
            {
                Log.Warning($"[API] Network error attempt {attempt}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"[API] Unexpected error: {ex.Message}");
                return false; // ❗ error desconocido → no retry infinito
            }

            await Task.Delay(delayMs * attempt);
        }

        Log.Error("[API] All retries failed");

        return false;
    }
}
