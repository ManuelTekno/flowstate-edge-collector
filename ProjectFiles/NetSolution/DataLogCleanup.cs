#region Using directives
using System;
using UAManagedCore;
using FTOptix.NetLogic;
using FTOptix.Store;
using FTOptix.Core;
using FTOptix.HMIProject;
#endregion

public class DataLogCleanup : BaseNetLogic
{
    private DataLogCleanupManager _manager;

    private string _localStorePath;
    private int _cleanupBatch;
    private int _cleanupInterval;

    private PeriodicTask _monitorTask;

    public override void Start()
    {
        try
        {
            Log.Info("[Cleanup] Starting (PeriodicTask mode)...");

            _localStorePath = GetString("Store_Data_Store_Path");
            _cleanupBatch = GetInt("Cleanup_Batch");
            _cleanupInterval = GetInt("Cleanup_Polling_Time_MS");


            _monitorTask = new PeriodicTask(MonitorCleanup, _cleanupInterval, LogicObject);
            _monitorTask.Start();

            Log.Info("[Cleanup] Monitor task started.");
        }
        catch (Exception ex)
        {
            Log.Error($"[Cleanup] Start failed: {ex}");
        }
    }

    public override void Stop()
    {
        _monitorTask?.Dispose();
        _monitorTask = null;

        _manager?.Stop();
        _manager = null;
    }

    private void MonitorCleanup()
    {
        try
        {
            var localStore = Project.Current.Get<Store>(_localStorePath);
            bool localOnline = (int)localStore.Status == 1;

            if (localOnline)
            {
                if (_manager == null)
                {
                    Log.Info("[Cleanup] LOCAL ONLINE → Initializing");

                    _manager = new DataLogCleanupManager(
                        LogicObject,
                        _localStorePath,
                        _cleanupBatch,
                        _cleanupInterval
                    );

                    _manager.Initialize();
                    _manager.Start();

                    Log.Info("[Cleanup] Started ✔");
                }
            }
            else
            {
                Log.Warning("[Cleanup] LOCAL store OFFLINE");

                if (_manager != null)
                {
                    Log.Warning("[Cleanup] Dependency lost → Stopping");

                    _manager.Stop();
                    _manager = null;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Cleanup] Monitor error: {ex}");
        }
    }

    private string GetString(string variableName)
    {
        var variable = LogicObject.GetVariable(variableName);
        return variable?.Value?.Value?.ToString() ?? string.Empty;
    }

    private int GetInt(string variableName)
    {
        try
        {
            var variable = LogicObject.GetVariable(variableName);

            if (variable == null || variable.Value == null || variable.Value.Value == null)
            {
                Log.Warning($"[GetInt] Variable '{variableName}' not found or null. Using default 0.");
                return 0;
            }

            if (variable.Value.Value is int intValue)
                return intValue;

            if (int.TryParse(variable.Value.Value.ToString(), out int parsed))
                return parsed;

            Log.Warning($"[GetInt] Could not parse '{variableName}'. Value: {variable.Value.Value}");
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error($"[GetInt] Error reading '{variableName}': {ex.Message}");
            return 0;
        }
    }
}
