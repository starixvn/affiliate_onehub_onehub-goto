using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace ONEHub.Shared.Redis;

public static class RedisConnectionHelper
{
    private static Lazy<ConnectionMultiplexer>? lazyConnection;
    private static readonly object InitLock = new();
    private const int ConnectRetryCount = 3;

    public static void Initialize(IConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        lock (InitLock)
        {
            if (lazyConnection != null && lazyConnection.IsValueCreated)
            {
                // Dispose previous connection if already created
                lazyConnection.Value.Dispose();
            }

            var connectionString = configuration["fanex.caching.redis:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Redis connection string is missing or empty.", nameof(configuration));
            }

            lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                var options = ConfigurationOptions.Parse(connectionString);

                options.AbortOnConnectFail = false;
                options.ConnectRetry = ConnectRetryCount;
                options.ReconnectRetryPolicy = new ExponentialRetry(5000);
                options.ClientName = configuration["fanex.caching.redis:ApplicationName"];
                return ConnectionMultiplexer.Connect(options);
            }, isThreadSafe: true);
        }
    }

    public static ConnectionMultiplexer Connection
    {
        get
        {
            if (lazyConnection == null)
            {
                throw new InvalidOperationException("RedisConnectionHelper is not initialized. Call Initialize() first.");
            }
            return lazyConnection.Value;
        }
    }
}
