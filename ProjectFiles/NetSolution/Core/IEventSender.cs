using NETCode.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IEventSender
{
    Task<bool> SendAsync(List<ProductionEventLocal> events);

}
