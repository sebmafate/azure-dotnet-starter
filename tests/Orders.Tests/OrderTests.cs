using Orders.Domain;

namespace Orders.Tests;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_calcule_le_total_des_lignes()
    {
        var order = Order.Create("client-1",
        [
            new OrderLine("SKU-A", 2, 10m),
            new OrderLine("SKU-B", 1, 5.5m),
        ], Now);

        Assert.Equal(25.5m, order.Total);
        Assert.Equal("client-1", order.CustomerId);
        Assert.Equal(Now, order.PlacedAt);
    }

    [Fact]
    public void Create_refuse_une_commande_sans_ligne()
    {
        var ex = Assert.Throws<OrderValidationException>(() => Order.Create("client-1", [], Now));
        Assert.Contains("au moins une ligne", ex.Message);
    }

    [Fact]
    public void Create_refuse_un_client_vide()
    {
        Assert.Throws<OrderValidationException>(() =>
            Order.Create("  ", [new OrderLine("SKU-A", 1, 1m)], Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Create_refuse_une_quantite_non_positive(int quantity)
    {
        Assert.Throws<OrderValidationException>(() =>
            Order.Create("client-1", [new OrderLine("SKU-A", quantity, 1m)], Now));
    }

    [Fact]
    public void Create_refuse_un_prix_negatif()
    {
        Assert.Throws<OrderValidationException>(() =>
            Order.Create("client-1", [new OrderLine("SKU-A", 1, -0.01m)], Now));
    }

    [Fact]
    public void Create_accepte_un_prix_nul()
    {
        var order = Order.Create("client-1", [new OrderLine("OFFERT", 1, 0m)], Now);
        Assert.Equal(0m, order.Total);
    }
}
