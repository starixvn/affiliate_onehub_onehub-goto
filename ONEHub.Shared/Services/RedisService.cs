using ONEHub.Shared.Redis;
using ONEHub.Shared.Services.Interfaces;
using Polly;
using Polly.Retry;
using StackExchange.Redis;

namespace ONEHub.Shared.Services;

public class RedisService: IRedisService
{
    private readonly IDatabase datatabse;
    private readonly AsyncRetryPolicy retryPolicy;
    private const int RetryCount = 3;

    public RedisService(IConnectionMultiplexer redis)
    {
        datatabse = redis.GetDatabase();
        retryPolicy = Policy
            .Handle<RedisConnectionException>()
            .Or<RedisTimeoutException>()
            .Or<RedisServerException>(ex => 
                ex.Message.Contains("READONLY"))
            .WaitAndRetryAsync(RetryCount, retryAttempt => TimeSpan.FromSeconds(1),
                (_, _, _, _) =>
                {
                    // Retry logic handled by Polly
                });
    }

    public async Task<bool> RemoveAsync(string key)
    {
        return await retryPolicy.ExecuteAsync(async () =>
            await datatabse.KeyDeleteAsync(key)
          );
    }

    public async Task<bool> SetAsync<T>(string key, T? value, TimeSpan? expireTime = null)
    {
        var redisValue = RedisTypeConverter.ToRedisValue(value);
        return await retryPolicy.ExecuteAsync(async () =>
            await datatabse.StringSetAsync(key, redisValue, expireTime));
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        return await retryPolicy.ExecuteAsync(async () =>
        {
            var redisValue = await datatabse.StringGetAsync(key);
            T? value = default;

            if (!redisValue.IsNull)
            {
                value = RedisTypeConverter.ToObject<T>(redisValue);
            }

            return value;
        });
    }

    public async Task SetWithTtlAsync(string key, string value, TimeSpan ttl)
    {
        await datatabse.StringSetAsync(key, value, ttl);
    }

    public async Task<T?> GetWithSlidingExpirationAsync<T>(string key, TimeSpan slidingExpirationTime)
    {
        return await retryPolicy.ExecuteAsync(async () =>
        {
            var redisValue = await datatabse.StringGetAsync(key);
            T? value = default;
            if (!redisValue.IsNull)
            {
                value = RedisTypeConverter.ToObject<T>(redisValue);
                await datatabse.KeyExpireAsync(key, slidingExpirationTime);
            }

            return value;
        });
    }

    public async Task<long> StringIncrementAsync(string key)
    {
        return await retryPolicy.ExecuteAsync(async () =>
        {
            return await datatabse.StringIncrementAsync(key);
        });
    }

    public async Task<bool> StringSetAsync(string key, string value)
    {
        return await retryPolicy.ExecuteAsync(async () =>
        {
            return await datatabse.StringSetAsync(key, value);
        });
    }

    public async Task<string?> StringGetAsync(string key)
    {
        return await retryPolicy.ExecuteAsync(async () =>
        {
            return await datatabse.StringGetAsync(key);
        });
    }

    public async Task<long> ListRightPushAsync<T>(string key, T? value)
    {
        var redisValue = RedisTypeConverter.ToRedisValue(value);
        return await retryPolicy.ExecuteAsync(async () =>
        {
            return await datatabse.ListRightPushAsync(key, redisValue);
        });
    }

    public async Task<T?> ListLeftPopAsync<T>(string key)
    {
        return await retryPolicy.ExecuteAsync(async () =>
        {
            var redisValue = await datatabse.ListLeftPopAsync(key);

            T? value = default;
            if (!redisValue.IsNull)
            {
                value = RedisTypeConverter.ToObject<T>(redisValue);
            }

            return value;
        });
    }
}
