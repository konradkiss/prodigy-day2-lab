using static Orders.Money;

namespace Orders.Orders;

public record Totals(int Subtotal, int BulkDiscount, int CodeDiscount, int Shipping, int Total);

public static class Pricing
{
    public const int FreeShippingFrom = 5000;
    public const int Shipping = 490;

    /* Prices an order: the items, the bulk discount, the discount code, then shipping. */
    public static Totals PriceOrder(Order order, DiscountCode code = null, DateOnly? today = null)
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

        // the code takes its percentage off the goods after the bulk discount,
        // as long as it still works and the goods still reach its minimum
        var goods = subtotal - bulkDiscount;
        var codeDiscount = 0;
        if (code != null && !code.IsExpiredOn(today ?? DiscountCodes.Today()) && code.QualifiesFor(goods))
        {
            codeDiscount = PercentOf(goods, code.Percent);
        }
        goods -= codeDiscount;

        // free shipping is decided after the code's discount
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
}
