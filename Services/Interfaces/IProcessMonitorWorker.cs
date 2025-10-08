using Microsoft.Extensions.Hosting;

namespace HeartbeatPOC.Services.Interfaces
{
    public interface IProcessMonitorWorker : IHostedService
    {
        // BackgroundService already implements IHostedService
    }
}
