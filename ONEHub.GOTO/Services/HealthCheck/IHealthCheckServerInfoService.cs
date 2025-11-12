namespace ONEHub.GOTO.Services.HealthCheck
{
    /// <summary>
    /// Service for retrieving server and environment information for health checks
    /// </summary>
    public interface IHealthCheckServerInfoService
    {
        /// <summary>
        /// Gets comprehensive server information including IP addresses, environment, and system details
        /// </summary>
        /// <returns>Dictionary containing server information</returns>
        Task<IDictionary<string, object>> GetServerInfoAsync();

        /// <summary>
        /// Gets basic server information without network details
        /// </summary>
        /// <returns>Dictionary containing basic server information</returns>
        IDictionary<string, object> GetBasicServerInfo();
    }
}