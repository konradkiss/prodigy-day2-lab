using static Orders.Http;
using static Orders.Orders.Access;
using static Orders.Orders.DiscountCodes;
using static Orders.Orders.OrderRepository;
using static Orders.Orders.Pricing;
using static Orders.Orders.Receipts;

namespace Orders.Orders;

public static class OrderHandlers
{
    /* What the API shows of an order: the order itself plus its prices. */
    public static object Present(Order order)
    {
        var totals = PriceOrder(order, AppliedCode(order));
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
        if (!TryLoadOwnOrder(req, id, out var own, out var refusal)) return refusal;
        return Ok(Present(own.Order));
    }

    /* POST /orders/:id/items  { sku, qty } */
    public static Response AddItemToOrder(Request req, int id)
    {
        if (!TryLoadOwnOrder(req, id, out var own, out var refusal)) return refusal;
        if (RequireOpen(own.Order) is { } closed) return closed;

        var sku = TextField(req.Body, "sku");
        var qty = IntField(req.Body, "qty");
        if (sku is null || !ProductExists(sku)) return BadRequest("unknown product");
        if (qty is not (>= 1 and <= 99)) return BadRequest("qty must be between 1 and 99");

        AddItem(own.Order.Id, sku, qty.Value);
        return Ok(Present(FindOrder(id) ?? own.Order));
    }

    /* DELETE /orders/:id/items/:sku */
    public static Response RemoveItemFromOrder(Request req, int id, string sku)
    {
        if (!TryLoadOwnOrder(req, id, out var own, out var refusal)) return refusal;
        if (RequireOpen(own.Order) is { } closed) return closed;

        RemoveItem(own.Order.Id, sku);
        return Ok(Present(FindOrder(id) ?? own.Order));
    }

    /* POST /orders/:id/discount  { code } – a new code replaces the previous one */
    public static Response ApplyDiscountCode(Request req, int id)
    {
        if (!TryLoadOwnOrder(req, id, out var own, out var refusal)) return refusal;
        if (RequireOpen(own.Order) is { } closed) return closed;

        var typed = TextField(req.Body, "code");
        var code = string.IsNullOrWhiteSpace(typed) ? null : FindDiscountCode(typed);
        if (code is null) return BadRequest("unknown discount code");

        var totals = PriceOrder(own.Order);
        var goods = totals.Subtotal - totals.BulkDiscount;
        if (Refusal(code, goods, DateOnly.FromDateTime(DateTime.Now)) is { } reason) return BadRequest(reason);

        SetDiscountCode(own.Order.Id, code.Code);
        return Ok(Present(FindOrder(id) ?? own.Order));
    }

    /* GET /orders/:id/receipt – a plain-text receipt to print */
    public static Response GetReceipt(Request req, int id)
    {
        if (!TryLoadOwnOrder(req, id, out var own, out var refusal)) return refusal;
        return Ok(new { receipt = BuildReceipt(own.Order, own.Customer) });
    }

    /* GET /warehouse/orders?status=paid – the packing team's list, grouped by customer */
    public static Response ListOrdersForWarehouse(Request req, string status)
    {
        var token = Config.StaffToken();
        if (token.Length == 0 || req.Headers["X-Staff-Token"] != token) return Forbidden("staff only");
        if (!OrderStatus.IsKnown(status)) return BadRequest("status must be open, paid or shipped");

        var orders = FindOrdersByStatus(status).OrderBy(o => o.CustomerId).ThenBy(o => o.Id);
        return Ok(orders.Select(Present).ToList());
    }
}
