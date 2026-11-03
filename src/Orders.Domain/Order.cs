namespace Orders.Domain;

/// <summary>Ligne de commande : un article, une quantité et un prix unitaire.</summary>
public sealed record OrderLine(string Sku, int Quantity, decimal UnitPrice)
{
    public decimal Total => Quantity * UnitPrice;
}

/// <summary>Commande client. Construite uniquement via <see cref="Create"/>, qui valide les règles métier.</summary>
public sealed class Order
{
    private readonly List<OrderLine> _lines;

    private Order(Guid id, string customerId, IEnumerable<OrderLine> lines, DateTimeOffset placedAt)
    {
        Id = id;
        CustomerId = customerId;
        _lines = lines.ToList();
        PlacedAt = placedAt;
    }

    public Guid Id { get; }
    public string CustomerId { get; }
    public DateTimeOffset PlacedAt { get; }
    public IReadOnlyList<OrderLine> Lines => _lines;
    public decimal Total => _lines.Sum(l => l.Total);

    public static Order Create(string customerId, IEnumerable<OrderLine> lines, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(lines);

        if (string.IsNullOrWhiteSpace(customerId))
            throw new OrderValidationException("Le client est obligatoire.");

        var list = lines.ToList();
        if (list.Count == 0)
            throw new OrderValidationException("La commande doit contenir au moins une ligne.");

        foreach (var line in list)
        {
            if (string.IsNullOrWhiteSpace(line.Sku))
                throw new OrderValidationException("Chaque ligne doit avoir une référence.");
            if (line.Quantity <= 0)
                throw new OrderValidationException($"La quantité de « {line.Sku} » doit être positive.");
            if (line.UnitPrice < 0)
                throw new OrderValidationException($"Le prix de « {line.Sku} » ne peut pas être négatif.");
        }

        return new Order(Guid.NewGuid(), customerId.Trim(), list, now);
    }
}

/// <summary>Erreur de validation métier, à présenter à l'appelant (HTTP 400).</summary>
public sealed class OrderValidationException(string message) : Exception(message);

/// <summary>Événement publié après l'enregistrement d'une commande.</summary>
public sealed record OrderPlaced(Guid OrderId, string CustomerId, decimal Total, DateTimeOffset PlacedAt);

/// <summary>Port de publication : l'implémentation Azure Service Bus vit dans l'API, le domaine n'en dépend pas.</summary>
public interface IOrderPublisher
{
    Task PublishAsync(OrderPlaced message, CancellationToken cancellationToken);
}
