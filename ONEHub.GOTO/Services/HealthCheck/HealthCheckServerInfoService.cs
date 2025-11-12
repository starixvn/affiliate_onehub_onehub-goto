namespace ONEHub.GOTO.Services.HealthCheck
{
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.Net;
    using System.Net.Sockets;
    using Newtonsoft.Json;
    using ONEHub.GOTO.Core.Constants;
    using ONEHub.GOTO.Helpers;
    using ONEHub.Shared.Extensions;

    /// <summary>
    /// Service implementation for retrieving server and environment information for health checks
    /// </summary>
    public class HealthCheckServerInfoService : IHealthCheckServerInfoService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHostEnvironment _env;
        private readonly ILogger<HealthCheckServerInfoService> _logger;

        public HealthCheckServerInfoService(
            IHttpContextAccessor httpContextAccessor,
            IHostEnvironment env,
            ILogger<HealthCheckServerInfoService> logger)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _env = env ?? throw new ArgumentNullException(nameof(env));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IDictionary<string, object>> GetServerInfoAsync()
        {
            try
            {
                var result = new Dictionary<string, object>
                {
                    { "Env", _env.EnvironmentName }
                };

                await AddServerIPInfo(result);
                AddExtraServerInfo(result);

                return new ReadOnlyDictionary<string, object>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve comprehensive server information");
                return GetBasicServerInfo();
            }
        }

        public IDictionary<string, object> GetBasicServerInfo()
        {
            const string ServerTimeKey = "Server Time";
            const string EnvKey = "Env";
            const string LastUpdatedKey = "Last Updated";

            try
            {
                var result = new Dictionary<string, object>
                {
                    { EnvKey, _env.EnvironmentName },
                    { ServerTimeKey, DateTimeHelper.ServerTime }
                };

                // Try to add assembly info
                try
                {
                    result.Add(LastUpdatedKey, File.GetCreationTime(typeof(HealthCheckServerInfoService).Assembly.Location).ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not retrieve assembly creation time");
                }

                return new ReadOnlyDictionary<string, object>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve basic server information");
                return new ReadOnlyDictionary<string, object>(new Dictionary<string, object>
                {
                    { EnvKey, "Unknown" },
                    { ServerTimeKey, DateTimeHelper.ServerTime }
                });
            }
        }

        /// <summary>
        /// Adds server IP information to the result dictionary
        /// </summary>
        private async Task AddServerIPInfo(IDictionary<string, object> result)
        {
            try
            {
                var hostName = Dns.GetHostName();
                result.Add("Host Name", hostName);

                var serverIps = await Dns.GetHostAddressesAsync(hostName).ConfigureAwait(false);
                for (var i = 0; i < serverIps.Length; i++)
                {
                    if (serverIps[i].AddressFamily == AddressFamily.InterNetwork)
                    {
                        result.Add($"IP{i}", serverIps[i].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to retrieve server IP information");
                result.Add("Host Name", "Unable to retrieve");
            }
        }

        /// <summary>
        /// Adds extra server information to the result dictionary
        /// </summary>
        private void AddExtraServerInfo(IDictionary<string, object> result)
        {
            const string ServerTimeKey = "Server Time";
            const string LastUpdatedKey = "Last Updated";

            try
            {
                var context = _httpContextAccessor.HttpContext;
                if (context != null)
                {
                    result.Add(ServerTimeKey, DateTimeHelper.ServerTime);
                    result.Add(LastUpdatedKey, File.GetCreationTime(typeof(HealthCheckServerInfoService).Assembly.Location).ToString(CultureInfo.InvariantCulture));
                    result.Add("Local Ip", context.Connection.LocalIpAddress?.ToString() ?? "Unknown");
                    result.Add("Local Port", context.Connection.LocalPort);
                    result.Add("Client Ip", context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
                    result.Add("Actual Client Ip", context.GetClientIpAddress());
                    result.Add("Host", context.Request.Host.ToString());
                    result.Add("Schema", context.Request.Scheme);
                    result.Add("Headers:", "---------------------------------");

                    foreach (var item in context.Request.Headers.Where(i => i.Key.Contains(HeaderKey.X, StringComparison.OrdinalIgnoreCase)
                        || i.Key.Contains(HeaderKey.CF, StringComparison.OrdinalIgnoreCase)))
                    {
                        result.Add($" - {item.Key}", JsonConvert.SerializeObject(item.Value));
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to retrieve extra server information");
                result.Add(ServerTimeKey, DateTimeHelper.ServerTime);
            }
        }
    }
}