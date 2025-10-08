using HeartbeatPOC.Models;

namespace HeartbeatPOC.Services.Interfaces
{
    public interface IHeartbeatRegistry
    {
        string CreateProcess(bool keepAlive);
        void RegisterHeartbeat(string processId);
        IEnumerable<ProcessStatus> GetStatuses();
        IEnumerable<ProcessStatus> GetActiveStatuses();
        void RemoveHeartbeat(string processId);
    }
}
