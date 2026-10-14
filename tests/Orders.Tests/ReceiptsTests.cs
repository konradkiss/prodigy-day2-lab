using Orders.Customers;
using Orders.Orders;

namespace Orders.Tests;

[Collection(nameof(Database))]
public class ReceiptsTests
{
    public ReceiptsTests()
    {
        Db.Seed();
    }

    [Fact]
    public void ListsTheCustomerEveryLineAndTheTotals()
    {
        var receipt = Receipts.BuildReceipt(OrderRepository.FindOrder(1003)!, CustomerRepository.FindCustomer(2)!);
        Assert.Equal(
            [
                "Order 1003 · Bob Miller <bob@example.com>",
                "1 × MUG  €12.50",
                "2 × TEE  €49.80",
                "Shipping  €0.00",
                "Total  €62.30",
            ],
            receipt.Split('\n'));
    }
}
