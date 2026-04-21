using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FTOptix.HMIProject;
using UAManagedCore;

namespace NETCode.Services;

public class PlcDataLogService
{
    private readonly string _basePath;
    private readonly string _stationName;

    public PlcDataLogService(string basePath, string stationName)
    {
        _basePath = basePath;
        _stationName = stationName;
    }

    private string GetFullPath() =>
        $"{_basePath}/{_stationName}";

    private string GetTriggersPath() =>
        $"{GetFullPath()}/Triggers";

    private IUANode TryGetNode(string fullPath)
    {
        IUANode node = null;
        int retry = 0;

        while (node == null && retry < 5)
        {
            node = Project.Current.Get(fullPath);

            if (node == null)
            {
                Log.Warning($"[DataLog:{_stationName}] Retry {retry + 1} - Node NULL at {fullPath}");
                Thread.Sleep(200);
                retry++;
            }
        }

        return node;
    }

    public Dictionary<string, object> ReadMultipleTags(List<string> tagNames)
    {
        var result = new Dictionary<string, object>();

        var plcNode = TryGetNode(GetFullPath()); // FIXED
        if (plcNode == null)
        {
            Log.Error($"PLC Node is NULL for ReadMultipleTags on station {_stationName}");
            return result;
        }

        try
        {
            var variableList = tagNames
                .Select(name => new RemoteChildVariable(name))
                .ToList();

            var readResults = plcNode
                .ChildrenRemoteRead(variableList)
                .ToList();

            for (int i = 0; i < tagNames.Count; i++)
            {
                var value = readResults[i].Value is UAValue ua
                    ? ua.Value
                    : readResults[i].Value;

                result[tagNames[i]] = value;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"ReadMultipleTags failed for {_stationName}: {ex}");
        }

        return result;
    }

    public void WriteMultipleTags(List<RemoteChildVariableValue> tagList)
    {
        var plcNode = TryGetNode(GetFullPath()); // FIXED
        if (plcNode == null)
        {
            Log.Error($"PLC Node is NULL for WriteMultipleTags on station {_stationName}");
            return;
        }

        try
        {
            plcNode.ChildrenRemoteWrite(tagList);
        }
        catch (Exception ex)
        {
            Log.Error($"WriteMultipleTags failed for {_stationName}: {ex}");
        }
    }

    public bool ReadTrigger(string triggerName)
    {
        var node = TryGetNode(GetTriggersPath());
        if (node == null)
            return false;

        try
        {
            var read = node.ChildrenRemoteRead(
                new List<RemoteChildVariable>
                {
                    new RemoteChildVariable(triggerName)
                })
                .FirstOrDefault();

            var value = read.Value is UAValue ua
                ? ua.Value
                : read.Value;

            return value != null && Convert.ToBoolean(value);
        }
        catch (Exception ex)
        {
            Log.Error($"[DataLog:{_stationName}] ReadTrigger failed: {ex}");
            return false;
        }
    }

    public void ResetTrigger(string triggerName)
    {
        var node = TryGetNode(GetTriggersPath());
        if (node == null)
            return;

        try
        {
            node.ChildrenRemoteWrite(new List<RemoteChildVariableValue>
            {
                new RemoteChildVariableValue(triggerName, new UAValue(false))
            });
        }
        catch (Exception ex)
        {
            Log.Error($"[DataLog:{_stationName}] ResetTrigger failed: {ex}");
        }
    }

    public void SetHeartBeat(bool state)
    {
        var node = TryGetNode(GetFullPath());
        if (node == null)
            return;

        try
        {
            node.ChildrenRemoteWrite(new List<RemoteChildVariableValue>
            {
                new RemoteChildVariableValue("HeartBeat", new UAValue(state))
            });
        }
        catch (Exception ex)
        {
            Log.Error($"[DataLog:{_stationName}] SetHeartBeat failed: {ex}");
        }
    }

    // =============================
    // 🔹 READ MULTIPLE TRIGGERS
    // =============================
    public Dictionary<string, bool> ReadMultipleTriggers(List<string> triggerNames)
    {
        var result = new Dictionary<string, bool>();

        var node = TryGetNode(GetTriggersPath());
        if (node == null)
            return result;

        try
        {
            var variables = triggerNames
                .Select(name => new RemoteChildVariable(name))
                .ToList();

            var reads = node.ChildrenRemoteRead(variables).ToList();

            for (int i = 0; i < triggerNames.Count; i++)
            {
                var value = reads[i].Value is UAValue ua
                    ? ua.Value
                    : reads[i].Value;

                result[triggerNames[i]] =
                    value != null && Convert.ToBoolean(value);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[DataLog:{_stationName}] ReadMultipleTriggers failed: {ex}");
        }

        return result;
    }

    // =============================
    // 🔹 RESET MULTIPLE TRIGGERS
    // =============================
    public void ResetMultipleTriggers(List<string> triggerNames)
    {
        var node = TryGetNode(GetTriggersPath());
        if (node == null)
            return;

        try
        {
            var writes = triggerNames
                .Select(name =>
                    new RemoteChildVariableValue(name, new UAValue(false)))
                .ToList();

            node.ChildrenRemoteWrite(writes);
        }
        catch (Exception ex)
        {
            Log.Error($"[DataLog:{_stationName}] ResetMultipleTriggers failed: {ex}");
        }
    }

    public void SetGlobalHeartbeat(bool state)
    {
        const string heartbeatPath = "Model/PLC_Tags/FT_OPTIX_ONLINE";

        var node = TryGetNode(heartbeatPath);

        if (node == null)
        {
            Log.Error("[GlobalHeartbeat] Node NULL");
            return;
        }

        var variable = node as IUAVariable;

        if (variable == null)
        {
            Log.Error("[GlobalHeartbeat] Node is not a variable");
            return;
        }

        try
        {
            variable.Value = state;
        }
        catch (Exception ex)
        {
            Log.Error($"[GlobalHeartbeat] Write failed: {ex}");
        }
    }
}
