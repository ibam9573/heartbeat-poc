namespace HeartbeatPOC.Models
{
    public class ProcessStatus
    {
        public required string ProcessId { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public int ProcessExpirationInSeconds { get; set; } 
        public bool IsAlive => (DateTime.UtcNow - LastHeartbeat).TotalSeconds <= ProcessExpirationInSeconds;
        public bool KeepAlive { get; set; } 
    }
}
