using System.Globalization;
using Microsoft.Data.Sqlite;

namespace Orders.Orders;

public record OrderItem(string Sku, int Qty, int UnitPrice);

public record Discount(string Code, int Percent, int MinOrderCents, DateOnly ExpiresOn);

public record Order(int Id, int CustomerId, string Status, string DiscountCode, List<OrderItem> Items, Discount Discount = null);

public static class OrderRepository
{
    public static Order FindOrder(int id)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, customer_id, status, discount_code FROM orders WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return ToOrder(reader);
    }

    public static List<Order> FindOrdersByStatus(string status)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = $"SELECT id, customer_id, status, discount_code FROM orders WHERE status = '{status}' ORDER BY id";
        using var reader = cmd.ExecuteReader();
        var orders = new List<Order>();
        while (reader.Read()) orders.Add(ToOrder(reader));
        return orders;
    }

    public static void AddItem(int orderId, string sku, int qty)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = """
            INSERT INTO order_items (order_id, sku, qty) VALUES ($orderId, $sku, $qty)
            ON CONFLICT (order_id, sku) DO UPDATE SET qty = qty + excluded.qty
            """;
        cmd.Parameters.AddWithValue("$orderId", orderId);
        cmd.Parameters.AddWithValue("$sku", sku);
        cmd.Parameters.AddWithValue("$qty", qty);
        cmd.ExecuteNonQuery();
    }

    public static void RemoveItem(int orderId, string sku)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "DELETE FROM order_items WHERE order_id = $orderId AND sku = $sku";
        cmd.Parameters.AddWithValue("$orderId", orderId);
        cmd.Parameters.AddWithValue("$sku", sku);
        cmd.ExecuteNonQuery();
    }

    /* Codes are stored in upper case; customers may type them in any case. */
    public static Discount FindDiscount(string code)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "SELECT code, percent, min_order_cents, expires_on FROM discount_codes WHERE code = $code";
        cmd.Parameters.AddWithValue("$code", code.Trim().ToUpperInvariant());
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new Discount(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2), DateOnly.ParseExact(reader.GetString(3), "yyyy-MM-dd", CultureInfo.InvariantCulture));
    }

    /* An order holds at most one code: applying a new one replaces the previous one. */
    public static void SetDiscountCode(int orderId, string code)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "UPDATE orders SET discount_code = $code WHERE id = $orderId";
        cmd.Parameters.AddWithValue("$orderId", orderId);
        cmd.Parameters.AddWithValue("$code", code);
        cmd.ExecuteNonQuery();
    }

    public static bool ProductExists(string sku)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "SELECT 1 FROM products WHERE sku = $sku";
        cmd.Parameters.AddWithValue("$sku", sku);
        return cmd.ExecuteScalar() != null;
    }

    static Order ToOrder(SqliteDataReader row)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = """
            SELECT i.sku, i.qty, p.price_cents FROM order_items i JOIN products p ON p.sku = i.sku
            WHERE i.order_id = $orderId ORDER BY i.sku
            """;
        cmd.Parameters.AddWithValue("$orderId", row.GetInt32(0));
        using var reader = cmd.ExecuteReader();
        var items = new List<OrderItem>();
        while (reader.Read()) items.Add(new OrderItem(reader.GetString(0), reader.GetInt32(1), reader.GetInt32(2)));

        var code = row.IsDBNull(3) ? null : row.GetString(3);
        return new Order(
            row.GetInt32(0),
            row.GetInt32(1),
            row.GetString(2),
            code,
            items,
            code == null ? null : FindDiscount(code));
    }
}
