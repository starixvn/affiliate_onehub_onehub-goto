namespace ONEHub.GOTO.Core.Models;

public class ClickStatistic
{
    public long ClickId { get; set; }

    public string UserAgent { get; set; }

    public string IP { get; set; }

    public bool IsIsExcluded { get; set; }
}
