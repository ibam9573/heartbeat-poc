using System.Collections.Concurrent;
using HeartbeatPOC.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using HeartbeatPOC.Configuration;
using HeartbeatPOC.Services.Interfaces;

namespace HeartbeatPOC.Services
{
    public class HeartbeatRegistry : IHeartbeatRegistry
    {
        private readonly ConcurrentDictionary<string, ProcessStatus> _processStatuses = new ConcurrentDictionary<string, ProcessStatus>();
        private readonly ILogger<HeartbeatRegistry> _logger;
        private readonly HeartbeatSettings _settings;

        public HeartbeatRegistry(ILogger<HeartbeatRegistry> logger, IOptions<HeartbeatSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public string CreateProcess(bool keepAlive)
        {
            string processId = (keepAlive ? "LongLivedProcess_" : "ShortLivedProcess_") + Guid.NewGuid().ToString().Substring(0, 4);
            _processStatuses.TryAdd(processId, new ProcessStatus { ProcessId = processId, LastHeartbeat = DateTime.UtcNow, KeepAlive = keepAlive, ProcessExpirationInSeconds = _settings.ProcessExpirationInSeconds });
            _logger.LogInformation("Process created: {ProcessId} (KeepAlive: {KeepAlive}) at: {Time}", processId, keepAlive, DateTimeOffset.Now);
            return processId;
        }

        public void RegisterHeartbeat(string processId)
        {
            _processStatuses.AddOrUpdate(
                processId,
                new ProcessStatus { ProcessId = processId, LastHeartbeat = DateTime.UtcNow, KeepAlive = false, ProcessExpirationInSeconds = _settings.ProcessExpirationInSeconds },
                (key, existingStatus) => {
                    existingStatus.LastHeartbeat = DateTime.UtcNow;
                    return existingStatus;
                }
            );
            _logger.LogInformation("Heartbeat received for process {ProcessId} at: {Time}", processId, DateTimeOffset.Now);
        }

        public IEnumerable<ProcessStatus> GetStatuses()
        {
            return _processStatuses.Values.ToList();
        }

        public IEnumerable<ProcessStatus> GetActiveStatuses()
        {
            return _processStatuses.Values.Where(p => p.IsAlive).ToList();
        }

        public void RemoveHeartbeat(string processId)
        {
            _processStatuses.TryRemove(processId, out _);
        }
    }
}
