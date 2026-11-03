using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Orders.Domain;

namespace Orders.Api.Publishing;

/// <summary>Publie les commandes sur une file Azure Service Bus, avec l'identité managée (aucun secret).</summary>
public sealed class ServiceBusOrderPublisher(ServiceBusSender sender) : IOrderPublisher, IAsyncDisposable
{
    public async Task PublishAsync(OrderPlaced message, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var serviceBusMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            MessageId = message.OrderId.ToString(),
            Subject = nameof(OrderPlaced),
        };
        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    public ValueTask DisposeAsync() => sender.DisposeAsync();
}
