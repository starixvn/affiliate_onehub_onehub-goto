namespace ONEHub.GOTO.Services.HealthCheck
{
    using Models.HealthCheck;

    /// <summary>
    /// Service for performing comprehensive system health checks
    /// </summary>
    public interface IHealthCheckService
    {
        Task<HealthStatus> GetHealthStatusAsync();
        Task<ComponentHealth> CheckDatabaseHealthAsync();
        Task<ComponentHealth> CheckRedisHealthAsync();
        Task<ComponentHealth> CheckRabbitMQHealthAsync();
        Task<ComponentHealth> CheckUnderMaintenanceServiceAsync();
        Task<SystemMetrics> GetSystemMetricsAsync();
    }
}