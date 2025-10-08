using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HeartbeatPOC.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using HeartbeatPOC.Configuration;
using HeartbeatPOC.Services.Interfaces;

namespace HeartbeatPOC.Services
{
    public class ProcessMonitorWorker : BackgroundService, IProcessMonitorWorker
    {
        private readonly ILogger<ProcessMonitorWorker> _logger;
        private readonly IHeartbeatRegistry _heartbeatRegistry;
        private readonly HeartbeatSettings _settings;

        public ProcessMonitorWorker(ILogger<ProcessMonitorWorker> logger, IHeartbeatRegistry heartbeatRegistry, IOptions<HeartbeatSettings> settings)
        {
            _logger = logger;
            _heartbeatRegistry = heartbeatRegistry;
            _settings = settings.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ProcessMonitorWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Get all processes that are currently alive
                var activeProcesses = _heartbeatRegistry.GetStatuses().Where(p => p.IsAlive).ToList();

                if (activeProcesses.Any())
                {
                    _logger.LogInformation("--- Active Processes ({Count}) ---", activeProcesses.Count);
                    foreach (var process in activeProcesses)
                    {
                        _logger.LogInformation("  ProcessId: {ProcessId}, LastHeartbeat: {LastHeartbeat}, IsAlive: {IsAlive}, KeepAlive: {KeepAlive}",
                            process.ProcessId, process.LastHeartbeat, process.IsAlive, process.KeepAlive);

                        // For processes marked as KeepAlive, simulate their continuous activity
                        if (process.KeepAlive)
                        {
                            _heartbeatRegistry.RegisterHeartbeat(process.ProcessId);
                        }
                    }
                    _logger.LogInformation("---------------------------------");
                }
                else
                {
                    _logger.LogInformation("No active processes to log.");
                }

                await Task.Delay(TimeSpan.FromSeconds(_settings.MonitorIntervalInSeconds), stoppingToken); // Log every X seconds from config
            }

            _logger.LogInformation("ProcessMonitorWorker stopped.");
        }
    }
}
