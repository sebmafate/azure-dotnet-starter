# .NET 10 · Azure Starter

[Lire en français](./README.md)

## What it is for

This is a starting point for a .NET API that publishes events to Azure Service Bus.
I built it to show a simple architecture. The domain does not depend on ASP.NET or Azure,
the infrastructure is written in Bicep, and no secret is stored in the code.

A typical use case: a service receives an order, checks it, and then informs other services
without calling them directly.

## Structure

| Folder | Content |
|---|---|
| `src/Orders.Domain` | Business rules and the `IOrderPublisher` port. No external dependency. |
| `src/Orders.Api` | Minimal API: `POST /orders`, `GET /health`, and the two message publishers. |
| `tests/Orders.Tests` | Unit tests for the domain and integration tests for the API. |
| `infra/main.bicep` | Service Bus, the `orders` queue, Log Analytics, Application Insights, managed identity. |
| `.forgejo/workflows/ci.yml` | Restore, build, tests and Bicep validation. |

## How to use it

### Locally

```bash
dotnet test                          # 10 tests, no Azure needed
dotnet run --project src/Orders.Api  # listens on http://localhost:5172
```

Locally, `Orders:Publisher` is `InMemory` by default. Events stay in memory.
They are lost when the application restarts, which is enough for testing.

```bash
curl -X POST http://localhost:5172/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId":"c-1","lines":[{"sku":"A","quantity":2,"unitPrice":10}]}'
```

The response is `202 Accepted` with the order ID and the total. An invalid order returns `400`.

### On Azure

```bash
az group create -n rg-orders-demo -l francecentral
az deployment group create -g rg-orders-demo -f infra/main.bicep -p prefix=ordersdemo
```

The deployment returns three outputs: `serviceBusNamespace`, `queueName` and `identityClientId`.
Then configure the API with:

- `Orders:Publisher=ServiceBus`
- `ServiceBus:FullyQualifiedNamespace` set to the value of `serviceBusNamespace`
- the managed identity `identityClientId`, attached to the API hosting

The API authenticates with `DefaultAzureCredential`. You do not need a connection string.

### Add a business rule

The rules are in `Order.Create` (`src/Orders.Domain/Order.cs`). Add a matching test in
`tests/Orders.Tests/OrderTests.cs`, then run `dotnet test`.

## How to integrate it into an existing project

There are three options, from the lightest to the most complete:

1. **The domain only**: copy `src/Orders.Domain`. It has no dependency.
2. **The port and the publisher**: take `IOrderPublisher` and the `src/Orders.Api/Publishing` folder.
   You can keep your own API.
3. **The infrastructure**: use `infra/main.bicep` as a module in your own deployment.

```bicep
module orders 'path/to/infra/main.bicep' = {
  name: 'orders'
  params: { prefix: 'myapp' }
}
```

## Limits

- No database: the starter publishes events, it does not store them.
- No OpenAPI or Swagger. The `Microsoft.OpenApi` 2.x package has a published vulnerability
  (GHSA-v5pm-xwqc-g5wc), so I removed it. The `Orders.Api.http` file is used for manual tests.
- No explicit OpenTelemetry. Application Insights is provisioned, but the code does not use it yet.
- The deployment of the API itself (App Service or Container Apps) is not described.
