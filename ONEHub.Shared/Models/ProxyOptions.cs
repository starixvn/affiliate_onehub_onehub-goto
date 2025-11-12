namespace ONEHub.Shared.Models;

public class ProxyOptions
{
    public bool IsUseProxy { get; set; }
    public string ProxyUrl { get; set; } = string.Empty;
    public string? Username { get; set; }
    public string? Password { get; set; }
}
