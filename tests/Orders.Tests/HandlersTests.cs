using System.Text.Json;
using Orders.Orders;
using static Orders.Tests.Helpers;

namespace Orders.Tests;

[Collection(nameof(Database))]
public class HandlersTests
{
    public HandlersTests()
    {
        Db.Seed();
    }

    static int Total(Response res) => Body(res).GetProperty("total").GetInt32();

    // GET /orders/:id

    [Fact]
    public void ShowsAnOrderToItsOwnerWithItsPrices()
    {
        var res = OrderHandlers.GetOrder(Request(customer: 1), 1002);
        Assert.Equal(200, res.Status);
        var body = Body(res);
        Assert.Equal(1002, body.GetProperty("id").GetInt32());
        Assert.Equal("open", body.GetProperty("status").GetString());
        Assert.Equal(5000, body.GetProperty("subtotal").GetInt32());
        Assert.Equal(0, body.GetProperty("shipping").GetInt32());
        Assert.Equal(5000, body.GetProperty("total").GetInt32());
    }

    [Fact]
    public void AnswersNotFoundToAnotherCustomer()
    {
        Assert.Equal(404, OrderHandlers.GetOrder(Request(customer: 2), 1002).Status);
    }

    [Fact]
    public void AnswersUnauthorizedWithoutASignedInCustomer()
    {
        Assert.Equal(401, OrderHandlers.GetOrder(Request(), 1002).Status);
    }

    [Fact]
    public void AnswersNotFoundForAMissingOrder()
    {
        Assert.Equal(404, OrderHandlers.GetOrder(Request(customer: 1), 9999).Status);
    }

    // POST /orders/:id/items

    [Fact]
    public void AddsAnItemAndRepricesTheOrder()
    {
        var res = OrderHandlers.AddItemToOrder(Request(customer: 1, body: new { sku = "CAP", qty = 1 }), 1001);
        Assert.Equal(200, res.Status);
        var body = Body(res);
        Assert.Equal(6490, body.GetProperty("subtotal").GetInt32());
        Assert.Equal(0, body.GetProperty("shipping").GetInt32());
        Assert.Equal(6490, body.GetProperty("total").GetInt32());
    }

    [Fact]
    public void RefusesAnUnknownProduct()
    {
        Assert.Equal(400, OrderHandlers.AddItemToOrder(Request(customer: 1, body: new { sku = "NOPE", qty = 1 }), 1001).Status);
    }

    [Fact]
    public void RefusesAQuantityOutsideOneToNinetyNine()
    {
        Assert.Equal(400, OrderHandlers.AddItemToOrder(Request(customer: 1, body: new { sku = "CAP", qty = 0 }), 1001).Status);
        Assert.Equal(400, OrderHandlers.AddItemToOrder(Request(customer: 1, body: new { sku = "CAP", qty = "2" }), 1001).Status);
    }

    [Fact]
    public void RefusesAPaidOrder()
    {
        Assert.Equal(409, OrderHandlers.AddItemToOrder(Request(customer: 3, body: new { sku = "CAP", qty = 1 }), 1004).Status);
    }

    [Fact]
    public void RefusesAnotherCustomersOrder()
    {
        Assert.Equal(404, OrderHandlers.AddItemToOrder(Request(customer: 2, body: new { sku = "CAP", qty = 1 }), 1001).Status);
    }

    // DELETE /orders/:id/items/:sku

    [Fact]
    public void RemovesAnItemForTheOwner()
    {
        var res = OrderHandlers.RemoveItemFromOrder(Request(customer: 1), 1007, "HOODIE");
        Assert.Equal(200, res.Status);
        Assert.Equal(0, Total(res));
    }

    [Fact]
    public void RefusesToRemoveFromAnotherCustomersOrderAndLeavesItUntouched()
    {
        Assert.Equal(404, OrderHandlers.RemoveItemFromOrder(Request(customer: 2), 1006, "MUG").Status);
        Assert.Equal(7500, Total(OrderHandlers.GetOrder(Request(customer: 1), 1006)));
    }

    [Fact]
    public void RefusesToRemoveFromAPaidOrder()
    {
        Assert.Equal(409, OrderHandlers.RemoveItemFromOrder(Request(customer: 3), 1004, "TEE").Status);
    }

    // GET /orders/:id/receipt

    [Fact]
    public void PrintsTheReceiptForTheOwner()
    {
        var res = OrderHandlers.GetReceipt(Request(customer: 1), 1006);
        Assert.Equal(200, res.Status);
        Assert.Contains("Alice Carter", Body(res).GetProperty("receipt").GetString());
    }

    [Fact]
    public void HidesTheReceiptFromAnotherCustomer()
    {
        var res = OrderHandlers.GetReceipt(Request(customer: 2), 1006);
        Assert.Equal(404, res.Status);
        Assert.DoesNotContain("Alice", JsonSerializer.Serialize(res.Body));
    }

    [Fact]
    public void ReceiptAnswersUnauthorizedWithoutASignedInCustomer()
    {
        Assert.Equal(401, OrderHandlers.GetReceipt(Request(), 1006).Status);
    }

    // GET /warehouse/orders

    [Fact]
    public void ListsOrdersByStatusForStaffGroupedByCustomer()
    {
        var res = OrderHandlers.ListOrdersForWarehouse(StaffRequest(), "open");
        Assert.Equal(200, res.Status);
        var ids = Body(res).EnumerateArray().Select(o => o.GetProperty("id").GetInt32());
        Assert.Equal([1001, 1002, 1005, 1006, 1007, 1003], ids);
    }

    [Fact]
    public void RefusesARequestWithoutTheStaffToken()
    {
        Assert.Equal(403, OrderHandlers.ListOrdersForWarehouse(Request(customer: 1), "open").Status);
    }

    [Fact]
    public void RefusesAStatusOutsideTheKnownOnes()
    {
        Assert.Equal(400, OrderHandlers.ListOrdersForWarehouse(StaffRequest(), "open' OR '1'='1").Status);
    }
}
