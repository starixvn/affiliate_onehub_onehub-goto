namespace ONEHub.GOTO.Models.HealthCheck;

/// <summary>
/// Configuration settings for health check system
/// </summary>
public class HealthCheckConfig
{
    public bool Enabled { get; set; } = true;
    public bool RequireAuthentication { get; set; }
    public string ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public int CacheDurationSeconds { get; set; } = 60;
    public bool EnableDetailedLogging { get; set; } = true;
    public bool EnableAlerts { get; set; }
    public HealthThresholds Thresholds { get; set; } = new();
    public Dictionary<string, bool> ComponentsEnabled { get; set; } = new()
    {
        ["database"] = true,
        ["redis"] = true,
        ["rabbitmq"] = true,
        ["route_constraints"] = true,
        ["maintenance_service"] = true
    };
}

public class HealthThresholds
{
    // Response time thresholds in milliseconds
    private const double DefaultDatabaseResponseTimeMs = 1000;
    private const double DefaultRedisResponseTimeMs = 100;
    private const double DefaultRouteConstraintResponseTimeMs = 1;
    
    // Memory and thread thresholds
    private const long DefaultMemoryUsageMB = 500;
    private const int DefaultActiveThreadsWarning = 100;
    private const int DefaultActiveThreadsCritical = 200;
    
    public double DatabaseResponseTimeMs { get; set; } = DefaultDatabaseResponseTimeMs;
    public double RedisResponseTimeMs { get; set; } = DefaultRedisResponseTimeMs;
    public double RouteConstraintResponseTimeMs { get; set; } = DefaultRouteConstraintResponseTimeMs;
    public long MemoryUsageMB { get; set; } = DefaultMemoryUsageMB;
    public int ActiveThreadsWarning { get; set; } = DefaultActiveThreadsWarning;
    public int ActiveThreadsCritical { get; set; } = DefaultActiveThreadsCritical;
}