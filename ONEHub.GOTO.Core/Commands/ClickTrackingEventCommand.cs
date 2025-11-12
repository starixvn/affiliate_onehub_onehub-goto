using ONEHub.Shared.Commands;

namespace ONEHub.GOTO.Core.Commands;

public class ClickTrackingEventCommand : BaseNonQueryCommand
{
    public required string IP { get; set; }
    public required string Browser { get; set; }
    public required string URLReferrer { get; set; }
    public required string DomainReferrer { get; set; }
    public required string OS { get; set; }
    public required string Device { get; set; }
    public required string BrowserVersion { get; set; }
    public required string BrowserLanguage { get; set; }
    public required string UserAgent { get; set; }
    public byte DeviceType { get; set; }
    public required string RedirectURL { get; set; }
    public long ClickId { get; set; }
    public DateTime UpdatedTime { get; set; }
    public int ClickTypeId { get; set; }
    public int LinkId { get; set; }

    public override string GetSettingKey() => "OH_ClickStatisticsIns";

    public override bool IsValid() => true;
}