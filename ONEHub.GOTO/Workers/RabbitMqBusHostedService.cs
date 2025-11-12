#nullable enable
using EasyNetQ;
using ONEHub.Shared.Services.Interfaces;

namespace ONEHub.GOTO.Workers;

public class RabbitMqBusHostedService(
       ILogProvider logProvider,
       IConfiguration configuration,
       IRabbitBus rabbitBus) : BackgroundService
{

    private IBus? bus;
    private readonly SemaphoreSlim connectionLock = new(1, 1);
    private bool isConnected;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connStr = configuration.GetValue<string>("RabbitMQConnectionString");
        if (string.IsNullOrEmpty(connStr))
        {
            await logProvider.SentryErrorAsync("RabbitMQ connection string is not configured");
        }

        bus = RabbitHutch.CreateBus(connStr);
        bus.Advanced.Connected += (_, _) =>
        {
            logProvider.LogFileMessage("RabbitMQ connected.");
        };
        bus.Advanced.Disconnected += async (_, _) =>
        {
            await TryConnectToRabbitMqAsync(stoppingToken);
            logProvider.LogFileMessage("RabbitMQ disconnected.");
        };

        await TryConnectToRabbitMqAsync(stoppingToken);
    }

    public override void Dispose()
    {
        bus?.Dispose();
        base.Dispose();
    }

    private async Task TryConnectToRabbitMqAsync(CancellationToken stoppingToken)
    {
        if (!await connectionLock.WaitAsync(0))
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested && !isConnected)
        {
            try
            {
                rabbitBus.SetBus(bus!.Advanced);
                await rabbitBus.RegisterQueueAsync();
                isConnected = true;
            }
            catch (Exception ex)
            {
                isConnected = false;
                await logProvider.SentryErrorAsync("RabbitMQ connect failed. Retrying in 10s...", ex);
                logProvider.LogFileError(ex);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        connectionLock.Release();
    }
}
