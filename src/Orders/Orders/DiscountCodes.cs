using System.Globalization;
using static Orders.Money;

namespace Orders.Orders;

/* A marketing code: Percent off the goods total, for orders of at least MinOrderCents, up to and including ExpiresOn. */
public record DiscountCode(string Code, int Percent, int MinOrderCents, DateOnly ExpiresOn);

public static class DiscountCodes
{
    /* Codes are stored in upper case; customers type them in any case. */
    public static string Normalize(string code) => code.Trim().ToUpperInvariant();

    public static DiscountCode? FindDiscountCode(string code)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "SELECT code, percent, min_order_cents, expires_on FROM discount_codes WHERE code = $code";
        cmd.Parameters.AddWithValue("$code", Normalize(code));
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new DiscountCode(
            reader.GetString(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            DateOnly.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    /* The code applied to an order, if it has one. */
    public static DiscountCode? AppliedCode(Order order) =>
        order.DiscountCode is null ? null : FindDiscountCode(order.DiscountCode);

    public static bool IsExpired(DiscountCode code, DateOnly today) => today > code.ExpiresOn;

    public static bool Qualifies(DiscountCode code, int goods) => goods >= code.MinOrderCents;

    /* Why a code can't be applied to an order with this goods total today, or null if it can. */
    public static string? Refusal(DiscountCode code, int goods, DateOnly today)
    {
        if (IsExpired(code, today)) return $"the code {code.Code} expired on {code.ExpiresOn:yyyy-MM-dd}";
        if (!Qualifies(code, goods)) return $"the code {code.Code} needs an order of at least {FormatCents(code.MinOrderCents)}";
        return null;
    }
}
