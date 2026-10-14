using Orders.Orders;

namespace Orders.Tests;

public class PricingTests
{
    private static Order Order(params (string Sku, int Qty, int UnitPrice)[] items) =>
        new(1, 1, "open", null, [.. items.Select(i => new OrderItem(i.Sku, i.Qty, i.UnitPrice))]);

    [Fact]
    public void AddsUpTheItems()
    {
        var totals = Pricing.PriceOrder(Order(("MUG", 2, 1250)));
        Assert.Equal(2500, totals.Subtotal);
        Assert.True(totals.Total > 2500);
    }

    [Fact]
    public void GivesFreeShippingOnLargeOrders()
    {
        var totals = Pricing.PriceOrder(Order(("HOODIE", 2, 4990)));
        Assert.Equal(0, totals.Shipping);
    }

    [Fact]
    public void ChargesShippingOnSmallOrders()
    {
        var totals = Pricing.PriceOrder(Order(("CAP", 1, 1500)));
        Assert.True(totals.Shipping > 0);
    }

    [Fact]
    public void GivesABulkDiscount()
    {
        var totals = Pricing.PriceOrder(Order(("MUG", 12, 1250)));
        Assert.True(totals.BulkDiscount > 0);
    }

    [Fact]
    public void GivesADiscountOnSocks()
    {
        var totals = Pricing.PriceOrder(Order(("SOCKS", 6, 990)));
        Assert.True(totals.BulkDiscount > 0);
    }

    [Fact]
    public void PricesAnEmptyOrder()
    {
        var totals = Pricing.PriceOrder(Order());
        Assert.Equal(0, totals.Total);
    }
}
