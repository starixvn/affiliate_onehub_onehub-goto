namespace ONEHub.GOTO.Attributes
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Services.HealthCheck;

    /// <summary>
    /// Authorization attribute for health check endpoints
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TokenAuthAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var configService = context.HttpContext.RequestServices
                .GetService<IHealthCheckConfigurationService>();

            if (configService == null || !configService.IsAuthenticationRequired())
            {
                return; // No authentication required
            }

            // Check for API key in header or query parameter
            var apiKey = context.HttpContext.Request.Headers["X-Health-API-Key"].FirstOrDefault()
                      ?? context.HttpContext.Request.Query["apiKey"].FirstOrDefault();

            if (string.IsNullOrEmpty(apiKey) || !configService.ValidateApiKey(apiKey))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    error = "Unauthorized",
                    message = "Valid API key required for health check access",
                    timestamp = DateTimeOffset.UtcNow
                });
            }
        }
    }
}