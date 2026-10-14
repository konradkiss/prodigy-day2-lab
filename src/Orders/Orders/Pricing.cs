using System.Globalization;
using static Orders.Money;

namespace Orders.Orders;

internal record Totals(int Subtotal, int BulkDiscount, int CodeDiscount, int Shipping, int Total);

internal static class Pricing
{
    public const int FreeShippingFrom = 5000;
    public const int Shipping = 490;

    /* Prices an order with its discount code (if any) as it stands today, in UTC. */
    public static Totals PriceOrder(Order order)
    {
        var code = order.DiscountCode == null ? null : OrderRepository.FindDiscountCode(order.DiscountCode);
        return PriceOrder(order, code, Today());
    }

    /* Prices an order: the items, the bulk discount, the discount code, then shipping. */
    public static Totals PriceOrder(Order order, DiscountCodeTerms? code, DateOnly today)
    {
        var subtotal = 0;
        var bulkDiscount = 0;
        foreach (var item in order.Items)
        {
            var line = item.UnitPrice * item.Qty;
            subtotal += line;
            if (item.Qty >= 10)
            {
                // ten or more of one product: 5% off that line
                bulkDiscount += PercentOf(line, 5);
            }
            else if (item.Sku == "SOCKS" && item.Qty >= 5)
            {
                // socks are sold in packs of five: 10% off
                bulkDiscount += PercentOf(line, 10);
            }
        }

        var goods = subtotal - bulkDiscount;
        // a code that no longer qualifies stays on the order but gives nothing
        var codeDiscount = code != null && WhyCodeDoesNotApply(code, goods, today) == null ? PercentOf(goods, code.Percent) : 0;
        var discounted = goods - codeDiscount;
        var shipping = Shipping;
        if (order.Items.Count == 0)
        {
            shipping = 0;
        }
        else if (discounted >= FreeShippingFrom)
        {
            shipping = 0;
        }

        return new Totals(subtotal, bulkDiscount, codeDiscount, shipping, discounted + shipping);
    }

    /* Null when the code applies to this goods total today; otherwise the reason, for the customer. */
    public static string? WhyCodeDoesNotApply(DiscountCodeTerms code, int goods, DateOnly today)
    {
        if (today > code.ExpiresOn)
        {
            return $"the code {code.Code} expired on {code.ExpiresOn.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
        }

        if (goods < code.MinOrderCents)
        {
            return $"the code {code.Code} needs an order of at least {FormatCents(code.MinOrderCents)}";
        }

        return null;
    }

    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);
}
