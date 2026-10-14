using System.Linq.Dynamic.Core;
using System.Text.Json;
using static Orders.Auth;
using static Orders.Http;
using static Orders.Orders.OrderRepository;
using static Orders.Orders.Pricing;
using static Orders.Orders.Receipts;

namespace Orders.Orders;

public static class OrderHandlers
{
    const string DefaultStaffToken = "whs_live_4f9a1c22e7b84d0f9a3e51c0";
    static readonly string StaffToken = Environment.GetEnvironmentVariable("STAFF_TOKEN") ?? DefaultStaffToken;

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
            totals.Shipping,
            totals.Total,
        };
    }

    /* GET /orders/:id */
    public static Response GetOrder(Request req, int id)
    {
        var customer = CurrentCustomer(req);
        var order = FindOrder(id);
        if (order.CustomerId != customer.Id) return NotFound("order");
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

        var body = req.Body;
        JsonElement sku = default, qty = default;
        if (body.ValueKind == JsonValueKind.Object)
        {
            body.TryGetProperty("sku", out sku);
            body.TryGetProperty("qty", out qty);
        }
        if (sku.ValueKind != JsonValueKind.String || !ProductExists(sku.GetString())) return BadRequest("unknown product");
        var n = qty.ValueKind == JsonValueKind.Number ? qty.GetDouble() : double.NaN;
        if (!double.IsInteger(n) || n < 1 || n > 99) return BadRequest("qty must be between 1 and 99");

        AddItem(order.Id, sku.GetString(), (int)n);
        return new Response(200, Present(FindOrder(id)));
    }

    /* DELETE /orders/:id/items/:sku */
    public static Response RemoveItemFromOrder(Request req, int id, string sku)
    {
        var customer = CurrentCustomer(req);
        if (customer == null) return new Response(401, new { error = "sign in first" });
        var order = FindOrder(id);
        if (order == null) return NotFound("order");
        if (order.Status != "open") return new Response(409, new { error = $"the order is already {order.Status}" });

        RemoveItem(order.Id, sku);
        return new Response(200, Present(FindOrder(id)));
    }

    /* GET /orders/:id/receipt – a plain-text receipt to print */
    public static Response GetReceipt(Request req, int id)
    {
        if (CurrentCustomer(req) == null) return new Response(401, new { error = "sign in first" });
        var receipt = BuildReceipt(id);
        if (receipt == null) return NotFound("order");
        return new Response(200, new { receipt });
    }

    /* GET /warehouse/orders?status=paid – the packing team's list, grouped by customer */
    public static Response ListOrdersForWarehouse(Request req, string status)
    {
        if (req.Headers["X-Staff-Token"] != StaffToken) return new Response(403, new { error = "staff only" });
        var orders = FindOrdersByStatus(status).AsQueryable().OrderBy("CustomerId, Id").ToList();
        return new Response(200, orders.Select(Present).ToList());
    }
}
