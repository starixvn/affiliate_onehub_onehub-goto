namespace ONEHub.GOTO.Services.HealthCheck
{
    using System.Diagnostics;
    using Fanex.Data.Repository;
    using Models.HealthCheck;
    using ONEbook.UM.Services;
    using ONEHub.GOTO.Core.Constants;
    using ONEHub.GOTO.Core.Criteria;
    using ONEHub.GOTO.Helpers;
    using ONEHub.Shared.Services.Interfaces;

    /// <summary>
    /// Comprehensive health check service implementation
    /// </summary>
    public class HealthCheckService : IHealthCheckService
    {
        private readonly IDynamicRepository _dynamicRepository;
        private readonly IRedisService _redisService;
        private readonly IRabbitBus _rabbitBus;
        private readonly IMaintenanceService _maintenanceService;
        private readonly IHealthCheckConfigurationService _configService;
        private readonly ILogger<HealthCheckService> _logger;
        private readonly DateTime _startTime;

        public HealthCheckService(
            IDynamicRepository dynamicRepository,
            IRedisService redisService,
            IRabbitBus rabbitBus,
            IMaintenanceService maintenanceService,
            IHealthCheckConfigurationService configService,
            ILogger<HealthCheckService> logger)
        {
            _dynamicRepository = dynamicRepository ?? throw new ArgumentNullException(nameof(dynamicRepository));
            _redisService = redisService ?? throw new ArgumentNullException(nameof(redisService));
            _rabbitBus = rabbitBus ?? throw new ArgumentNullException(nameof(rabbitBus));
            _maintenanceService = maintenanceService ?? throw new ArgumentNullException(nameof(maintenanceService));
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _startTime = DateTimeHelper.ServerTime;
        }

        public async Task<HealthStatus> GetHealthStatusAsync()
        {
            var stopwatch = Stopwatch.StartNew();
            var healthStatus = new HealthStatus();

            try
            {
                // Run all health checks in parallel for better performance
                var healthTasks = new Dictionary<string, Task<ComponentHealth>>
                {
                    ["database"] = CheckDatabaseHealthAsync(),
                    ["redis"] = CheckRedisHealthAsync(),
                    ["rabbitmq"] = CheckRabbitMQHealthAsync(),
                    ["maintenance_service"] = CheckUnderMaintenanceServiceAsync()
                };

                await Task.WhenAll(healthTasks.Values);

                // Collect results
                foreach (var task in healthTasks)
                {
                    healthStatus.Components[task.Key] = await task.Value;
                }

                // Get system metrics
                healthStatus.Metrics = await GetSystemMetricsAsync();

                // Determine overall status
                healthStatus.Status = DetermineOverallStatus(healthStatus.Components.Values);
                healthStatus.TotalDuration = stopwatch.Elapsed;

                _logger.LogInformation("Health check completed in {Duration}ms with status {Status}",
                    stopwatch.ElapsedMilliseconds, healthStatus.Status);

                return healthStatus;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to perform health check");
                healthStatus.Status = "Unhealthy";
                healthStatus.TotalDuration = stopwatch.Elapsed;
                return healthStatus;
            }
        }

        public async Task<ComponentHealth> CheckDatabaseHealthAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            // Test connect Database by the dynamic repository (external API service)
            try
            {
                var criteria = new GetMaxClickIdCriteria();
                var result = await _dynamicRepository.GetAsync<long>(criteria);

                stopwatch.Stop();

                if (result != 0)
                {
                    return ComponentHealth.Healthy($"Database  - ClickId - {result}",
                        "Database connection via DynamicRepository successful", stopwatch.Elapsed);
                }

                return ComponentHealth.Warning("Database",
                    "Database connected but returned unexpected result", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database health check failed");
                return ComponentHealth.Unhealthy("Database", ex.Message, stopwatch.Elapsed);
            }
        }

        public async Task<ComponentHealth> CheckRedisHealthAsync()
        {
            const string RedisComponentName = "Redis";

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var testKey = $"health_check_{Guid.NewGuid()}";
                var testValue = $"test_{DateTimeHelper.ServerTime:yyyyMMddHHmmss}";
                var clickIdFromCache = await _redisService.StringGetAsync(CacheKey.CLICKID);

                await _redisService.SetAsync(testKey, testValue, TimeSpan.FromSeconds(30));
                var retrievedValue = await _redisService.GetAsync<string>(testKey);

                stopwatch.Stop();

                if (retrievedValue == testValue)
                {
                    // Clean up test key
                    try
                    {
                        await _redisService.RemoveAsync(testKey);
                    }
                    catch
                    {
                        /* Ignore cleanup errors */
                    }

                    return ComponentHealth.Healthy($"Redis  - ClickId - {clickIdFromCache}",
                        $"Redis cache responding correctly (response time: {stopwatch.ElapsedMilliseconds}ms)", stopwatch.Elapsed);
                }

                return ComponentHealth.Warning(RedisComponentName,
                    $"Redis cache test failed - expected '{testValue}', got '{retrievedValue}'", stopwatch.Elapsed);
            }
            catch (TimeoutException)
            {
                stopwatch.Stop();
                return ComponentHealth.Warning(RedisComponentName,
                    "Redis connection timeout (may be overloaded)", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogDebug(ex, "Redis health check failed");
                return ComponentHealth.Warning(RedisComponentName,
                    $"Redis connection issue: {ex.Message}", stopwatch.Elapsed);
            }
        }

        public async Task<ComponentHealth> CheckRabbitMQHealthAsync()
        {
            const string RabbitMQComponentName = "RabbitMQ";
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (!_rabbitBus.IsConnected)
                {
                    stopwatch.Stop();
                    return ComponentHealth.Unhealthy(RabbitMQComponentName,
                        "RabbitMQ connection is not active", stopwatch.Elapsed);
                }

                var connectionInfo = await _rabbitBus.GetConnectionInfoAsync();
                stopwatch.Stop();

                var description = $"RabbitMQ connection is active and responsive (response time: {stopwatch.ElapsedMilliseconds}ms)";

                var healthResult = ComponentHealth.Healthy(RabbitMQComponentName, description, stopwatch.Elapsed);
                healthResult.Data = connectionInfo;
                return healthResult;
            }
            catch (TimeoutException)
            {
                stopwatch.Stop();
                return ComponentHealth.Warning(RabbitMQComponentName,
                    "RabbitMQ health check timeout", stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogDebug(ex, "RabbitMQ health check failed");
                return ComponentHealth.Warning(RabbitMQComponentName,
                    $"RabbitMQ connection issue: {ex.Message}", stopwatch.Elapsed);
            }
        }

        public Task<ComponentHealth> CheckUnderMaintenanceServiceAsync()
        {
            const string MaintenanceServiceName = "Maintenance Service";
            const string WarningStatus = "Warning";
            const string HealthyStatus = "Healthy";

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var maintenanceTime = _maintenanceService.GetUnderMaintenanceTime();
                stopwatch.Stop();

                var status = maintenanceTime.IsUnderMaintenanceTime ? WarningStatus : HealthyStatus;
                var description = maintenanceTime.IsUnderMaintenanceTime
                    ? "System is currently under maintenance"
                    : "System is operational";

                return Task.FromResult(new ComponentHealth
                {
                    Name = MaintenanceServiceName,
                    Status = status,
                    Description = description,
                    ResponseTime = stopwatch.Elapsed,
                    Data = new Dictionary<string, object>
                    {
                        ["isUnderMaintenance"] = maintenanceTime.IsUnderMaintenanceTime,
                        ["maintenanceWindow"] = maintenanceTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Under maintenance service health check failed");
                return Task.FromResult(ComponentHealth.Unhealthy(MaintenanceServiceName, ex.Message, stopwatch.Elapsed));
            }
        }

        public Task<SystemMetrics> GetSystemMetricsAsync()
        {
            try
            {
                var process = Process.GetCurrentProcess();
                var assembly = typeof(HealthCheckService).Assembly;

                return Task.FromResult(new SystemMetrics
                {
                    MemoryUsedBytes = process.WorkingSet64,
                    ActiveThreads = process.Threads.Count,
                    Uptime = DateTimeHelper.ServerTime - _startTime,
                    Version = assembly.GetName().Version?.ToString() ?? "Unknown",
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                    AdditionalMetrics = new Dictionary<string, object>
                    {
                        ["ProcessId"] = process.Id,
                        ["MachineName"] = Environment.MachineName,
                        ["ProcessorCount"] = Environment.ProcessorCount,
                        ["WorkingSet"] = process.WorkingSet64,
                        ["VirtualMemorySize"] = process.VirtualMemorySize64,
                        ["GCTotalMemory"] = GC.GetTotalMemory(false)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to collect system metrics");
                return Task.FromResult(new SystemMetrics());
            }
        }

        private static string DetermineOverallStatus(IEnumerable<ComponentHealth> components)
        {
            const string UnhealthyStatus = "Unhealthy";
            const string WarningStatus = "Warning";
            const string HealthyStatus = "Healthy";
            const string UnknownStatus = "Unknown";

            var componentsList = components.ToList();

            if (componentsList.Exists(c => c.IsUnhealthy))
            {
                return UnhealthyStatus;
            }

            if (componentsList.Exists(c => c.IsWarning))
            {
                return WarningStatus;
            }

            return componentsList.TrueForAll(c => c.IsHealthy) ? HealthyStatus : UnknownStatus;
        }

        private async Task CheckForAlertsAsync(HealthStatus healthStatus)
        {
            // Memory conversion constants
            const int BytesToKB = 1024;
            const int KBToMB = 1024;
            const string CriticalSeverity = "Critical";
            const string WarningSeverity = "Warning";

            try
            {
                if (!_configService.GetConfiguration().EnableAlerts)
                {
                    return;
                }

                var thresholds = _configService.GetThresholds();

                // Check for unhealthy components
                foreach (var component in healthStatus.Components.Where(c => c.Value.IsUnhealthy))
                {
                    await _configService.SendAlertAsync(
                        component.Key,
                        $"Component is unhealthy: {component.Value.ErrorMessage}",
                        CriticalSeverity);
                }

                // Check for performance thresholds
                if (healthStatus.Metrics.MemoryUsedBytes / BytesToKB / KBToMB > thresholds.MemoryUsageMB)
                {
                    await _configService.SendAlertAsync(
                        "system",
                        $"High memory usage: {healthStatus.Metrics.MemoryUsedBytes / BytesToKB / KBToMB}MB",
                        WarningSeverity);
                }

                if (healthStatus.Metrics.ActiveThreads > thresholds.ActiveThreadsCritical)
                {
                    await _configService.SendAlertAsync(
                        "system",
                        $"Critical thread count: {healthStatus.Metrics.ActiveThreads}",
                        CriticalSeverity);
                }
                else if (healthStatus.Metrics.ActiveThreads > thresholds.ActiveThreadsWarning)
                {
                    await _configService.SendAlertAsync(
                        "system",
                        $"High thread count: {healthStatus.Metrics.ActiveThreads}",
                        WarningSeverity);
                }
                else
                {
                    // Thread count is within normal range - no action needed
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check for alerts");
            }
        }
    }
}