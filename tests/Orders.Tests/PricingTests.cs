using Orders.Orders;

namespace Orders.Tests;

public class PricingTests
{
    private static Order Order(params (string Sku, int Qty, int UnitPrice)[] items) =>
        new(1, 1, "open", null, [.. items.Select(i => new OrderItem(i.Sku, i.Qty, i.UnitPrice))]);

    private static readonly DateOnly Today = new(2026, 10, 14);
    private static readonly DiscountCodeTerms Save15 = new("SAVE15", 15, 5000, new DateOnly(2099, 12, 31));

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

    // Discount codes

    [Fact]
    public void TakesThePercentageOffTheGoodsTotalAfterTheBulkDiscount()
    {
        // 10 × 990 = 9900, bulk 5% = 495, goods 9405, 15% = 1410.75 → 1411
        var totals = Pricing.PriceOrder(Order(("SOCKS", 10, 990)), Save15, Today);
        Assert.Equal(495, totals.BulkDiscount);
        Assert.Equal(1411, totals.CodeDiscount);
        Assert.Equal(9405 - 1411, totals.Total);
    }

    [Fact]
    public void RoundsTheCodeDiscountHalfUp()
    {
        // 10% of 5005 = 500.5 → 501
        var code = Save15 with { Percent = 10 };
        var totals = Pricing.PriceOrder(Order(("X", 1, 5005)), code, Today);
        Assert.Equal(501, totals.CodeDiscount);
    }

    [Fact]
    public void AppliesAtExactlyTheMinimum()
    {
        var totals = Pricing.PriceOrder(Order(("X", 1, 5000)), Save15, Today);
        Assert.Equal(750, totals.CodeDiscount);
    }

    [Fact]
    public void GivesNothingBelowTheMinimum()
    {
        var totals = Pricing.PriceOrder(Order(("X", 1, 4999)), Save15, Today);
        Assert.Equal(0, totals.CodeDiscount);
        Assert.Equal(4999 + Pricing.Shipping, totals.Total);
    }

    [Fact]
    public void ChecksTheMinimumAgainstTheGoodsTotalNotTheSubtotal()
    {
        // 6 × 990 = 5940, socks 10% = 594, goods 5346
        var code = Save15 with { MinOrderCents = 5500 };
        var totals = Pricing.PriceOrder(Order(("SOCKS", 6, 990)), code, Today);
        Assert.Equal(0, totals.CodeDiscount);
    }

    [Fact]
    public void WorksOnItsLastDay()
    {
        var code = Save15 with { ExpiresOn = Today };
        Assert.Null(Pricing.WhyCodeDoesNotApply(code, 5000, Today));
        Assert.Equal(750, Pricing.PriceOrder(Order(("X", 1, 5000)), code, Today).CodeDiscount);
    }

    [Fact]
    public void StopsTheDayAfterItsLastDay()
    {
        var code = Save15 with { ExpiresOn = Today.AddDays(-1) };
        Assert.Contains("expired", Pricing.WhyCodeDoesNotApply(code, 5000, Today), StringComparison.Ordinal);
        Assert.Equal(0, Pricing.PriceOrder(Order(("X", 1, 5000)), code, Today).CodeDiscount);
    }

    [Fact]
    public void DecidesFreeShippingAfterTheCodeDiscount()
    {
        // goods 6000, 15% = 900, 5100 still ships free; goods 5000 → 4250 does not
        Assert.Equal(0, Pricing.PriceOrder(Order(("X", 1, 6000)), Save15, Today).Shipping);
        var totals = Pricing.PriceOrder(Order(("X", 1, 5000)), Save15, Today);
        Assert.Equal(Pricing.Shipping, totals.Shipping);
        Assert.Equal(4250 + Pricing.Shipping, totals.Total);
    }
}
