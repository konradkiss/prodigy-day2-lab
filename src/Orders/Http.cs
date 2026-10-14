using System.Text.Json;

namespace Orders;

internal record Request(string Method, string Path, IHeaderDictionary Headers, JsonElement Body);

internal record Response(int Status, object Body);

internal static class Http
{
    public static async Task<Request> ReadRequest(HttpRequest incoming)
    {
        return new Request(incoming.Method, incoming.Path, incoming.Headers, await ReadBody(incoming));
    }

    public static async Task<JsonElement> ReadBody(HttpRequest incoming)
    {
        using var reader = new StreamReader(incoming.Body);
        var raw = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<JsonElement>(raw.Length == 0 ? "{}" : raw);
    }

    public static Response NotFound(string what) => new(404, new { error = $"{what} not found" });

    public static Response BadRequest(string error) => new(400, new { error });
}
