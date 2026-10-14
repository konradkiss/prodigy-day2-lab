using Orders.Orders;

namespace Orders.Tests;

public class DiscountCodeTests
{
    static readonly DateOnly Today = new(2026, 10, 14);
    static readonly Discount Save15 = new("SAVE15", 15, 5000, new DateOnly(2099, 12, 31));
    static readonly Discount Welcome10 = new("WELCOME10", 10, 0, new DateOnly(2099, 12, 31));

    static Order Order(Discount discount, params (string Sku, int Qty, int UnitPrice)[] items) =>
        new(1, 1, "open", discount?.Code, items.Select(i => new OrderItem(i.Sku, i.Qty, i.UnitPrice)).ToList(), discount);

    [Fact]
    public void TakesThePercentageOffTheGoodsTotal()
    {
        var totals = Pricing.PriceOrder(Order(Save15, ("HOODIE", 2, 4990)));
        Assert.Equal(1497, totals.CodeDiscount);
        Assert.Equal(9980 - 1497, totals.Total);
    }

    [Fact]
    public void AppliesAfterTheBulkDiscount()
    {
        // 10 socks: 9900, bulk 5% = 495, goods 9405, code 10% = 940.5 -> 941
        var totals = Pricing.PriceOrder(Order(Welcome10, ("SOCKS", 10, 990)));
        Assert.Equal(495, totals.BulkDiscount);
        Assert.Equal(941, totals.CodeDiscount);
        Assert.Equal(9900 - 495 - 941, totals.Total);
    }

    [Fact]
    public void RoundsTheDiscountHalfUp()
    {
        var totals = Pricing.PriceOrder(Order(Save15, ("X", 1, 9990)));
        Assert.Equal(1499, totals.CodeDiscount);
    }

    [Fact]
    public void DecidesFreeShippingAfterTheCode()
    {
        var totals = Pricing.PriceOrder(Order(Welcome10, ("X", 1, 5400)));
        Assert.Equal(540, totals.CodeDiscount);
        Assert.Equal(Pricing.Shipping, totals.Shipping);
        Assert.Equal(5400 - 540 + Pricing.Shipping, totals.Total);
    }

    [Fact]
    public void GivesNoDiscountOnceTheGoodsFallBelowTheMinimum()
    {
        var totals = Pricing.PriceOrder(Order(Save15, ("HOODIE", 1, 4990)));
        Assert.Equal(0, totals.CodeDiscount);
    }

    [Fact]
    public void AcceptsAnOrderOfExactlyTheMinimum()
    {
        Assert.Null(Pricing.RefuseDiscount(Save15, Order(null, ("X", 1, 5000)), Today));
        Assert.Equal(750, Pricing.PriceOrder(Order(Save15, ("X", 1, 5000))).CodeDiscount);
    }

    [Fact]
    public void ChecksTheMinimumAgainstTheGoodsTotal()
    {
        // 10 × 520 = 5200, but after the 5% bulk discount the goods are 4940
        Assert.NotNull(Pricing.RefuseDiscount(Save15, Order(null, ("X", 10, 520)), Today));
    }

    [Fact]
    public void WorksOnItsLastDay()
    {
        var code = Welcome10 with { ExpiresOn = Today };
        Assert.Null(Pricing.RefuseDiscount(code, Order(null, ("MUG", 1, 1250)), Today));
    }

    [Fact]
    public void RefusesAnExpiredCode()
    {
        var code = Welcome10 with { Code = "SUMMER25", ExpiresOn = new DateOnly(2026, 8, 31) };
        var refusal = Pricing.RefuseDiscount(code, Order(null, ("MUG", 1, 1250)), Today);
        Assert.Contains("expired", refusal);
    }

    [Fact]
    public void RefusesAnUnknownCode()
    {
        Assert.NotNull(Pricing.RefuseDiscount(null, Order(null, ("MUG", 1, 1250)), Today));
    }

    [Fact]
    public void FindsCodesInAnyCase()
    {
        Assert.Equal("SAVE15", OrderRepository.FindDiscount("save15").Code);
        Assert.Equal("SAVE15", OrderRepository.FindDiscount(" Save15 ").Code);
    }
}
