using Fanex.Data.Repository;

namespace ONEHub.Shared.Commands;

public abstract class BaseNonQueryCommand : NonQueryCommand
{
    public int ErrCode { get; set; }

    public string ErrMsg { get; set; } = string.Empty;
}