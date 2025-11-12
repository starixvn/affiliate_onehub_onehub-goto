using ONEHub.GOTO.Helpers;

namespace ONEHub.GOTO.Models.HealthCheck
{
    /// <summary>
    /// Overall system health status
    /// </summary>
    public class HealthStatus
    {
        public string Status { get; set; } = "Unknown";
        public DateTime Timestamp { get; set; } = DateTimeHelper.ServerTime;
        public TimeSpan TotalDuration { get; set; }
        public Dictionary<string, ComponentHealth> Components { get; set; } = new();
        public SystemMetrics Metrics { get; set; } = new();

        public bool IsHealthy => Status == "Healthy";
        public string Summary => $"{Components.Count(c => c.Value.IsHealthy)}/{Components.Count} components healthy";
    }
}