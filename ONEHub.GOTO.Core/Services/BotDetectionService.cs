using Fanex.Data.Repository;
using Microsoft.AspNetCore.Http;
using NetCrawlerDetect;
using ONEHub.GOTO.Core.Commands;
using ONEHub.GOTO.Core.Criteria;
using ONEHub.GOTO.Core.Models;
using ONEHub.GOTO.Core.Services.Interfaces;
using ONEHub.Shared.Extensions;
using ONEHub.Shared.Services.Interfaces;

namespace ONEHub.GOTO.Core.Services;

public sealed class BotDetectionService (
    IDynamicRepository dynamicRepository,
    ILogProvider logProvider
    ) : IBotDetectionService
{
    private const int thresholdBotScore = 60;
    private const int thresholdBotScore2 = 40;
    private static readonly string[] BOT_INDICATORS = new[]
    {
         "Twitterbot",
        "SkypeUriPreview",
        "Slackbot",
        "WhatsApp",
        "TelegramBot",
        "facebookexternalhit",
        "meta-externalagent",
        "meta-externalads",
        "Instagram",
        "Slurp",
        "YandexBot",
        "Discordbot",
        "LinkedInBot",
        "Pinterestbot",
        "Googlebot",
        "Google-InspectionTool",
        "AdsBot",
        "Mediapartners-Google",
        "bingbot",
        "AdIdxBot",
        "MSNBot",
        "ZaloPreviewBot",
        "Viberbot",
        "redditbot",
        "TiktokBot",
        "Applebot",
        "Dalvik",
        "bitlybot",
        "Telegram-Android",
        "CCBot",
        "crawler",
        "ELinks",
        "okhttp",
        "python-requests",
        "WordPress",
        "Owler",
        ".NET Framework",
        "axios",
        "Go-http-client",
        "GuzzleHttp/7",
        "Java-http-client",
        "spider",
        "scrapy",
        "node-fetch",
        "httpclient",
        "wget",
        "curl",
        "aiohttp",
        "urllib",
        "libwww",
        "feedfetcher",
        "bingpreview",
        "ahrefs",
        "semrush",
        "mj12bot",
        "lighthouse",
        "gtmetrix",
        "uptimerobot",
        "feedly",
        "duckduckbot",
        "petalbot",
        "sogou",
        "censysinspect",
        "zoominfo",
        "similarweb",
        "netcraft",
        "linkfluence",
        "bot"
    };

    private BotScore GetBotScore(HttpContext context)
    {
        var headers = context.Request.Headers;
        string userAgent = headers["User-Agent"].FirstOrDefault() ?? string.Empty;
        var botScore = CheckBotByUserAgent(userAgent);

        CheckSecurityHeaders(headers, botScore);
        CheckAcceptHeader(headers, botScore);
        CheckRefererAndOrigin(headers, botScore);
        CheckLanguageHeader(headers, botScore);

        string clientIp = context.GetClientIpAddress();
        LogBotDetectionResult(botScore, clientIp, userAgent);

        return botScore;
    }

    private static void CheckSecurityHeaders(IHeaderDictionary headers, BotScore botScore)
    {
        bool hasSecHeaders = headers.ContainsKey("Sec-CH-UA") || headers.ContainsKey("Sec-Fetch-Site");
        if (!hasSecHeaders)
        {
            botScore.Score += 25;
            botScore.Reason += "[Missing Sec-*] ";
        }
    }

    private static void CheckAcceptHeader(IHeaderDictionary headers, BotScore botScore)
    {
        string accept = headers["Accept"].FirstOrDefault() ?? "";
        if (!accept.Contains("text/html") && !accept.Contains("application/json"))
        {
            botScore.Score += 20;
            botScore.Reason += "[Weird Accept] ";
        }
    }

    private static void CheckRefererAndOrigin(IHeaderDictionary headers, BotScore botScore)
    {
        string referer = headers["Referer"].FirstOrDefault() ?? "";
        string origin = headers["Origin"].FirstOrDefault() ?? "";
        if (string.IsNullOrWhiteSpace(referer) && string.IsNullOrWhiteSpace(origin))
        {
            botScore.Score += 10;
            botScore.Reason += "[No Referer/Origin] ";
        }
    }

    private static void CheckLanguageHeader(IHeaderDictionary headers, BotScore botScore)
    {
        string lang = headers["Accept-Language"].FirstOrDefault() ?? "";
        if (string.IsNullOrWhiteSpace(lang))
        {
            botScore.Score += 10;
            botScore.Reason += "[No Language] ";
        }
    }

    private void LogBotDetectionResult(BotScore botScore, string clientIp, string userAgent)
    {
        if (botScore.Score >= thresholdBotScore)
        {
            botScore.IsBot = true;
            string level = botScore.Score >= 80 ? "HIGH" : "MEDIUM";
            logProvider.LogFileMessage($"Bot detected ({level}): Score={botScore}, Reason={botScore.Reason.Trim()}, IP={clientIp}, UA={userAgent}");
        }
    }


    public bool IsBot(HttpContext context)
    {
        var botScore = GetBotScore(context);
        if (botScore.IsBot)
        {
            return true;
        }

        string userAgent = context.Request.Headers["User-Agent"].FirstOrDefault() ?? string.Empty;
        return DetectBotByThirdParty(userAgent);
    }

    public bool DetectBotFromUserAgent(string userAgent)
    {
        var botScore = CheckBotByUserAgent(userAgent);
        if (botScore.Score >= thresholdBotScore2)
        {
            return true;
        }

        return DetectBotByThirdParty(userAgent);
    }

    private static BotScore CheckBotByUserAgent(string userAgent)
    {
        var botScore = new BotScore();

        if (string.IsNullOrWhiteSpace(userAgent))
        {
            botScore.Score += 50;
            botScore.Reason += "[Missing UA] ";
        }
        else
        {
            if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase) &&
                    userAgent.Contains("Version/2", StringComparison.OrdinalIgnoreCase))
            {
                botScore.Score += 45;
                botScore.Reason += "[Fake Safari Version/2] ";
            }
            else
            {
                if (Array.Exists(BOT_INDICATORS, b => userAgent.IndexOf(b, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    botScore.Score += 40;
                    botScore.Reason += "[Suspicious UA] ";
                }
                // else: User agent doesn't match known bot patterns, no score adjustment needed
            }
        }

        return botScore;
    }

    private static bool DetectBotByThirdParty(string userAgent)
    {
        // CrawlerDetect may throw only fatal errors, usually safe
        var crawlerDetect = new CrawlerDetect();
        if (crawlerDetect.IsCrawler(userAgent))
        {
            return true;
        }

        ////var deviceDetector = new DeviceDetector(userAgent);
        ////deviceDetector.Parse();
        ////if (deviceDetector.IsBot())
        ////{
        ////    return true;
        ////}

        return false;
    }

    public async Task DetectBot(DateTime fromDate, DateTime toDate)
    {
        var criteria = new GetClickStatisticCriteria
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        var normalClicks = new List<long>();
        var excludedClicks = new List<long>();

        var clickStatistics = await dynamicRepository.FetchAsync<ClickStatistic>(criteria).ConfigureAwait(false);
        if (clickStatistics != null)
        {
            foreach (var clickStatistic in clickStatistics)
            {
                ClassifyClick(normalClicks, excludedClicks, clickStatistic);
            }
        }

        var tasks = new List<Task>(2);

        if (normalClicks.Count > 0)
        {
            tasks.Add(UpdateClickStatisticAsync(normalClicks, isExcluded: false));
        }
        if (excludedClicks.Count > 0)
        {
            tasks.Add(UpdateClickStatisticAsync(excludedClicks, isExcluded: true));
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    private async Task UpdateClickStatisticAsync(IEnumerable<long> clickIds, bool isExcluded)
    {
        if (clickIds == null)
        {
            return;
        }

        var ids = string.Join(",", clickIds);
        if (string.IsNullOrEmpty(ids))
        {
            return;
        }

        var command = new UpdateClickStatisticCommand
        {
            ClickIDs = ids,
            IsExcluded = isExcluded,
            ErrMsg = string.Empty,
        };

        await dynamicRepository.ExecuteAsync(command).ConfigureAwait(false);
    }

    private void ClassifyClick(ICollection<long> normalClicks, ICollection<long> excludedClicks, ClickStatistic clickStatistic)
    {
        if (clickStatistic == null)
        {
            return;
        }

        var isBot = DetectBotFromUserAgent(clickStatistic.UserAgent);

        if (isBot && !clickStatistic.IsIsExcluded)
        {
            excludedClicks.Add(clickStatistic.ClickId);
        }
        else if (!isBot && clickStatistic.IsIsExcluded)
        {
            normalClicks.Add(clickStatistic.ClickId);
        }
        else
        {
            // Click state is already correct, no update needed
        }
    }
}
