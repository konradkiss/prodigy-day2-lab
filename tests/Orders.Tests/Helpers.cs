using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Orders.Tests;

public static class Helpers
{
    public const string TestStaffToken = "test-staff-token";

    static Helpers() => Environment.SetEnvironmentVariable("STAFF_TOKEN", TestStaffToken);

    public static Request Request(int? customer = null, object? body = null)
    {
        var headers = new HeaderDictionary();
        if (customer != null) headers["X-Customer-Id"] = customer.ToString();
        return new Request("GET", "/", headers, JsonSerializer.SerializeToElement(body ?? new { }));
    }

    public static Request StaffRequest()
    {
        var headers = new HeaderDictionary { ["X-Staff-Token"] = TestStaffToken };
        return new Request("GET", "/", headers, JsonSerializer.SerializeToElement(new { }));
    }

    /* A response body as the API sends it: camelCase JSON. */
    public static JsonElement Body(Response res) => JsonSerializer.SerializeToElement(res.Body, JsonSerializerOptions.Web);
}

/* The service keeps one in-memory database per process, so the tests that use it run one at a time. */
[CollectionDefinition(nameof(Database), DisableParallelization = true)]
public sealed class Database;
