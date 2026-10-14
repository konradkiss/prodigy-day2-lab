namespace Orders;

/*
 * Money is always a whole number of cents, held in an int. See docs/money.md.
 */
internal static class Money
{
    /// <summary><paramref name="pct"/> percent of an amount, rounded half up to the cent.</summary>
    public static int PercentOf(int amount, int pct)
    {
        return (amount * pct + 50) / 100;
    }

    public static string FormatCents(int amount)
    {
        var sign = amount < 0 ? "-" : "";
        var abs = Math.Abs(amount);
        return $"{sign}€{abs / 100}.{abs % 100:D2}";
    }
}
