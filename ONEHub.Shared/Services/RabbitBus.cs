using EasyNetQ;
using EasyNetQ.Topology;
using Newtonsoft.Json;
using ONEHub.Shared.Services.Interfaces;

namespace ONEHub.Shared.Services;

public class RabbitBus(IQueueKeyService queueKeyService, 
    ILogProvider logService) : IRabbitBus
{
    private IAdvancedBus? advancedBus;
    private Exchange? exchange;

    public bool IsConnected => advancedBus?.IsConnected ?? false;

    public void SetBus(IAdvancedBus bus)
    {
        advancedBus = bus;
    }

    public async Task<bool> PublishAsync<T>(T model)
    {
        var result = false;
        if (IsConnected)
        {
            try
            {
                if (!exchange.HasValue)
                {
                    await RegisterQueueAsync();
                }

                if (exchange.HasValue)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    await advancedBus.PublishAsync(
                           exchange.Value,
                           routingKey: queueKeyService.RoutingKey,
                           mandatory: true,
                       new Message<T>(model)
                       ).ConfigureAwait(false);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    result = true;
                }
            }
            catch (Exception ex)
            {
                await logService.SentryErrorAsync(
                    $"Error publishing object: {JsonConvert.SerializeObject(model)}.", ex);

                exchange = null;
                logService.LogFileMessage($"Message queued for later publishing after error: {typeof(T).Name}");
            }
        }

        return result;
    }

    public async Task RegisterQueueAsync()
    {
        exchange = await advancedBus.ExchangeDeclareAsync(queueKeyService.MainExchangeName, ExchangeType.Direct);
        var queue = await advancedBus!.QueueDeclareAsync(queueKeyService.MainQueueName, c =>
        {
            c.AsDurable(true);
            c.AsExclusive(false);
            c.AsAutoDelete(false);
            c.WithArgument("x-queue-type", "quorum");
        });

        await advancedBus.BindAsync(exchange.Value, queue, queueKeyService.RoutingKey);
    }

    public async Task<Dictionary<string, object>> GetConnectionInfoAsync()
    {
        var info = new Dictionary<string, object>
        {
            ["IsConnected"] = IsConnected,
            ["HasAdvancedBus"] = advancedBus != null,
            ["HasExchange"] = exchange.HasValue,
            ["LastCheck"] = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        if (advancedBus != null)
        {
            try
            {
                info["ConnectionState"] = advancedBus.IsConnected ? "Connected" : "Disconnected";

                if (advancedBus.IsConnected)
                {
                    info["ConnectionActive"] = true;
                    info["ConnectionType"] = "EasyNetQ.IAdvancedBus";
                }

                var mainQueueStatus = await advancedBus.GetQueueStatsAsync(queueKeyService.MainQueueName);
                var retryQueueStatus = await advancedBus.GetQueueStatsAsync(queueKeyService.RetryQueueName);
                var errorQueueStatus = await advancedBus.GetQueueStatsAsync(queueKeyService.ErrorQueueName);
                info["mainQueueStatus.ConsumersCount"] = mainQueueStatus.ConsumersCount;
                info["mainQueueStatus.MessagesCount"] = mainQueueStatus.MessagesCount;
                info["retryQueueStatus.ConsumersCount"] = retryQueueStatus.ConsumersCount;
                info["retryQueueStatus.MessagesCount"] = retryQueueStatus.MessagesCount;
                info["errorQueueStatus.ConsumersCount"] = errorQueueStatus.ConsumersCount;
                info["errorQueueStatus.MessagesCount"] = errorQueueStatus.MessagesCount;
            }
            catch (Exception ex)
            {
                info["Error"] = ex.Message;
                await logService.SentryErrorAsync("Failed to get RabbitMQ connection info", ex);
            }
        }

        return info;
    }
}
