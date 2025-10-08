namespace HeartbeatPOC.Configuration
{
    public class HeartbeatSettings
    {
        public const string SectionName = "HeartbeatSettings";

        public int ProcessExpirationInSeconds { get; set; } = 30;
        public int MonitorIntervalInSeconds { get; set; } = 5; 
    }
}
