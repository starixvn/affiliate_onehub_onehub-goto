namespace ONEHub.GOTO.Core.Models;

public class ClickStatistic
{
    public long ClickId { get; set; }

    public string UserAgent { get; set; } = string.Empty;

    public string IP { get; set; } = string.Empty;

    public bool IsIsExcluded { get; set; }
}
