using ONEHub.GOTO.Core.Models;

namespace ONEHub.GOTO.Core.Services.Interfaces;

public interface IRedirectLinkService
{
    Task<RedirectLink> GetRedirectLink(string linkCode);

    Task<RedirectLink> GetRedirectLinkFromDatabase(string linkCode);

    Task<IEnumerable<RedirectLink>> GetAllRedirectLink();

    Task SyncAllRedirectLinkToCache();

    Task<long> GetClickId();

    Task<long> SyncMaxClickId(bool isSync);

    Task PublishMessage(ClickTrackingEvent clickTrackingEvent);
}
