using ONEHub.GOTO.Helpers;

namespace ONEHub.GOTO.Models.HealthCheck
{
    /// <summary>
    /// Health status for individual system components
    /// </summary>
    public class ComponentHealth
    {
        public string Name { get; set; }
        public string Status { get; set; } = "Unknown";
        public string Description { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public DateTime LastChecked { get; set; } = DateTimeHelper.ServerTime;
        public Dictionary<string, object> Data { get; set; } = new();
        public List<string> Tags { get; set; } = new();
        public string ErrorMessage { get; set; }

        public bool IsHealthy => Status == "Healthy";
        public bool IsWarning => Status == "Warning";
        public bool IsUnhealthy => Status == "Unhealthy";

        public static ComponentHealth Healthy(string name, string description = null, TimeSpan? responseTime = null)
        {
            return new ComponentHealth
            {
                Name = name,
                Status = "Healthy",
                Description = description ?? $"{name} is operating normally",
                ResponseTime = responseTime ?? TimeSpan.Zero
            };
        }

        public static ComponentHealth Warning(string name, string description, TimeSpan? responseTime = null)
        {
            return new ComponentHealth
            {
                Name = name,
                Status = "Warning",
                Description = description,
                ResponseTime = responseTime ?? TimeSpan.Zero
            };
        }

        public static ComponentHealth Unhealthy(string name, string errorMessage, TimeSpan? responseTime = null)
        {
            return new ComponentHealth
            {
                Name = name,
                Status = "Unhealthy",
                Description = "Component is not functioning properly",
                ErrorMessage = errorMessage,
                ResponseTime = responseTime ?? TimeSpan.Zero
            };
        }
    }
}