using Orders.Orders;

namespace Orders.Tests;

public class PricingTests
{
    static Order Order(params (string Sku, int Qty, int UnitPrice)[] items) =>
        new(1, 1, "open", null, items.Select(i => new OrderItem(i.Sku, i.Qty, i.UnitPrice)).ToList());

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

public class DiscountCodePricingTests
{
    static readonly DateOnly Today = new(2026, 10, 14);

    static Order Order(params (string Sku, int Qty, int UnitPrice)[] items) =>
        new(1, 1, "open", "CODE", items.Select(i => new OrderItem(i.Sku, i.Qty, i.UnitPrice)).ToList());

    static DiscountCode Code(int percent, int min = 0, string expires = "2099-12-31") =>
        new("CODE", percent, min, DateOnly.Parse(expires));

    [Fact]
    public void TakesThePercentageOffTheGoodsAfterTheBulkDiscount()
    {
        // 12 mugs: 15000, bulk 750, goods 14250; 15% of 14250 = 2137.5 → 2138
        var totals = Pricing.PriceOrder(Order(("MUG", 12, 1250)), Code(15), Today);
        Assert.Equal(750, totals.BulkDiscount);
        Assert.Equal(2138, totals.CodeDiscount);
        Assert.Equal(14250 - 2138, totals.Total);
    }

    [Fact]
    public void AppliesAtExactlyTheMinimum()
    {
        var totals = Pricing.PriceOrder(Order(("HOODIE", 1, 5000)), Code(10, min: 5000), Today);
        Assert.Equal(500, totals.CodeDiscount);
    }

    [Fact]
    public void DoesNotApplyBelowTheMinimum()
    {
        var totals = Pricing.PriceOrder(Order(("HOODIE", 1, 4999)), Code(10, min: 5000), Today);
        Assert.Equal(0, totals.CodeDiscount);
    }

    [Fact]
    public void ChecksTheMinimumAgainstTheGoodsAfterTheBulkDiscount()
    {
        // 10 socks: 9900, bulk 495, goods 9405
        var totals = Pricing.PriceOrder(Order(("SOCKS", 10, 990)), Code(10, min: 9500), Today);
        Assert.Equal(0, totals.CodeDiscount);
    }

    [Fact]
    public void WorksOnItsLastDay()
    {
        var totals = Pricing.PriceOrder(Order(("CAP", 1, 1500)), Code(10, expires: "2026-10-14"), Today);
        Assert.Equal(150, totals.CodeDiscount);
    }

    [Fact]
    public void DoesNotWorkTheDayAfter()
    {
        var totals = Pricing.PriceOrder(Order(("CAP", 1, 1500)), Code(10, expires: "2026-10-13"), Today);
        Assert.Equal(0, totals.CodeDiscount);
    }

    [Fact]
    public void DecidesFreeShippingAfterTheCode()
    {
        // goods 5500, minus 15% = 4675: below the free-shipping threshold
        var totals = Pricing.PriceOrder(Order(("HOODIE", 1, 5500)), Code(15), Today);
        Assert.Equal(Pricing.Shipping, totals.Shipping);
        Assert.Equal(5500 - 825 + Pricing.Shipping, totals.Total);
    }
}
