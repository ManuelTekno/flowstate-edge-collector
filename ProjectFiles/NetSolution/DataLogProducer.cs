using FTOptix.HMIProject;
using FTOptix.NetLogic;
using NETCode.Core;
using System.Collections.Generic;
using System;
using UAManagedCore;
using FTOptix.RAEtherNetIP;
using FTOptix.CommunicationDriver;

public class DataLogProducer : BaseNetLogic
{
    private DataLogProducerManager _manager;
    private string _storePath;
    private PeriodicTask _monitorTask;

    public override void Start()
    {
        try
        {
            Log.Info("[FlowState] Starting...");

            _storePath = GetString("Data_Store_Path");

            _monitorTask = new PeriodicTask(MonitorStore, 2000, LogicObject);
            _monitorTask.Start();

            Log.Info("[FlowState] Monitor task started.");
        }
        catch (Exception ex)
        {
            Log.Error($"[FlowState] Start failed: {ex}");
        }
    }

    public override void Stop()
    {
        try
        {
            Log.Info("[FlowState] Stopping...");

            _monitorTask?.Dispose();
            _monitorTask = null;

            _manager?.Stop();
            _manager = null;

            Log.Info("[FlowState] Stopped.");
        }
        catch (Exception ex)
        {
            Log.Error($"[FlowState] Stop failed: {ex}");
        }
    }

    private void MonitorStore()
    {
        try
        {
            var store = Project.Current.Get<FTOptix.Store.Store>(_storePath);
            bool online = (int)store.Status == 1;

            if (online)
            {
                if (_manager == null)
                {
                    Log.Info("[FlowState] Store ONLINE → Initializing manager");
                    InitManager();
                }
            }
            else
            {
                if (_manager != null)
                {
                    Log.Warning("[FlowState] Store OFFLINE → Stopping manager");

                    _manager.Stop();
                    _manager = null;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[FlowState] Monitor error: {ex}");
        }
    }

    private void InitManager()
    {
        var config = new FlowStateConfig
        {
            CustomerId = GetString("Customer_ID"),
            ServerId = GetString("Server_ID"),
            TeknoDeviceId = GetString("Tekno_Device_ID"),
            PlantId = GetString("Plant_ID"),
            DepartmentId = GetString("Department_ID"),
            LineId = GetString("Line_ID"),
            TeknoPayloadVersion = GetString("Tekno_Payload_Version"),
            TeknoEmitterVersion = GetString("Tekno_Emitter_Version")
        };

        var stations = GetStringArray("PLC_Tag_Names");
        var basePath = GetString("PLC_Base_Path");

        _manager = new DataLogProducerManager(basePath, LogicObject, config, _storePath);

        _manager.Initialize(stations);
        _manager.Start();

        Log.Info("[FlowState] Manager started ✔");
    }

    private string GetString(string variableName)
    {
        var variable = LogicObject.GetVariable(variableName);
        return variable?.Value?.Value?.ToString() ?? string.Empty;
    }

    private List<string> GetStringArray(string variableName)
    {
        var result = new List<string>();
        var variable = LogicObject.GetVariable(variableName);

        if (variable?.Value?.Value is object[] array)
        {
            foreach (var item in array)
            {
                var str = item?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(str) && str != "0")
                    result.Add(str);
            }
        }

        return result;
    }
}
