using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Orders.Api.Publishing;
using Orders.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddSingleton(TimeProvider.System);

// Choix de l'éditeur de messages : InMemory en local, ServiceBus sur Azure.
var publisher = builder.Configuration["Orders:Publisher"] ?? "InMemory";
switch (publisher)
{
    case "ServiceBus":
        var fqdn = builder.Configuration["ServiceBus:FullyQualifiedNamespace"]
            ?? throw new InvalidOperationException("ServiceBus:FullyQualifiedNamespace est requis.");
        var queue = builder.Configuration["ServiceBus:QueueName"] ?? "orders";

        // Authentification par identité managée (DefaultAzureCredential) : aucun secret dans la configuration.
        builder.Services.AddSingleton(_ => new ServiceBusClient(fqdn, new DefaultAzureCredential()));
        builder.Services.AddSingleton<IOrderPublisher>(sp =>
            new ServiceBusOrderPublisher(sp.GetRequiredService<ServiceBusClient>().CreateSender(queue)));
        break;

    case "InMemory":
        builder.Services.AddSingleton<InMemoryOrderPublisher>();
        builder.Services.AddSingleton<IOrderPublisher>(sp => sp.GetRequiredService<InMemoryOrderPublisher>());
        break;

    default:
        throw new InvalidOperationException($"Orders:Publisher inconnu : « {publisher} ».");
}

var app = builder.Build();

app.MapHealthChecks("/health");

app.MapPost("/orders", async (CreateOrderRequest request, IOrderPublisher publisher, TimeProvider time, CancellationToken ct) =>
{
    try
    {
        var lines = request.Lines.Select(l => new OrderLine(l.Sku, l.Quantity, l.UnitPrice));
        var order = Order.Create(request.CustomerId, lines, time.GetUtcNow());

        await publisher.PublishAsync(
            new OrderPlaced(order.Id, order.CustomerId, order.Total, order.PlacedAt), ct);

        return Results.Accepted(value: new { order.Id, order.Total });
    }
    catch (OrderValidationException ex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["order"] = [ex.Message],
        });
    }
})
.WithName("PlaceOrder");

app.Run();

public record CreateOrderRequest(string CustomerId, List<OrderLineRequest> Lines);

public record OrderLineRequest(string Sku, int Quantity, decimal UnitPrice);

// Permet aux tests d'intégration de référencer le point d'entrée.
public partial class Program { }
