using ONEHub.Shared.Commands;

namespace ONEHub.GOTO.Core.Commands;

public class UpdateClickStatisticCommand : BaseNonQueryCommand
{
    public required string ClickIDs { get; set; }

    public bool IsExcluded { get; set; }

    public override string GetSettingKey() => "OH_ClickStatisticsPatterns_Upd";

    public override bool IsValid() => !string.IsNullOrWhiteSpace(ClickIDs);
}