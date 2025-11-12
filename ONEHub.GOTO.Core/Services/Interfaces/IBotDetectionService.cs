using Microsoft.AspNetCore.Http;

namespace ONEHub.GOTO.Core.Services.Interfaces;

public interface IBotDetectionService
{
    bool IsBot(HttpContext context);

    bool DetectBotFromUserAgent(string userAgent);

    Task DetectBot(DateTime fromDate, DateTime toDate);
}
