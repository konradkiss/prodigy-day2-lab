using Microsoft.AspNetCore.Http;
using Orders.Customers;
using Orders.Orders;

namespace Orders.Tests;

public class OrdersTests
{
    private const string Injection = "x' OR '1'='1";

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

    [Fact]
    public void AsksToSignInBeforeShowingAnOrder()
    {
        var res = OrderHandlers.GetOrder(Helpers.Request(), 1001);
        Assert.Equal(401, res.Status);
    }

    [Fact]
    public void ReportsAMissingOrder()
    {
        var res = OrderHandlers.GetOrder(Helpers.Request(customer: 1), 9999);
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

    // GET /warehouse/orders

    [Fact]
    public void RefusesTheFormerlyHardCodedStaffToken()
    {
        var headers = new HeaderDictionary { ["X-Staff-Token"] = "whs_live_4f9a1c22e7b84d0f9a3e51c0" };
        var res = OrderHandlers.ListOrdersForWarehouse(new Request("GET", "/", headers, default), "open");
        Assert.Equal(403, res.Status);
    }

    // Repositories: user input is a value, never SQL

    [Fact]
    public void FindsOrdersByStatus()
    {
        Assert.Equal([1004], OrderRepository.FindOrdersByStatus("paid").Select(o => o.Id));
    }

    [Fact]
    public void TreatsTheStatusAsAValueNotAsSql()
    {
        Assert.Empty(OrderRepository.FindOrdersByStatus(Injection));
    }

    [Fact]
    public void FindsACustomerByEmail()
    {
        Assert.Equal(2, CustomerRepository.FindCustomerByEmail("bob@example.com")?.Id);
    }

    [Fact]
    public void TreatsTheEmailAsAValueNotAsSql()
    {
        Assert.Null(CustomerRepository.FindCustomerByEmail(Injection));
    }
}
