using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowState_Magna.Entities;

public class ApiConfig
{
    public string BaseUrl { get; set; }
    public string Route { get; set; }
    public int TimeoutSeconds { get; set; }

    public int RetryCount { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 500;

    public string GetFullUrl()
    {
        return $"{BaseUrl.TrimEnd('/')}/{Route.TrimStart('/')}";
    }
}
