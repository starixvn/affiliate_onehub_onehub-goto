namespace ONEHub.GOTO.Controllers;

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ONEHub.GOTO.Attributes;
using ONEHub.GOTO.Core.Models;
using ONEHub.GOTO.Core.Services.Interfaces;
using ONEHub.Shared.Services.Interfaces;
using ONEHub.Shared.Extensions;

public class HomeController(
        IRedirectLinkService redirectLinkService,
        IRedisService redisService,
        IBotDetectionService botDetectionService,
        ILogProvider logProvider) : Controller
{
    private const string LinkIsInvalidMessage = "Oops! This link does not exist. Please reach out to the administrator if you need help.";        
    // HTTP Status Codes
    private const int HttpStatusInternalServerError = 500;

    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Error() => View();

    [HttpGet]
    public IActionResult PageNotFound() => View();

    [HttpGet]
    [TokenAuth]
    public async Task<IActionResult> DeleteRedisKey(string redisKey)
    {
        try
        {
            var result = await redisService.RemoveAsync(redisKey);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await logProvider.SentryErrorAsync(ex).ConfigureAwait(false);
            return StatusCode(HttpStatusInternalServerError, ex.Message);
        }
    }

    [HttpGet]
    [TokenAuth]
    public async Task<IActionResult> CheckBot(string userAgent)
    {
        try
        {
            var isBot = botDetectionService.DetectBotFromUserAgent(userAgent);
            return Ok(isBot);
        }
        catch (Exception ex)
        {
            await logProvider.SentryErrorAsync(ex).ConfigureAwait(false);
            return StatusCode(HttpStatusInternalServerError, ex.Message);
        }
    }

    [HttpGet]
    [TokenAuth]
    public async Task<IActionResult> DetectBot(DateTime fromDate, DateTime toDate)
    {
        try
        {
            await botDetectionService.DetectBot(fromDate, toDate);
            return Ok();
        }
        catch (Exception ex)
        {
            await logProvider.SentryErrorAsync(ex).ConfigureAwait(false);
            return StatusCode(HttpStatusInternalServerError, ex.Message);
        }
    }

    [HttpGet]
    [TokenAuth]
    public async Task<IActionResult> SyncMaxClickId(bool isSync = true)
    {
        try
        {
            var result = await redirectLinkService.SyncMaxClickId(isSync);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await logProvider.SentryErrorAsync(ex).ConfigureAwait(false);
            return StatusCode(HttpStatusInternalServerError, $"An error occurred while syncing max click id: {ex.Message}");
        }
    }

    [HttpGet]
    [TokenAuth]
    public async Task<IActionResult> RefreshCacheData()
    {
        try
        {
            await redirectLinkService.SyncAllRedirectLinkToCache();
            return Ok(true);
        }
        catch (Exception ex)
        {
            await logProvider.SentryErrorAsync(ex).ConfigureAwait(false);
            return StatusCode(HttpStatusInternalServerError, $"An error occurred while Refresh Cache: {ex.Message}");
        }
    }

    public async Task<IActionResult> Goto(string slug)
    {
        var redirectLink = await redirectLinkService.GetRedirectLink(slug);
        if (redirectLink is null)
        {
            ViewBag.Result = LinkIsInvalidMessage;
            return View("Link");
        }
        else
        {
            return await ProcessRedirectLink(redirectLink);
        }
    }

    private async Task<IActionResult> ProcessRedirectLink(RedirectLink redirectLink)
    {
        var clickTrackingEvent = await GetTrackingEvent(redirectLink);
        var forwardQueryString = Request.QueryString.Value.Replace("?", "&");
        var fullLink = $"{redirectLink.FullLink}&clickId={clickTrackingEvent.ClickId}{forwardQueryString}";
        clickTrackingEvent.RedirectURL = fullLink;
        await redirectLinkService.PublishMessage(clickTrackingEvent).ConfigureAwait(false);
        return Redirect(fullLink);
    }

    private async Task<ClickTrackingEvent> GetTrackingEvent(RedirectLink redirectLink)
    {
        var isExcluded = botDetectionService.IsBot(HttpContext);
        var clickId = await redirectLinkService.GetClickId().ConfigureAwait(false);
        var clickTrackingEvent = new ClickTrackingEvent 
        {
            LinkId = redirectLink.LinkId,
            ClickId = clickId,
            ClickTypeId = isExcluded ? (byte)1 : (byte)0,
            IP = HttpContext.GetClientIpAddress(),
            UserAgent = HttpContext.GetUserAgent(),
            Browser = HttpContext.GetBrowserName(),
            BrowserLanguage = HttpContext.GetLanguage(),
            BrowserVersion = HttpContext.GetBrowserVersion(),
            Device = HttpContext.GetDeviceName(),
            DomainReferrer = HttpContext.GetDomainReferrer(),
            URLReferrer = HttpContext.GetURLReferrer()?.ToString() ?? string.Empty,
            DeviceType = HttpContext.IsMobile() ? (byte)1 : (byte)0,
            OS = HttpContext.GetOsName(),
            UpdatedTime = DateTimeOffset.UtcNow.DateTime,
            RedirectURL = redirectLink.FullLink
        };
        return clickTrackingEvent;
    }
}