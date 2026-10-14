using Orders.Orders;

namespace Orders.Tests;

public class OrdersTests
{
    public OrdersTests()
    {
        Db.Seed();
    }

    // GET /orders/:id

    [Fact]
    public void ShowsAnOrderToItsOwner()
    {
        var res = OrderHandlers.GetOrder(Helpers.Request(customer: 1), 1001);
        Assert.Equal(200, res.Status);
    }

    [Fact]
    public void HidesAnOrderFromOtherCustomers()
    {
        var res = OrderHandlers.GetOrder(Helpers.Request(customer: 2), 1001);
        Assert.Equal(404, res.Status);
    }

    // POST /orders/:id/items

    [Fact]
    public void AddsAnItem()
    {
        var res = OrderHandlers.AddItemToOrder(Helpers.Request(customer: 1, body: new { sku = "CAP", qty = 1 }), 1001);
        Assert.Equal(200, res.Status);
    }

    [Fact]
    public void RefusesAnUnknownProduct()
    {
        var res = OrderHandlers.AddItemToOrder(Helpers.Request(customer: 1, body: new { sku = "NOPE", qty = 1 }), 1001);
        Assert.Equal(400, res.Status);
    }

    [Fact]
    public void RefusesAPaidOrder()
    {
        var res = OrderHandlers.AddItemToOrder(Helpers.Request(customer: 3, body: new { sku = "CAP", qty = 1 }), 1004);
        Assert.Equal(409, res.Status);
    }

    // POST /orders/:id/discount

    static Response ApplyCode(int customer, int orderId, string code) =>
        OrderHandlers.ApplyDiscountCode(Helpers.Request(customer: customer, body: new { code }), orderId);

    static int CodeDiscountOf(int orderId)
    {
        var order = OrderRepository.FindOrder(orderId);
        return Pricing.PriceOrder(order, DiscountCodes.FindDiscountCode(order.DiscountCode)).CodeDiscount;
    }

    [Fact]
    public void AppliesACodeInAnyCase()
    {
        // 1007: two hoodies, 9980 of goods
        var res = ApplyCode(1, 1007, "save15");
        Assert.Equal(200, res.Status);
        Assert.Equal("SAVE15", OrderRepository.FindOrder(1007).DiscountCode);
        Assert.Equal(1497, CodeDiscountOf(1007));
    }

    [Fact]
    public void ReplacesThePreviousCode()
    {
        ApplyCode(1, 1007, "SAVE15");
        var res = ApplyCode(1, 1007, "WELCOME10");
        Assert.Equal(200, res.Status);
        Assert.Equal("WELCOME10", OrderRepository.FindOrder(1007).DiscountCode);
        Assert.Equal(998, CodeDiscountOf(1007));
    }

    [Fact]
    public void RefusesAnExpiredCode()
    {
        var res = ApplyCode(1, 1007, "SUMMER25");
        Assert.Equal(400, res.Status);
        Assert.Null(OrderRepository.FindOrder(1007).DiscountCode);
    }

    [Fact]
    public void RefusesACodeBelowItsMinimum()
    {
        // 1002: four mugs, 5000 of goods: exactly the minimum, so it qualifies
        Assert.Equal(200, ApplyCode(1, 1002, "SAVE15").Status);
        // 1001: one hoodie, 4990 of goods
        Assert.Equal(400, ApplyCode(1, 1001, "SAVE15").Status);
    }

    [Fact]
    public void RefusesAnUnknownCode()
    {
        Assert.Equal(400, ApplyCode(1, 1007, "NOPE").Status);
    }

    [Fact]
    public void HidesOtherCustomersOrdersFromTheCode()
    {
        Assert.Equal(404, ApplyCode(2, 1007, "WELCOME10").Status);
    }

    [Fact]
    public void RefusesACodeOnAPaidOrder()
    {
        Assert.Equal(409, ApplyCode(3, 1004, "WELCOME10").Status);
    }
}
