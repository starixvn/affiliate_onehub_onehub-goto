using ONEbook.UM.Services;
using ONEHub.GOTO.Core.Constants;
using ONEHub.GOTO.Core.Models;
using ONEHub.GOTO.Core.Services.Interfaces;
using ONEHub.Shared.Services.Interfaces;

namespace ONEHub.GOTO.Consumer;

public sealed class ClickTrackingRedisStorageWorker(
    IMaintenanceService maintenanceService,
    IClickTrackingService databaseService,
    IRedisService redisService,
    ICacheKeyService cacheKeyService,
    ILogProvider logProvider) : BackgroundService
{
    private const int JobDelayMinute = 1;
    private const int InitialDelayMilliseconds = 10;
    private bool isRunning;

    private bool IsCurrentlyUnderMaintenance()
    {
        var maintenanceTime = maintenanceService.GetUnderMaintenanceTime();
        return maintenanceTime.IsUnderMaintenanceTime;
    }

    private async Task StartConsumer()
    {
        if (isRunning || IsCurrentlyUnderMaintenance())
        {
            return;
        }

        isRunning = true;
        try
        {
            while (true)
            {
                var cacheKey = cacheKeyService.GetFullKey(CacheKey.CLICKQUEUES);
                var clickTrackingEvent = await redisService.ListLeftPopAsync<ClickTrackingEvent>(cacheKey).ConfigureAwait(false);
                if (clickTrackingEvent == null)
                {
                    break;
                }

                var result = await databaseService.InsertClickInfo(clickTrackingEvent).ConfigureAwait(false);
                if (!result.IsSuccessful)
                {
                    await logProvider.SentryErrorAsync("InsertClickInfo failed", new Exception(result.ErrorMessage)).ConfigureAwait(false);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            await logProvider.SentryErrorAsync(ex).ConfigureAwait(false);
        }
        finally
        {
            isRunning = false;
        }
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(InitialDelayMilliseconds, stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            logProvider.LogFileMessage($"ClickTrackingRedisStorageWorker running at: {DateTimeOffset.Now}");

            await StartConsumer();

            await Task.Delay(TimeSpan.FromMinutes(JobDelayMinute), stoppingToken).ConfigureAwait(false);
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        return base.StopAsync(cancellationToken);
    }
}
