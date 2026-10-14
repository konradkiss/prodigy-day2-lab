using Orders.Customers;
using static Orders.Money;
using static Orders.Orders.Pricing;

namespace Orders.Orders;

internal static class Receipts
{
    /* A plain-text receipt: who ordered what, and what it costs. */
    public static string BuildReceipt(Order order, Customer customer)
    {
        var totals = PriceOrder(order);
        string[] lines =
        [
            $"Order {order.Id} · {customer.Name} <{customer.Email}>",
            .. order.Items.Select(i => $"{i.Qty} × {i.Sku}  {FormatCents(i.UnitPrice * i.Qty)}"),
            $"Shipping  {FormatCents(totals.Shipping)}",
            $"Total  {FormatCents(totals.Total)}",
        ];
        return string.Join("\n", lines);
    }
}
