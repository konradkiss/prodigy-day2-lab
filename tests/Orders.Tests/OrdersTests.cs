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
}
