namespace ONEHub.Shared.Services.Interfaces;

public interface ICacheKeyService
{
    string GetFullKey(string key, string? subKey1 = null, string? subKey2 = null);
}

