using System.Diagnostics.CodeAnalysis;
using Orders.Customers;
using static Orders.Auth;
using static Orders.Http;
using static Orders.Orders.OrderRepository;

namespace Orders.Orders;

/* A signed-in customer and one of their orders. */
public sealed record OwnOrder(Customer Customer, Order Order);

/*
 * Every order handler goes through here: the caller must be signed in, the order must exist,
 * and it must be theirs. Someone else's order looks exactly like a missing one.
 */
public static class Access
{
    public static bool TryLoadOwnOrder(Request req, int orderId,
        [NotNullWhen(true)] out OwnOrder? own, [NotNullWhen(false)] out Response? refusal)
    {
        own = null;
        var customer = CurrentCustomer(req);
        if (customer is null)
        {
            refusal = Unauthorized();
            return false;
        }
        var order = FindOrder(orderId);
        if (order is null || order.CustomerId != customer.Id)
        {
            refusal = NotFound("order");
            return false;
        }
        own = new OwnOrder(customer, order);
        refusal = null;
        return true;
    }

    /* Only an open order can change. */
    public static Response? RequireOpen(Order order) =>
        order.Status == OrderStatus.Open ? null : Conflict($"the order is already {order.Status}");
}
