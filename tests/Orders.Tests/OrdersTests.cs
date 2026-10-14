using System.Globalization;
using System.Text.Json;
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

    private static Response Apply(int orderId, string code, int customer = 1) =>
        OrderHandlers.ApplyDiscountCode(Helpers.Request(customer: customer, body: new { code }), orderId);

    private static JsonElement Shown(int orderId, int customer = 1) =>
        JsonSerializer.SerializeToElement(OrderHandlers.GetOrder(Helpers.Request(customer: customer), orderId).Body);

    private static string? Error(Response res) => JsonSerializer.SerializeToElement(res.Body).GetProperty("error").GetString();

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

    // DELETE /orders/:id/items/:sku

    [Fact]
    public void RemovesAnItemFromItsOwnersOrder()
    {
        var res = OrderHandlers.RemoveItemFromOrder(Helpers.Request(customer: 1), 1002, "MUG");
        Assert.Equal(200, res.Status);
        Assert.Equal(0, Shown(1002).GetProperty("Items").GetArrayLength());
    }

    [Fact]
    public void LeavesAnotherCustomersOrderAlone()
    {
        var res = OrderHandlers.RemoveItemFromOrder(Helpers.Request(customer: 2), 1002, "MUG");
        Assert.Equal(404, res.Status);
        Assert.Equal(1, Shown(1002).GetProperty("Items").GetArrayLength());
    }

    // GET /orders/:id/receipt

    [Fact]
    public void PrintsAReceiptForItsOwner()
    {
        var res = OrderHandlers.GetReceipt(Helpers.Request(customer: 1), 1001);
        Assert.Equal(200, res.Status);
        Assert.StartsWith("Order 1001 · Alice Carter <alice@example.com>", JsonSerializer.SerializeToElement(res.Body).GetProperty("receipt").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public void HidesAnotherCustomersReceipt()
    {
        var res = OrderHandlers.GetReceipt(Helpers.Request(customer: 2), 1001);
        Assert.Equal(404, res.Status);
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

    // POST /orders/:id/discount

    [Fact]
    public void AppliesACodeToAnOpenOrder()
    {
        // 2 × 4990 = 9980, 15% = 1497
        var res = Apply(1007, "SAVE15");
        Assert.Equal(200, res.Status);
        var order = Shown(1007);
        Assert.Equal("SAVE15", order.GetProperty("DiscountCode").GetString());
        Assert.Equal(1497, order.GetProperty("CodeDiscount").GetInt32());
        Assert.Equal(9980 - 1497, order.GetProperty("Total").GetInt32());
    }

    [Fact]
    public void AcceptsACodeInAnyCase()
    {
        Assert.Equal(200, Apply(1007, "save15").Status);
        Assert.Equal("SAVE15", Shown(1007).GetProperty("DiscountCode").GetString());
    }

    [Fact]
    public void AcceptsAnOrderAtExactlyTheMinimum()
    {
        // 4 × 1250 = 5000
        Assert.Equal(200, Apply(1002, "SAVE15").Status);
        Assert.Equal(750, Shown(1002).GetProperty("CodeDiscount").GetInt32());
    }

    [Fact]
    public void RefusesACodeBelowTheMinimum()
    {
        // 1 × 4990
        var res = Apply(1001, "SAVE15");
        Assert.Equal(400, res.Status);
        Assert.Contains("€50.00", Error(res), StringComparison.Ordinal);
        Assert.Equal(JsonValueKind.Null, Shown(1001).GetProperty("DiscountCode").ValueKind);
    }

    [Fact]
    public void RefusesAnExpiredCodeWithAClearMessage()
    {
        var res = Apply(1007, "SUMMER25");
        Assert.Equal(400, res.Status);
        Assert.Equal("the code SUMMER25 expired on 2026-08-31", Error(res));
        Assert.Equal(JsonValueKind.Null, Shown(1007).GetProperty("DiscountCode").ValueKind);
    }

    [Fact]
    public void AcceptsACodeOnItsLastDayInUtc()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "INSERT INTO discount_codes (code, percent, min_order_cents, expires_on) VALUES ('LASTDAY', 20, 0, $today)";
        cmd.Parameters.AddWithValue("$today", today);
        cmd.ExecuteNonQuery();

        Assert.Equal(200, Apply(1007, "LASTDAY").Status);
        Assert.Equal(1996, Shown(1007).GetProperty("CodeDiscount").GetInt32());
    }

    [Fact]
    public void RefusesAnUnknownCode()
    {
        Assert.Equal(400, Apply(1007, "NOPE").Status);
    }

    [Fact]
    public void RefusesARequestWithoutACode()
    {
        var res = OrderHandlers.ApplyDiscountCode(Helpers.Request(customer: 1, body: new { }), 1007);
        Assert.Equal(400, res.Status);
    }

    [Fact]
    public void ReplacesThePreviousCode()
    {
        Assert.Equal(200, Apply(1007, "WELCOME10").Status);
        Assert.Equal(200, Apply(1007, "SAVE15").Status);
        var order = Shown(1007);
        Assert.Equal("SAVE15", order.GetProperty("DiscountCode").GetString());
        Assert.Equal(1497, order.GetProperty("CodeDiscount").GetInt32());
    }

    [Fact]
    public void KeepsTheCodeWithNoDiscountWhenTheOrderDropsBelowTheMinimum()
    {
        // 2 × 2490 + 1250 = 6230; without the mug 4980
        Assert.Equal(200, Apply(1003, "SAVE15", customer: 2).Status);
        OrderHandlers.RemoveItemFromOrder(Helpers.Request(customer: 2), 1003, "MUG");
        var order = Shown(1003, customer: 2);
        Assert.Equal("SAVE15", order.GetProperty("DiscountCode").GetString());
        Assert.Equal(0, order.GetProperty("CodeDiscount").GetInt32());
        Assert.Equal(4980 + Pricing.Shipping, order.GetProperty("Total").GetInt32());
    }

    [Fact]
    public void ChargesShippingWhenTheCodeTakesTheOrderBelowFreeShipping()
    {
        // 5000 - 10% = 4500
        Assert.Equal(200, Apply(1002, "WELCOME10").Status);
        var order = Shown(1002);
        Assert.Equal(Pricing.Shipping, order.GetProperty("Shipping").GetInt32());
        Assert.Equal(4500 + Pricing.Shipping, order.GetProperty("Total").GetInt32());
    }

    [Fact]
    public void RefusesACodeOnAPaidOrder()
    {
        Assert.Equal(409, Apply(1004, "WELCOME10", customer: 3).Status);
    }

    [Fact]
    public void HidesAnotherCustomersOrder()
    {
        Assert.Equal(404, Apply(1007, "WELCOME10", customer: 2).Status);
    }

    [Fact]
    public void AsksToSignInBeforeApplyingACode()
    {
        var res = OrderHandlers.ApplyDiscountCode(Helpers.Request(body: new { code = "SAVE15" }), 1007);
        Assert.Equal(401, res.Status);
    }
}
