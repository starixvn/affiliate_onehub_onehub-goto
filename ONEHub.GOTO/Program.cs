using Fanex.Data;
using Fanex.Data.Repository;
using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using ONEbook.UM.DependencyInjection;
using ONEbook.UM.Services;
using ONEHub.GOTO.Core.Services;
using ONEHub.GOTO.Core.Services.Interfaces;
using ONEHub.GOTO.Middleware;
using ONEHub.GOTO.Models.HealthCheck;
using ONEHub.GOTO.Services.HealthCheck;
using ONEHub.GOTO.Workers;
using ONEHub.Shared.Redis;
using ONEHub.Shared.Services;
using ONEHub.Shared.Services.Interfaces;
using Serilog;
using SharpRaven;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(context.Configuration);
});

// Configure logging
var env = builder.Configuration["Sentry:Environment"];
var backEndDsn = builder.Configuration["Sentry:Dsn"];
var category = builder.Configuration["Sentry:LogCategory"];

if (!string.IsNullOrEmpty(backEndDsn) && !string.IsNullOrEmpty(category))
{
    LogManager.SetDefaultLogCategory(category).Use(new List<ILogEngine>
    {
        new SentryLogging(new SentryEngineOptions
        {
            Dsn = new Dsn(backEndDsn),
            Environment = env
        })
    });
}

// Add services to the container
builder.Services.AddControllersWithViews();

// Add HTTP context accessor
builder.Services.AddHttpContextAccessor();

// Register custom services
builder.Services.AddScoped<IRedirectLinkService, RedirectLinkService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddSingleton<IDynamicRepository, DynamicRepository>();

// Register health check services
builder.Services.AddScoped<IHealthCheckService, HealthCheckService>();
builder.Services.AddScoped<IHealthCheckConfigurationService, HealthCheckConfigurationService>();
builder.Services.AddScoped<IHealthCheckServerInfoService, HealthCheckServerInfoService>();

// Configure health check settings
builder.Services.Configure<HealthCheckConfig>(
    builder.Configuration.GetSection("HealthCheck"));

// Configure configuration
builder.Configuration
    .SetBasePath(builder.Environment.ContentRootPath)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddScoped<IBotDetectionService, BotDetectionService>();

RedisConnectionHelper.Initialize(builder.Configuration);
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => RedisConnectionHelper.Connection);
builder.Services.AddSingleton<IRedisService, RedisService>();
builder.Services.AddSingleton<ICacheKeyService, CacheKeyService>();
builder.Services.AddSingleton<IQueueKeyService, QueueKeyService>();

builder.Services.AddHttpClient();
builder.Services.AddSingleton<Fanex.Logging.ILogger, Fanex.Logging.Logger>();
builder.Services.AddSingleton<ILogProvider, LogProvider>();

builder.Services.AddHostedService<RabbitMqBusHostedService>();
builder.Services.AddSingleton<IRabbitBus, RabbitBus>();
builder.Services.AddSingleton<IClickTrackingService, ClickTrackingService>();

builder.Services.AddOneBookUmForService(builder.Configuration);

// Configure thread pool settings
const short DefaultWorkerThreads = 300;
const short DefaultCompletionPortThreads = 300;
_ = ThreadPool.SetMinThreads(DefaultWorkerThreads, DefaultCompletionPortThreads);

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.MapControllerRoute(
    name: "goto",
    pattern: "{slug:regex(^[a-zA-Z0-9_-]+$)}",
    defaults: new { controller = "Home", action = "Goto" }
);

app.MapControllerRoute(
    name: "home",
    pattern: "",
    defaults: new { controller = "Home", action = "Index" }
);

app.MapFallback(context =>
{
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    return Task.CompletedTask;
});

// Initialize Fanex Data
DbSettingProviderManager
    .StartNewSession()
    .Register(builder.Configuration)
    .Run();

await app.RunAsync();