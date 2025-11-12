using EasyNetQ;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;
using ONEbook.UM.Services;
using ONEHub.GOTO.Core.Constants;
using ONEHub.GOTO.Core.Models;
using ONEHub.GOTO.Core.Services.Interfaces;
using ONEHub.Shared.Services.Interfaces;

namespace ONEHub.GOTO.Consumer;

public sealed class ClickTrackingDLQWorker(
    IBus bus,
    IMaintenanceService maintenanceService,
    IClickTrackingService databaseService,
    IRedisService redisService,
    IQueueKeyService queueKeyService,
    ICacheKeyService cacheKeyService,
    ILogProvider logProvider) : BackgroundService
{
    private IDisposable? consumerHandle;
    private bool isTurnOffConsumer;
    private readonly object maintenanceLock = new();
    private const int InitialDelayMilliseconds = 10;

    private Queue errorQueue;

    public void TurnOffConsumerMode()
    {
        lock (maintenanceLock)
        {
            if (isTurnOffConsumer)
            {
                return;
            }

            isTurnOffConsumer = true;
            consumerHandle?.Dispose();
            consumerHandle = null;
            logProvider.LogFileMessage("Maintenance mode enabled – message processing paused.");
        }
    }

    public void TurnOnConsumerMode()
    {
        lock (maintenanceLock)
        {
            if (!isTurnOffConsumer)
            {
                return;
            }

            isTurnOffConsumer = false;
            StartConsumer();
            logProvider.LogFileMessage("Maintenance mode disabled – message processing resumed.");
        }
    }

    public async Task CheckConsumerMode()
    {
        var isDisableConsumer = await IsTurnOffConsumer();
        if (isDisableConsumer)
        {
            TurnOffConsumerMode();
        }
        else
        {
            TurnOnConsumerMode();
        }
    }

    private bool IsCurrentlyUnderMaintenance()
    {
        var maintenanceTime = maintenanceService.GetUnderMaintenanceTime();
        return maintenanceTime.IsUnderMaintenanceTime;
    }

    private async Task<bool> IsTurnOffConsumer()
    {
        var cacheKey = cacheKeyService.GetFullKey(CacheKey.TurnOnErrorQueueConsumer);
        var isEnableConsumeFromCache = await redisService.GetAsync<bool>(cacheKey);
        return IsCurrentlyUnderMaintenance() || !isEnableConsumeFromCache;
    }

    private void StartConsumer()
    {
        consumerHandle = bus.Advanced.Consume<ClickTrackingEvent>(errorQueue, async (message, _) =>
        {
            if (IsCurrentlyUnderMaintenance())
            {
                logProvider.LogFileMessage("Maintenance mode active – message retained in queue.");
                await Task.FromResult(AckStrategies.NackWithRequeue);
                return;
            }

            try
            {
                var result = await databaseService.InsertClickInfo(message.Body);
                if (!result.IsSuccessful)
                {
                    await logProvider.SentryErrorAsync($"InsertClickInfo fail - { result.ErrorMessage }");
                    await Task.FromResult(AckStrategies.NackWithRequeue);
                }
                else
                {
                    await Task.FromResult(AckStrategies.Ack);
                }
            }
            catch (Exception ex)
            {
                await logProvider.SentryErrorAsync(ex);
                await Task.FromResult(AckStrategies.NackWithRequeue);
            }
        });
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelayMilliseconds, stoppingToken).ConfigureAwait(false);
        // Setup exchanges and queues
        var errorExchange = await bus.Advanced.ExchangeDeclareAsync(queueKeyService.ErrorExchangeName, ExchangeType.Direct);
        errorQueue = await bus.Advanced.QueueDeclareAsync(queueKeyService.ErrorQueueName, c => c
            .AsDurable(true)
            .AsExclusive(false)
            .AsAutoDelete(false)
            .WithArgument("x-queue-type", "quorum")
        );
        
        await bus.Advanced.BindAsync(errorExchange, errorQueue, queueKeyService.RoutingKey);

        StartConsumer();

        while (!stoppingToken.IsCancellationRequested)
        {
            logProvider.LogFileMessage($"Worker running at: {DateTimeOffset.Now}");

            await CheckConsumerMode();

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken).ConfigureAwait(false);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        consumerHandle?.Dispose();
        return base.StopAsync(cancellationToken);
    }
}
