namespace ONEHub.GOTO.Models.HealthCheck
{
    /// <summary>
    /// Historical health check data point
    /// </summary>
    public class HealthCheckHistory
    {
        public DateTime Timestamp { get; set; }
        public string OverallStatus { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public Dictionary<string, ComponentHealthHistory> Components { get; set; } = new();
        public SystemMetricsHistory Metrics { get; set; } = new();
    }

    public class ComponentHealthHistory
    {
        public string Name { get; set; }
        public string Status { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SystemMetricsHistory
    {
        public long MemoryUsedBytes { get; set; }
        public int ActiveThreads { get; set; }
        public TimeSpan Uptime { get; set; }
        public double CpuUsagePercent { get; set; }
    }

    public class HealthCheckSummary
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int TotalChecks { get; set; }
        public int HealthyChecks { get; set; }
        public int WarningChecks { get; set; }
        public int UnhealthyChecks { get; set; }
        private const double PercentageMultiplier = 100.0;
        
        public double UptimePercent => TotalChecks > 0 ? (double)HealthyChecks / TotalChecks * PercentageMultiplier : 0;
        public Dictionary<string, ComponentSummary> ComponentSummaries { get; set; } = new();
    }

    public class ComponentSummary
    {
        public string ComponentName { get; set; }
        public int TotalChecks { get; set; }
        public int HealthyChecks { get; set; }
        public int WarningChecks { get; set; }
        public int UnhealthyChecks { get; set; }
        public double AverageResponseTimeMs { get; set; }
        private const double PercentageMultiplier = 100.0;
        public double UptimePercent => TotalChecks > 0 ? (double)HealthyChecks / TotalChecks * PercentageMultiplier : 0;
        public List<string> RecentErrors { get; set; } = new();
    }
}