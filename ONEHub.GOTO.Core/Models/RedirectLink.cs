namespace ONEHub.GOTO.Core.Models;

public class RedirectLink
{
    public string LinkCode { get; set; }

    public string ShortLink { get; set; } = string.Empty;

    public int LinkId { get; set; }

    public string FullLink { get; set; } = string.Empty;
}
