using Microsoft.Extensions.Configuration;
using ONEHub.Shared.Services.Interfaces;

namespace ONEHub.Shared.Services;

public class CacheKeyService : ICacheKeyService
{
    private readonly string applicationName;

    public CacheKeyService(IConfiguration configuration)
    {
        applicationName = configuration["fanex.caching.redis:ApplicationName"] ?? "ONEHub.Dev";
    }

    public string GetFullKey(string key, string? subKey1 = null, string? subKey2 = null)
    {
        var fullKey = $"{applicationName}:{key}";
        if (!string.IsNullOrWhiteSpace(subKey1))
        {
            fullKey = $"{fullKey}_{subKey1}";
        }

        if (!string.IsNullOrWhiteSpace(subKey2))
        {
            fullKey = $"{fullKey}_{subKey2}";
        }

        return fullKey;
    }
}

