using static Orders.Money;

namespace Orders.Orders;

public record Totals(int Subtotal, int BulkDiscount, int CodeDiscount, int Shipping, int Total);

public static class Pricing
{
    public const int FreeShippingFrom = 5000;
    public const int Shipping = 490;

    /* Prices an order: the items, the bulk discount, the discount code, then shipping. */
    public static Totals PriceOrder(Order order)
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

        // the code only counts while the goods total still reaches its minimum
        var codeDiscount = 0;
        if (order.Discount != null && goods >= order.Discount.MinOrderCents)
        {
            codeDiscount = PercentOf(goods, order.Discount.Percent);
        }
        goods -= codeDiscount;

        var shipping = Shipping;
        if (order.Items.Count == 0)
        {
            shipping = 0;
        }
        else if (goods >= FreeShippingFrom)
        {
            shipping = 0;
        }

        return new Totals(subtotal, bulkDiscount, codeDiscount, shipping, goods + shipping);
    }

    /* Why a code can't be applied to an order today, or null if it can. */
    public static string RefuseDiscount(Discount discount, Order order, DateOnly today)
    {
        if (discount == null) return "unknown discount code";
        if (today > discount.ExpiresOn) return $"discount code {discount.Code} expired on {discount.ExpiresOn:yyyy-MM-dd}";
        var totals = PriceOrder(order with { Discount = null });
        var goods = totals.Subtotal - totals.BulkDiscount;
        if (goods < discount.MinOrderCents) return $"discount code {discount.Code} needs an order of at least {FormatCents(discount.MinOrderCents)}";
        return null;
    }
}
