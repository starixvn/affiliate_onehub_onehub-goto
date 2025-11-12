namespace ONEHub.Shared.Services.Interfaces;

public interface IRedisService
{
    Task<bool> RemoveAsync(string key);

    Task<bool> SetAsync<T>(string key, T? value, TimeSpan? expireTime = null);

    Task<T?> GetAsync<T>(string key);

    Task<T?> GetWithSlidingExpirationAsync<T>(string key, TimeSpan slidingExpirationTime);

    Task<long> StringIncrementAsync(string key);

    Task<bool> StringSetAsync(string key, string value);

    Task<string?> StringGetAsync(string key);

    Task<long> ListRightPushAsync<T>(string key, T? value);

    Task<T?> ListLeftPopAsync<T>(string key);
}
