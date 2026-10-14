using Orders.Customers;
using static Orders.Customers.CustomerRepository;

namespace Orders;

public static class Auth
{
    /* The API gateway signs the customer in and passes their id in X-Customer-Id. */
    public static Customer? CurrentCustomer(Request req)
    {
        if (!int.TryParse(req.Headers["X-Customer-Id"], out var id)) return null;
        return FindCustomer(id);
    }
}
