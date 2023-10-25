using PortalWorkerTask.Infra.CrossCutting.Services.Options;

namespace PortalWorkerTask.Infra.CrossCutting.Services.Services;

public interface IRabbitConnectorService
{
    void ConnectQueue(Func<string, bool, Task> func);
}
