using Orders.Customers;
using Orders.Orders;

namespace Orders.Tests;

[Collection(nameof(Database))]
public class RepositoryTests
{
    public RepositoryTests()
    {
        Db.Seed();
    }

    // orders

    [Fact]
    public void LoadsAnOrderWithItsPricedItems()
    {
        var order = OrderRepository.FindOrder(1003);
        Assert.NotNull(order);
        Assert.Equal((1003, 2, "open", (string?)null), (order.Id, order.CustomerId, order.Status, order.DiscountCode));
        Assert.Equal([new OrderItem("MUG", 1, 1250), new OrderItem("TEE", 2, 2490)], order.Items);
    }

    [Fact]
    public void ReturnsNullForAMissingOrder()
    {
        Assert.Null(OrderRepository.FindOrder(9999));
    }

    [Fact]
    public void MergesQuantitiesWhenTheSameProductIsAddedTwice()
    {
        OrderRepository.AddItem(1001, "HOODIE", 2);
        Assert.Equal([new OrderItem("HOODIE", 3, 4990)], OrderRepository.FindOrder(1001)!.Items);
    }

    [Fact]
    public void RemovesAnItem()
    {
        OrderRepository.RemoveItem(1001, "HOODIE");
        Assert.Empty(OrderRepository.FindOrder(1001)!.Items);
    }

    [Fact]
    public void FiltersByStatus()
    {
        Assert.Equal([1004], OrderRepository.FindOrdersByStatus(OrderStatus.Paid).Select(o => o.Id));
    }

    // customers

    [Fact]
    public void FindsACustomerByEmail()
    {
        Assert.Equal(new Customer(2, "Bob Miller", "bob@example.com"), CustomerRepository.FindCustomerByEmail("bob@example.com"));
    }

    [Fact]
    public void TreatsAQuotedEmailAsDataNotSql()
    {
        Assert.Null(CustomerRepository.FindCustomerByEmail("x' OR '1'='1"));
    }
}
