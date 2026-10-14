using System.Text.Json;

namespace Orders;

public record Request(string Method, string Path, IHeaderDictionary Headers, JsonElement Body);

public record Response(int Status, object? Body);

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

    /* One helper per outcome, so every handler answers in the same shape. */
    public static Response Ok(object body) => new(200, body);
    public static Response BadRequest(string error) => new(400, new { error });
    public static Response Unauthorized() => new(401, new { error = "sign in first" });
    public static Response Forbidden(string error) => new(403, new { error });
    public static Response NotFound(string what) => new(404, new { error = $"{what} not found" });
    public static Response Conflict(string error) => new(409, new { error });

    /* Reads a text field of a JSON body without trusting its shape. */
    public static string? TextField(JsonElement body, string name) =>
        body.ValueKind == JsonValueKind.Object && body.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /* Reads a whole-number field the same way: 2.5 or "2" is not one. */
    public static int? IntField(JsonElement body, string name) =>
        body.ValueKind == JsonValueKind.Object && body.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
            ? number
            : null;
}
