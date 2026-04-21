#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.HMIProject;
using FTOptix.WebUI;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.Alarm;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.ODBCStore;
using FTOptix.RAEtherNetIP;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.DataLogger;
using FTOptix.CommunicationDriver;
using FTOptix.SerialPort;
using FTOptix.Core;
using System.Threading;
using System.Threading.Tasks;
#endregion

public class DataLogDispatcher : BaseNetLogic
{
    private DataLogDispatcherManager _manager;
    private string _localStorePath;
    private string _forwardStorePath;
    private int _batchSize;

    private PeriodicTask _monitorTask;

    public override void Start()
    {
        try
        {
            Log.Info("[Dispatcher] Starting (PeriodicTask mode)...");

            _localStorePath = GetString("Store_Data_Store_Path");
            _forwardStorePath = GetString("Forward_Data_Store_Path");
            _batchSize = GetInt("Dispatcher_Batch_Size");

            _monitorTask = new PeriodicTask(MonitorDispatcher, 3000, LogicObject);
            _monitorTask.Start();

            Log.Info("[Dispatcher] Monitor task started.");
        }
        catch (Exception ex)
        {
            Log.Error($"[Dispatcher] Start failed: {ex}");
        }
    }

    public override void Stop()
    {
        _monitorTask?.Dispose();
        _monitorTask = null;

        _manager?.Stop();
        _manager = null;
    }

    private void MonitorDispatcher()
    {
        try
        {
            var localStore = Project.Current.Get<Store>(_localStorePath);
            var forwardStore = Project.Current.Get<Store>(_forwardStorePath);

            bool localOnline = (int)localStore.Status == 1;
            bool forwardOnline = (int)forwardStore.Status == 1;


            if (localOnline && forwardOnline)
            {
                if (_manager == null)
                {
                    Log.Info("[Dispatcher] ALL ONLINE → Initializing");

                    _manager = new DataLogDispatcherManager(
                        LogicObject,
                        _localStorePath,
                        _forwardStorePath,
                        _batchSize
                    );

                    _manager.Initialize();
                    _manager.Start();

                    Log.Info("[Dispatcher] Started ✔");
                }
            }
            else
            {
                if (!localOnline)
                    Log.Warning("[Dispatcher] LOCAL store OFFLINE");

                if (!forwardOnline)
                    Log.Warning("[Dispatcher] FORWARD store OFFLINE");

                if (_manager != null)
                {
                    Log.Warning("[Dispatcher] Dependency lost → Stopping");

                    _manager.Stop();
                    _manager = null;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[Dispatcher] Monitor error: {ex}");
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
