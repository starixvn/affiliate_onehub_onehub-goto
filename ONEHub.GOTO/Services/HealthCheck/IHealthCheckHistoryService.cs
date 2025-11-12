namespace ONEHub.GOTO.Services.HealthCheck
{
    using Models.HealthCheck;

    /// <summary>
    /// Service for tracking health check history and generating reports
    /// </summary>
    public interface IHealthCheckHistoryService
    {
        Task RecordHealthCheckAsync(HealthStatus healthStatus);
        Task<List<HealthCheckHistory>> GetHistoryAsync(DateTime startTime, DateTime endTime, int maxRecords = 100);
        Task<HealthCheckSummary> GetSummaryAsync(DateTime startTime, DateTime endTime);
        Task<List<ComponentSummary>> GetComponentTrendsAsync(string componentName, DateTime startTime, DateTime endTime);
        Task CleanupOldRecordsAsync(TimeSpan retentionPeriod);
        Task<Dictionary<string, object>> GetRealTimeMetricsAsync();
    }
}