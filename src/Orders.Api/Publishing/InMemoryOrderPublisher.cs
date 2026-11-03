using System.Collections.Concurrent;
using Orders.Domain;

namespace Orders.Api.Publishing;

/// <summary>Publication en mémoire, pour le développement local et les tests.</summary>
public sealed class InMemoryOrderPublisher : IOrderPublisher
{
    private readonly ConcurrentQueue<OrderPlaced> _messages = new();

    public IReadOnlyCollection<OrderPlaced> Messages => _messages.ToArray();

    public Task PublishAsync(OrderPlaced message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }
}
