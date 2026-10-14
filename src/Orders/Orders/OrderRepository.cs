using Microsoft.Data.Sqlite;

namespace Orders.Orders;

public record OrderItem(string Sku, int Qty, int UnitPrice);

public record Order(int Id, int CustomerId, string Status, string? DiscountCode, List<OrderItem> Items);

/* The states an order moves through, stored and shown as these words. */
public static class OrderStatus
{
    public const string Open = "open";
    public const string Paid = "paid";
    public const string Shipped = "shipped";

    public static bool IsKnown(string? value) => value is Open or Paid or Shipped;
}

/* Every query takes its values as parameters; SQL is never built from strings. */
public static class OrderRepository
{
    public static Order? FindOrder(int id)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, customer_id, status, discount_code FROM orders WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ToOrder(reader) : null;
    }

    public static List<Order> FindOrdersByStatus(string status)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, customer_id, status, discount_code FROM orders WHERE status = $status ORDER BY id";
        cmd.Parameters.AddWithValue("$status", status);
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

        return new Order(
            row.GetInt32(0),
            row.GetInt32(1),
            row.GetString(2),
            row.IsDBNull(3) ? null : row.GetString(3),
            items);
    }
}
