using System.Text.Json;
using Orders.Customers;
using static Orders.Auth;
using static Orders.Http;
using static Orders.Orders.OrderRepository;
using static Orders.Orders.Pricing;
using static Orders.Orders.Receipts;

namespace Orders.Orders;

internal static class OrderHandlers
{
    /* The staff token comes from the environment only; without it, the warehouse list is closed to everyone. */
    private static readonly string? StaffToken = Environment.GetEnvironmentVariable("STAFF_TOKEN");

    public static object Present(Order order)
    {
        var totals = PriceOrder(order);
        return new
        {
            order.Id,
            order.Status,
            order.DiscountCode,
            order.Items,
            totals.Subtotal,
            totals.BulkDiscount,
            totals.CodeDiscount,
            totals.Shipping,
            totals.Total,
        };
    }

    /* GET /orders/:id */
    public static Response GetOrder(Request req, int id)
    {
        var customer = CurrentCustomer(req);
        if (customer == null) return new Response(401, new { error = "sign in first" });
        var order = FindOrder(id);
        if (order == null || order.CustomerId != customer.Id) return NotFound("order");
        return new Response(200, Present(order));
    }

    /* POST /orders/:id/items  { sku, qty } */
    public static Response AddItemToOrder(Request req, int id)
    {
        var customer = CurrentCustomer(req);
        if (customer == null) return new Response(401, new { error = "sign in first" });
        var order = FindOrder(id);
        if (order == null || order.CustomerId != customer.Id) return NotFound("order");
        if (order.Status != "open") return new Response(409, new { error = $"the order is already {order.Status}" });

        var (sku, qty) = ReadItem(req.Body);
        if (sku == null || !ProductExists(sku)) return BadRequest("unknown product");
        if (!double.IsInteger(qty) || qty < 1 || qty > 99) return BadRequest("qty must be between 1 and 99");

        AddItem(order.Id, sku, (int)qty);
        return ShowOrder(customer, id);
    }

    /* The { sku, qty } of a request body: sku is null unless it is a string, qty is NaN unless it is a number. */
    private static (string? Sku, double Qty) ReadItem(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object) return (null, double.NaN);
        var sku = body.TryGetProperty("sku", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() : null;
        var qty = body.TryGetProperty("qty", out var q) && q.ValueKind == JsonValueKind.Number ? q.GetDouble() : double.NaN;
        return (sku, qty);
    }

    private static Response ShowOrder(Customer customer, int id)
    {
        var order = FindOrder(id);
        if (order == null || order.CustomerId != customer.Id) return NotFound("order");
        return new Response(200, Present(order));
    }

    /* POST /orders/:id/discount  { code } – applies a discount code, replacing any previous one */
    public static Response ApplyDiscountCode(Request req, int id)
    {
        var customer = CurrentCustomer(req);
        if (customer == null) return new Response(401, new { error = "sign in first" });
        var order = FindOrder(id);
        if (order == null || order.CustomerId != customer.Id) return NotFound("order");
        if (order.Status != "open") return new Response(409, new { error = $"the order is already {order.Status}" });

        var typed = ReadCode(req.Body);
        if (string.IsNullOrEmpty(typed)) return BadRequest("code is required");
        var code = FindDiscountCode(typed);
        if (code == null) return BadRequest("unknown discount code");

        var today = Today();
        var totals = PriceOrder(order, null, today);
        var refusal = WhyCodeDoesNotApply(code, totals.Subtotal - totals.BulkDiscount, today);
        if (refusal != null) return BadRequest(refusal);

        SetDiscountCode(order.Id, code.Code);
        return ShowOrder(customer, id);
    }

    /* The code of a { code } request body, or null unless it is a string. */
    private static string? ReadCode(JsonElement body)
    {
        if (body.ValueKind != JsonValueKind.Object) return null;
        return body.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
    }

    /* DELETE /orders/:id/items/:sku */
    public static Response RemoveItemFromOrder(Request req, int id, string sku)
    {
        var customer = CurrentCustomer(req);
        if (customer == null) return new Response(401, new { error = "sign in first" });
        var order = FindOrder(id);
        if (order == null || order.CustomerId != customer.Id) return NotFound("order");
        if (order.Status != "open") return new Response(409, new { error = $"the order is already {order.Status}" });

        RemoveItem(order.Id, sku);
        return ShowOrder(customer, id);
    }

    /* GET /orders/:id/receipt – a plain-text receipt to print */
    public static Response GetReceipt(Request req, int id)
    {
        var customer = CurrentCustomer(req);
        if (customer == null) return new Response(401, new { error = "sign in first" });
        var order = FindOrder(id);
        if (order == null || order.CustomerId != customer.Id) return NotFound("order");
        return new Response(200, new { receipt = BuildReceipt(order, customer) });
    }

    /* GET /warehouse/orders?status=paid – the packing team's list, grouped by customer */
    public static Response ListOrdersForWarehouse(Request req, string status)
    {
        if (StaffToken == null || req.Headers["X-Staff-Token"] != StaffToken) return new Response(403, new { error = "staff only" });
        var orders = FindOrdersByStatus(status).OrderBy(o => o.CustomerId).ThenBy(o => o.Id).ToList();
        return new Response(200, orders.Select(Present).ToList());
    }
}
