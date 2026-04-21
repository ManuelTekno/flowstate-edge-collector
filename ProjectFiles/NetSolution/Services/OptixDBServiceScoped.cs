using NETCode.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class OptixDBServiceScoped
{
    public ProductionEventLocalRepository ProductionEventLocalRepo { get; private set; }
    public ProductionEventCloudRepository ProductionEventCloudRepo { get; private set; }



    public OptixDBServiceScoped(string storePath)
    {
        ProductionEventLocalRepo = new ProductionEventLocalRepository(storePath);
        ProductionEventCloudRepo = new ProductionEventCloudRepository(storePath);

    }
}
