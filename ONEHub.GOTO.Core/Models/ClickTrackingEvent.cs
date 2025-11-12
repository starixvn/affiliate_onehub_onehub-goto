namespace ONEHub.GOTO.Core.Models;

public class ClickTrackingEvent
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
    public byte ClickTypeId { get; set; }
    public int LinkId { get; set; }
}
