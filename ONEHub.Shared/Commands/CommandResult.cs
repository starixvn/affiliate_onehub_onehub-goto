namespace ONEHub.Shared.Commands;

public class CommandResult
{
    public bool IsSuccessful
    {
        get
        {
            return ErrorCode == 0;
        }
    }

    public int ErrorCode { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;
}
