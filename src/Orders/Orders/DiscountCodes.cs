using System.Globalization;

namespace Orders.Orders;

/* A code from the marketing team's tool. ExpiresOn is the last day the code works. */
public record DiscountCode(string Code, int Percent, int MinOrderCents, DateOnly ExpiresOn)
{
    public bool IsExpiredOn(DateOnly day) => day > ExpiresOn;

    public bool QualifiesFor(int goods) => goods >= MinOrderCents;
}

public static class DiscountCodes
{
    /* Codes are stored in upper case; customers type them in any case. */
    public static DiscountCode FindDiscountCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "SELECT code, percent, min_order_cents, expires_on FROM discount_codes WHERE code = $code";
        cmd.Parameters.AddWithValue("$code", code.Trim().ToUpperInvariant());
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new DiscountCode(
            reader.GetString(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            DateOnly.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    public static void SetDiscountCode(int orderId, string code)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "UPDATE orders SET discount_code = $code WHERE id = $orderId";
        cmd.Parameters.AddWithValue("$code", code);
        cmd.Parameters.AddWithValue("$orderId", orderId);
        cmd.ExecuteNonQuery();
    }

    /* The shop's calendar day: a code works until the end of its ExpiresOn day. */
    public static DateOnly Today() => DateOnly.FromDateTime(DateTime.Now);
}
