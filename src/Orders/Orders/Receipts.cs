using static Orders.Customers.CustomerRepository;
using static Orders.Orders.DiscountCodes;
using static Orders.Money;
using static Orders.Orders.OrderRepository;
using static Orders.Orders.Pricing;

namespace Orders.Orders;

public static class Receipts
{
    /* A plain-text receipt: who ordered what, and what it costs. */
    public static string BuildReceipt(int orderId)
    {
        var order = FindOrder(orderId);
        if (order == null) return null;
        var customer = FindCustomer(order.CustomerId);
        var totals = PriceOrder(order, FindDiscountCode(order.DiscountCode));
        string[] lines =
        [
            $"Order {order.Id} · {customer.Name} <{customer.Email}>",
            .. order.Items.Select(i => $"{i.Qty} × {i.Sku}  {FormatCents(i.UnitPrice * i.Qty)}"),
            .. (totals.CodeDiscount > 0 ? [$"Code {order.DiscountCode}  {FormatCents(-totals.CodeDiscount)}"] : Array.Empty<string>()),
            $"Shipping  {FormatCents(totals.Shipping)}",
            $"Total  {FormatCents(totals.Total)}",
        ];
        return string.Join("\n", lines);
    }
}
