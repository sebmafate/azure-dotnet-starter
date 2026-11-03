using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Orders.Api.Publishing;
using Orders.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Orders.Tests;

public class OrdersApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private HttpClient CreateClient() => factory.WithWebHostBuilder(b =>
        b.UseSetting("Orders:Publisher", "InMemory")).CreateClient();

    [Fact]
    public async Task Health_repond_ok()
    {
        var response = await CreateClient().GetAsync("/health");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task PlaceOrder_publie_un_evenement_et_renvoie_202()
    {
        var publisher = new InMemoryOrderPublisher();
        var client = factory.WithWebHostBuilder(b =>
        {
            b.UseSetting("Orders:Publisher", "InMemory");
            b.ConfigureServices(s => s.AddSingleton<IOrderPublisher>(publisher));
        }).CreateClient();

        var response = await client.PostAsJsonAsync("/orders", new
        {
            customerId = "client-1",
            lines = new[] { new { sku = "SKU-A", quantity = 2, unitPrice = 10.0 } },
        });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var message = Assert.Single(publisher.Messages);
        Assert.Equal("client-1", message.CustomerId);
        Assert.Equal(20m, message.Total);
    }

    [Fact]
    public async Task PlaceOrder_refuse_une_commande_invalide_avec_400()
    {
        var response = await CreateClient().PostAsJsonAsync("/orders", new
        {
            customerId = "client-1",
            lines = Array.Empty<object>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
