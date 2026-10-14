using static Orders.Money;
using static Orders.Orders.DiscountCodes;

namespace Orders.Orders;

public record Totals(int Subtotal, int BulkDiscount, int Shipping, int Total, int CodeDiscount = 0);

public static class Pricing
{
    public const int FreeShippingFrom = 5000;
    public const int Shipping = 490;

    /* Prices an order: the items, the bulk discount, the discount code, then shipping. */
    public static Totals PriceOrder(Order order, DiscountCode? code = null)
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

        // the code takes its percentage off the goods, as long as they still reach its minimum
        var codeDiscount = code is not null && Qualifies(code, goods) ? PercentOf(goods, code.Percent) : 0;
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

        return new Totals(subtotal, bulkDiscount, shipping, discounted + shipping, codeDiscount);
    }
}
