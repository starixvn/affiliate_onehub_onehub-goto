using EasyNetQ;
using Fanex.Data;
using Fanex.Data.Repository;
using Fanex.Logging;
using Fanex.Logging.Sentry.RavenClient;
using Microsoft.AspNetCore.Builder;
using ONEbook.UM.DependencyInjection;
using ONEbook.UM.Services;
using ONEHub.GOTO.Consumer;
using ONEHub.GOTO.Core.Constants;
using ONEHub.GOTO.Core.Services;
using ONEHub.GOTO.Core.Services.Interfaces;
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
RegisterWorker(builder);

var rabbitMqConnectionString = builder.Configuration.GetValue<string>("RabbitMQConnectionString");
builder.Services.AddHttpClient();
builder.Services.AddSingleton(RabbitHutch.CreateBus(rabbitMqConnectionString));
builder.Services.AddOneBookUmForService(builder.Configuration);
builder.Services.AddSingleton<IMaintenanceService, MaintenanceService>();
builder.Services.AddSingleton<IDynamicRepository, DynamicRepository>();
builder.Services.AddSingleton<IClickTrackingService, ClickTrackingService>();
RedisConnectionHelper.Initialize(builder.Configuration);
builder.Services.AddSingleton<IConnectionMultiplexer>(sp => RedisConnectionHelper.Connection);
builder.Services.AddSingleton<IRedisService, RedisService>();
builder.Services.AddSingleton<ICacheKeyService, CacheKeyService>();
builder.Services.AddSingleton<IQueueKeyService, QueueKeyService>();
builder.Services.AddSingleton<Fanex.Logging.ILogger, Fanex.Logging.Logger>();
builder.Services.AddSingleton<ILogProvider, LogProvider>();

var env = builder.Configuration["Sentry:Environment"];
var backEndDsn = builder.Configuration["Sentry:Dsn"];
var category = builder.Configuration["Sentry:LogCategory"];

LogManager.SetDefaultLogCategory(category).Use(new List<ILogEngine>
        {
            new SentryLogging(new SentryEngineOptions
            {
                Dsn = new Dsn(backEndDsn),
                Environment = env
            })
        });

DbSettingProviderManager
    .StartNewSession()
    .Register(builder.Configuration)
    .Run();

var host = builder.Build();

RouteToAction(host);

await host.RunAsync();

static void RouteToAction(WebApplication host)
{
    host.MapGet("/", () => "Affiliate.Link.Consumer running!");
    host.MapGet("/sentry", async () =>
    {
        var logService = host.Services.GetRequiredService<Fanex.Logging.ILogger>();
        await logService.ErrorAsync("Consumer Test Sentry");
        return "Consumer Test sentry!";
    });
    host.MapGet("/OnDQLQueueConsumer", async () =>
    {
        var redisService = host.Services.GetRequiredService<IRedisService>();
        var cacheKeyService = host.Services.GetRequiredService<ICacheKeyService>();
        var cachekey = cacheKeyService.GetFullKey(CacheKey.TurnOnErrorQueueConsumer);
        var result = await redisService.SetAsync(cachekey, true);
        return result;
    });

    host.MapGet("/OffDQLQueueConsumer", async () =>
    {
        var redisService = host.Services.GetRequiredService<IRedisService>();
        var cacheKeyService = host.Services.GetRequiredService<ICacheKeyService>();
        var cachekey = cacheKeyService.GetFullKey(CacheKey.TurnOnErrorQueueConsumer);
        var result = await redisService.SetAsync(cachekey, false);
        return result;
    });
}

static void RegisterWorker(WebApplicationBuilder builder)
{
    RegisterWorkerIfEnabled<ClickTrackingDLQWorker>(builder, "JobSettings:ClickTrackingDLQWorker");
    RegisterWorkerIfEnabled<ClickTrackingRedisStorageWorker>(builder, "JobSettings:ClickTrackingRedisStorageWorker");
    RegisterWorkerIfEnabled<ClickTrackingWorker>(builder, "JobSettings:ClickTrackingWorker");
}

static void RegisterWorkerIfEnabled<T>(WebApplicationBuilder builder, string configKey) where T : class, IHostedService
{
    if (bool.TryParse(builder.Configuration[configKey], out var isEnabled) && isEnabled)
    {
        builder.Services.AddHostedService<T>();
    }
}