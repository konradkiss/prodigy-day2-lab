using Orders.Orders;

namespace Orders.Tests;

[Collection(nameof(Database))]
public class DiscountCodesTests
{
    public DiscountCodesTests()
    {
        Db.Seed();
    }

    static readonly DiscountCode Save15 = new("SAVE15", 15, 5000, new DateOnly(2026, 8, 31));

    [Fact]
    public void FindsACodeTypedInAnyCase()
    {
        Assert.Equal(new DiscountCode("SAVE15", 15, 5000, new DateOnly(2099, 12, 31)), DiscountCodes.FindDiscountCode(" save15 "));
    }

    [Fact]
    public void ReturnsNullForAnUnknownCode()
    {
        Assert.Null(DiscountCodes.FindDiscountCode("NOPE"));
    }

    [Fact]
    public void WorksOnItsLastDayAndNotTheDayAfter()
    {
        Assert.Null(DiscountCodes.Refusal(Save15, 5000, new DateOnly(2026, 8, 31)));
        Assert.Equal("the code SAVE15 expired on 2026-08-31", DiscountCodes.Refusal(Save15, 5000, new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public void QualifiesAtExactlyTheMinimumAndNotOneCentBelow()
    {
        var today = new DateOnly(2026, 1, 1);
        Assert.Null(DiscountCodes.Refusal(Save15, 5000, today));
        Assert.Equal("the code SAVE15 needs an order of at least €50.00", DiscountCodes.Refusal(Save15, 4999, today));
    }
}
