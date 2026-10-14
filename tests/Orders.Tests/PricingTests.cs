using Orders.Orders;

namespace Orders.Tests;

public class PricingTests
{
    static Order Order(params (string Sku, int Qty, int UnitPrice)[] items) =>
        new(1, 1, "open", null, items.Select(i => new OrderItem(i.Sku, i.Qty, i.UnitPrice)).ToList());

    [Fact]
    public void AddsUpTheItemsAndChargesShipping()
    {
        var totals = Pricing.PriceOrder(Order(("MUG", 2, 1250)));
        Assert.Equal(new Totals(2500, 0, Pricing.Shipping, 2500 + Pricing.Shipping), totals);
    }

    [Fact]
    public void GivesFreeShippingAtExactlyTheThreshold()
    {
        var totals = Pricing.PriceOrder(Order(("MUG", 4, 1250)));
        Assert.Equal(Pricing.FreeShippingFrom, totals.Subtotal);
        Assert.Equal(0, totals.Shipping);
        Assert.Equal(5000, totals.Total);
    }

    [Fact]
    public void ChargesShippingOneCentBelowTheThreshold()
    {
        var totals = Pricing.PriceOrder(Order(("X", 1, 4999)));
        Assert.Equal(Pricing.Shipping, totals.Shipping);
        Assert.Equal(4999 + Pricing.Shipping, totals.Total);
    }

    [Fact]
    public void GivesFivePercentOffALineOfTenOrMore()
    {
        var totals = Pricing.PriceOrder(Order(("MUG", 12, 1250)));
        Assert.Equal(750, totals.BulkDiscount);
        Assert.Equal(15000 - 750, totals.Total);
    }

    [Fact]
    public void GivesTenPercentOffFiveOrMoreSocks()
    {
        var totals = Pricing.PriceOrder(Order(("SOCKS", 6, 990)));
        Assert.Equal(594, totals.BulkDiscount);
        Assert.Equal(5940 - 594, totals.Total);
    }

    [Fact]
    public void DecidesFreeShippingAfterTheBulkDiscount()
    {
        var totals = Pricing.PriceOrder(Order(("SOCKS", 5, 1000)));
        Assert.Equal(500, totals.BulkDiscount);
        Assert.Equal(Pricing.Shipping, totals.Shipping);
    }

    [Fact]
    public void PricesAnEmptyOrderAtZeroWithoutShipping()
    {
        Assert.Equal(new Totals(0, 0, 0, 0), Pricing.PriceOrder(Order()));
    }

    // discount codes

    static DiscountCode Code(int percent, int minOrderCents = 0) => new("CODE", percent, minOrderCents, new DateOnly(2099, 12, 31));

    [Fact]
    public void TakesTheCodesPercentageOffTheGoodsAfterTheBulkDiscount()
    {
        var totals = Pricing.PriceOrder(Order(("MUG", 12, 1250)), Code(10));
        Assert.Equal(750, totals.BulkDiscount);
        Assert.Equal(1425, totals.CodeDiscount);
        Assert.Equal(15000 - 750 - 1425, totals.Total);
    }

    [Fact]
    public void RoundsTheCodeDiscountHalfUpToTheCent()
    {
        var totals = Pricing.PriceOrder(Order(("X", 1, 1010)), Code(15));
        Assert.Equal(152, totals.CodeDiscount);
        Assert.Equal(1010 - 152 + Pricing.Shipping, totals.Total);
    }

    [Fact]
    public void DecidesFreeShippingAfterTheCodeDiscount()
    {
        var totals = Pricing.PriceOrder(Order(("MUG", 4, 1250)), Code(15, 5000));
        Assert.Equal(750, totals.CodeDiscount);
        Assert.Equal(Pricing.Shipping, totals.Shipping);
        Assert.Equal(4250 + Pricing.Shipping, totals.Total);
    }

    [Fact]
    public void GivesNoCodeDiscountOnceTheGoodsFallBelowTheMinimum()
    {
        var totals = Pricing.PriceOrder(Order(("MUG", 3, 1250)), Code(15, 5000));
        Assert.Equal(0, totals.CodeDiscount);
        Assert.Equal(3750 + Pricing.Shipping, totals.Total);
    }
}
