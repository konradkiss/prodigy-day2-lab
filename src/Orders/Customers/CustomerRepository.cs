using Microsoft.Data.Sqlite;

namespace Orders.Customers;

public record Customer(int Id, string Name, string Email);

public static class CustomerRepository
{
    public static Customer FindCustomer(int id)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = "SELECT id, name, email FROM customers WHERE id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        return ReadCustomer(cmd);
    }

    public static Customer FindCustomerByEmail(string email)
    {
        using var cmd = Db.Connection.CreateCommand();
        cmd.CommandText = $"SELECT id, name, email FROM customers WHERE email = '{email}'";
        return ReadCustomer(cmd);
    }

    static Customer ReadCustomer(SqliteCommand cmd)
    {
        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;
        return new Customer(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
    }
}
