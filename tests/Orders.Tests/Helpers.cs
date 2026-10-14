using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Orders.Tests;

internal static class Helpers
{
    public static Request Request(int? customer = null, object? body = null)
    {
        var headers = new HeaderDictionary();
        if (customer != null) headers["X-Customer-Id"] = customer.Value.ToString(CultureInfo.InvariantCulture);
        return new Request("GET", "/", headers, JsonSerializer.SerializeToElement(body ?? new { }));
    }
}
