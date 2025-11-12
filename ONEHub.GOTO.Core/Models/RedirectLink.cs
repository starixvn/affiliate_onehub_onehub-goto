namespace ONEHub.GOTO.Core.Models;

public class RedirectLink
{
    public string ShortPath { get; set; } = string.Empty;

    public string ShortDomain { get; set; } = string.Empty;

    public int LinkId { get; set; }

    public string FullLink { get; set; } = string.Empty;
}
