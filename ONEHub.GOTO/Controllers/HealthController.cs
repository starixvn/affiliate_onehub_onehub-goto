namespace ONEHub.GOTO.Controllers
{
    using System.Text.Json;
    using Attributes;
    using Microsoft.AspNetCore.Mvc;
    using Models.HealthCheck;
    using ONEHub.GOTO.Helpers;
    using Services.HealthCheck;

    /// <summary>
    /// Health check endpoints for monitoring system status
    /// </summary>
    [Route("healthz")]
    public class HealthController : Controller
    {
        private readonly IHealthCheckService _healthCheckService;
        private readonly IHealthCheckServerInfoService _serverInfoService;
        private readonly ILogger<HealthController> _logger;

        // HTTP Status Codes
        private const int HttpStatusOk = 200;
        private const int HttpStatusInternalServerError = 500;
        private const int HttpStatusServiceUnavailable = 503;

        public HealthController(
            IHealthCheckService healthCheckService,
            IHealthCheckServerInfoService serverInfoService,
            ILogger<HealthController> logger)
        {
            _healthCheckService = healthCheckService ?? throw new ArgumentNullException(nameof(healthCheckService));
            _serverInfoService = serverInfoService ?? throw new ArgumentNullException(nameof(serverInfoService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Basic health check endpoint - returns 200 if system is operational with server info
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var serverInfo = await _serverInfoService.GetServerInfoAsync();

                // Add health status information
                var result = new Dictionary<string, object>
                {
                    ["Status"] = "Healthy",
                    ["Timestamp"] = DateTimeHelper.ServerTime,
                    ["Monitors"] = new Dictionary<string, object>(serverInfo)
                };

                return Json(result, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get basic health check information");

                // Fallback to basic info
                var basicInfo = _serverInfoService.GetBasicServerInfo();
                var fallbackResult = new Dictionary<string, object>(basicInfo)
                {
                    ["Status"] = "Healthy",
                    ["Timestamp"] = DateTimeHelper.ServerTime,
                    ["Error"] = "Unable to retrieve detailed server info"
                };

                return Json(fallbackResult, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
        }

        /// <summary>
        /// Detailed health check with all component statuses
        /// </summary>
        [HttpGet("detailed")]
        [TokenAuth]
        public async Task<ActionResult<HealthStatus>> GetDetailed()
        {
            try
            {
                var healthStatus = await _healthCheckService.GetHealthStatusAsync();

                var statusCode = healthStatus.Status switch
                {
                    "Healthy" => HttpStatusOk,
                    "Warning" => HttpStatusOk, // Still operational but with warnings
                    "Unhealthy" => HttpStatusServiceUnavailable, // Service unavailable
                    _ => HttpStatusInternalServerError
                };

                return StatusCode(statusCode, healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed");
                return StatusCode(HttpStatusInternalServerError, new HealthStatus
                {
                    Status = "Unhealthy",
                    Components = { ["system"] = ComponentHealth.Unhealthy("System", ex.Message) }
                });
            }
        }

        /// <summary>
        /// Health check for specific component
        /// </summary>
        [HttpGet("component/{componentName}")]
        [TokenAuth]
        public async Task<ActionResult<ComponentHealth>> GetComponentHealth(string componentName)
        {
            try
            {
                var component = componentName.ToLowerInvariant() switch
                {
                    "database" => await _healthCheckService.CheckDatabaseHealthAsync(),
                    "redis" => await _healthCheckService.CheckRedisHealthAsync(),
                    "rabbitmq" => await _healthCheckService.CheckRabbitMQHealthAsync(),
                    "maintenance" => await _healthCheckService.CheckUnderMaintenanceServiceAsync(),
                    _ => ComponentHealth.Unhealthy("Unknown", $"Component '{componentName}' not found")
                };

                var isWarningStatus = component.IsWarning ? HttpStatusOk : HttpStatusServiceUnavailable;
                var statusCode = component.IsHealthy ? HttpStatusOk : isWarningStatus;
                return StatusCode(statusCode, component);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Component health check failed for {Component}", componentName);
                return StatusCode(HttpStatusInternalServerError, ComponentHealth.Unhealthy(componentName, ex.Message));
            }
        }

        /// <summary>
        /// System metrics endpoint
        /// </summary>
        [HttpGet("metrics")]
        [TokenAuth]
        public async Task<ActionResult<SystemMetrics>> GetMetrics()
        {
            try
            {
                var metrics = await _healthCheckService.GetSystemMetricsAsync();
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system metrics");
                return StatusCode(HttpStatusInternalServerError, new { Error = ex.Message });
            }
        }

        /// <summary>
        /// Health check dashboard view
        /// </summary>
        [HttpGet("dashboard")]
        [TokenAuth]
        public async Task<IActionResult> Dashboard()
        {
            try
            {
                var healthStatus = await _healthCheckService.GetHealthStatusAsync();
                return View(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load health dashboard");
                ViewBag.Error = ex.Message;
                return View(new HealthStatus { Status = "Unhealthy" });
            }
        }


    }
}