using ONEHub.Shared.Services.Interfaces;

namespace ONEHub.GOTO.Middleware;

public class ErrorHandlingMiddleware(RequestDelegate next, ILogProvider logProvider)
{
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await logProvider.SentryErrorAsync(ex);
        }
    }
}
