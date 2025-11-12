using Microsoft.Extensions.Configuration;
using ONEHub.Shared.Services.Interfaces;

namespace ONEHub.Shared.Services;

public class QueueKeyService : IQueueKeyService
{
    private readonly string queueIdentify;

    public string MainExchangeName { 
        get 
        { 
            return $"{queueIdentify}.exchange";
        } 
    }

    public string RetryExchangeName
    {
        get
        {
            return $"{queueIdentify}.retry.exchange";
        }
    }

    public string ErrorExchangeName
    {
        get
        {
            return $"{queueIdentify}.error.exchange";
        }
    }

    public string MainQueueName
    {
        get
        {
            return $"{queueIdentify}.queue";
        }
    }

    public string RetryQueueName
    {
        get
        {
            return $"{queueIdentify}.retry.queue";
        }
    }

    public string ErrorQueueName
    {
        get
        {
            return $"{queueIdentify}.error.queue";
        }
    }

    public string RoutingKey
    {
        get
        {
            return $"{queueIdentify}.routing";
        }
    }

    public QueueKeyService(IConfiguration configuration)
    {
        queueIdentify = configuration["AppSettings:QueueIdentify"] ?? "ONEHub";
    }

    public string GetQueueIdentify()
    {
        return queueIdentify;
    }
}

