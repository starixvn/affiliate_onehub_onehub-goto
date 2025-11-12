namespace ONEHub.GOTO.Services.HealthCheck
{
    using Models.HealthCheck;

    /// <summary>
    /// Service for managing health check configuration
    /// </summary>
    public interface IHealthCheckConfigurationService
    {
        HealthCheckConfig GetConfiguration();
        bool IsComponentEnabled(string componentName);
        bool IsAuthenticationRequired();
        bool ValidateApiKey(string apiKey);
        HealthThresholds GetThresholds();
        Task<bool> SendAlertAsync(string componentName, string message, string severity);
    }
}