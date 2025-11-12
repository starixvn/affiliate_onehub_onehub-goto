namespace ONEHub.GOTO.Models.HealthCheck
{
    /// <summary>
    /// System performance metrics
    /// </summary>
    public class SystemMetrics
    {
        public double CpuUsagePercent { get; set; }
        public long MemoryUsedBytes { get; set; }
        public long MemoryTotalBytes { get; set; }
        private const double PercentageMultiplier = 100.0;
        
        public double MemoryUsagePercent => MemoryTotalBytes > 0 ? (double)MemoryUsedBytes / MemoryTotalBytes * PercentageMultiplier : 0;
        public long DiskUsedBytes { get; set; }
        public long DiskTotalBytes { get; set; }
        public double DiskUsagePercent => DiskTotalBytes > 0 ? (double)DiskUsedBytes / DiskTotalBytes * PercentageMultiplier : 0;
        public int ActiveThreads { get; set; }
        public int ActiveConnections { get; set; }
        public TimeSpan Uptime { get; set; }
        public string Version { get; set; }
        public string Environment { get; set; }

        public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    }
}