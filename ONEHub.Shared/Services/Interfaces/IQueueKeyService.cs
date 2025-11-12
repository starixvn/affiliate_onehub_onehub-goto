namespace ONEHub.Shared.Services.Interfaces;

public interface IQueueKeyService
{
    string MainExchangeName { get; }

    string RetryExchangeName { get; }

    string ErrorExchangeName { get; }

    string MainQueueName { get; }

    string RetryQueueName { get; }

    string ErrorQueueName { get; }

    string RoutingKey { get; }

    string GetQueueIdentify();
}

