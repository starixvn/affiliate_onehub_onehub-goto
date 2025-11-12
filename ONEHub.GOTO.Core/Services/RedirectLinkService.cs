using Fanex.Data.Repository;
using ONEHub.GOTO.Core.Constants;
using ONEHub.GOTO.Core.Criteria;
using ONEHub.GOTO.Core.Models;
using ONEHub.GOTO.Core.Services.Interfaces;
using ONEHub.Shared.Services.Interfaces;

namespace ONEHub.GOTO.Core.Services;

public class RedirectLinkService(IDynamicRepository dynamicRepository, 
    IRedisService redisService,
    IRabbitBus rabbitBus,
    ILogProvider logProvider,
    ICacheKeyService cacheKeyService,
    IClickTrackingService clickTrackingService) : IRedirectLinkService
{
    private const int cacheDurationInSeconds = 86400;

    public async Task<RedirectLink> GetRedirectLink(string linkCode)
    {
        var cacheKey = cacheKeyService.GetFullKey(CacheKey.REDIRECTLINK, linkCode);
        var redirectLink = await redisService.GetAsync<RedirectLink>(cacheKey);
        if (redirectLink is null)
        {
            redirectLink = await GetRedirectLinkFromDatabase(linkCode);
            await redisService.SetAsync(cacheKey, redirectLink);
        }
        return redirectLink;
    }

    public async Task<RedirectLink> GetRedirectLinkFromDatabase(string linkCode)
    {
        var criteria = new GetRedirectInfoByShortLinkCriteria
        {
            LinkCode = linkCode
        };
        var redirectLink = await dynamicRepository.GetAsync<RedirectLink>(criteria);
        return redirectLink;
    }

    public async Task<IEnumerable<RedirectLink>> GetAllRedirectLink()
    {
        var criteria = new GetAllRedirectLinkCriteria();
        var redirectLinks = await dynamicRepository.FetchAsync<RedirectLink>(criteria);
        return redirectLinks;
    }

    public async Task SyncAllRedirectLinkToCache()
    {
        var cacheKeyAll = cacheKeyService.GetFullKey(CacheKey.ALLREDIRECTLINK);
        var redirectLinks = await GetAllRedirectLink();
        var oldRedirectLinkIds = await redisService.GetAsync<List<int>>(cacheKeyAll);
        var redirectLinkIds = redirectLinks.Select(x => x.LinkId).ToList();
        var slidingExpirationTime = TimeSpan.FromSeconds(cacheDurationInSeconds);
        await redisService.SetAsync(cacheKeyAll, redirectLinkIds, slidingExpirationTime);
        var newRedirectLinks = redirectLinks
            .Where(x => oldRedirectLinkIds == null || !oldRedirectLinkIds.Contains(x.LinkId))
            .ToList();

        foreach (var redirectLink in newRedirectLinks)
        {
            var cacheKey = cacheKeyService.GetFullKey(CacheKey.REDIRECTLINK, redirectLink.ShortLink);
            await redisService.SetAsync(cacheKey, redirectLink);
        }
        
    }

    public async Task<long> GetClickId()
    {
        var clickIdFromCache = await redisService.StringGetAsync(CacheKey.CLICKID);
        if (clickIdFromCache is null)
        {
            await SyncMaxClickId(true);
        }

        var result = await redisService.StringIncrementAsync(CacheKey.CLICKID);
        return result;
    }

    public async Task<long> SyncMaxClickId(bool isSync)
    {
        const int ClickIdIncrement = 100;

        var clickIdFromDatabase = await GetMaxClickIdFromDatabase();
        clickIdFromDatabase += ClickIdIncrement;
        if (isSync)
        {
            await redisService.StringSetAsync(CacheKey.CLICKID, clickIdFromDatabase.ToString());
        }

        return clickIdFromDatabase;
    }

    public async Task PublishMessage(ClickTrackingEvent clickTrackingEvent)
    {
        var isSuceess = await rabbitBus.PublishAsync(clickTrackingEvent);
        if (!isSuceess)
        {
            try
            {
                await redisService.ListRightPushAsync(CacheKey.CLICKQUEUES, clickTrackingEvent);
            }
            catch (Exception ex)
            {
                await logProvider.SentryErrorAsync(ex);
                await clickTrackingService.InsertClickInfo(clickTrackingEvent);
            }
        }
    }

    private async Task<long> GetMaxClickIdFromDatabase()
    {
        try
        {
            var criteria = new GetMaxClickIdCriteria();
            var clickId = await dynamicRepository.GetAsync<long>(criteria);
            return clickId;
        }
        catch (Exception ex) 
        {
            await logProvider.SentryErrorAsync(ex);
            return 0;
        }
    }
}
