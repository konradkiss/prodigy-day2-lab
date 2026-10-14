using System.Text.Json;

namespace Orders;

public record Request(string Method, string Path, IHeaderDictionary Headers, JsonElement Body);

public record Response(int Status, object Body);

public static class Http
{
    public static async Task<Request> ReadRequest(HttpRequest incoming)
    {
        return new Request(incoming.Method, incoming.Path, incoming.Headers, await ReadBody(incoming));
    }

    public static async Task<JsonElement> ReadBody(HttpRequest incoming)
    {
        using var reader = new StreamReader(incoming.Body);
        var raw = await reader.ReadToEndAsync();
        return JsonSerializer.Deserialize<JsonElement>(raw == "" ? "{}" : raw);
    }

    public static Response NotFound(string what) => new(404, new { error = $"{what} not found" });

    public static Response BadRequest(string error) => new(400, new { error });
}
