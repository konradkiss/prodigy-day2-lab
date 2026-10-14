// misc helpers
using static Orders.Customers.CustomerRepository;

namespace Orders;

public static class Utils
{
    public static Customers.Customer GetCust(Request req)
    {
        var id = 0;
        try { id = Convert.ToInt32(req.Headers["X-Customer-Id"].ToString()); } catch { return null; }
        return FindCustomer(id);
    }

    // returns true if ok
    public static bool CheckStuff(object order, object cust)
    {
        if (order == null) return false;
        if (cust == null) return false;
        return true;
    }

    public static Response Err(int status, string msg)
    {
        return new Response(status, new { error = msg });
    }

    public static Response Err2(string msg)
    {
        return new Response(400, new { message = msg });
    }

    // TODO: remove, not used anymore?
    public static string FormatMoney(int c)
    {
        return (c / 100.0).ToString("0.00") + " EUR";
    }
}
