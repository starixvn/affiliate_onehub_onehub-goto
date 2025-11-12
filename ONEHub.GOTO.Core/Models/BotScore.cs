namespace ONEHub.GOTO.Core.Models;
public class BotScore
{
    public bool IsBot { get; set; }

    public int Score { get; set; }

    public string Reason { get; set; } = string.Empty;
}
