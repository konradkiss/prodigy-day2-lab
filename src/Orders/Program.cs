using System.Text.Encodings.Web;
using Orders;
using Orders.Orders;
using static Orders.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
var app = builder.Build();

app.UseExceptionHandler(errors => errors.Run(context => Send(new Response(500, new { error = "internal error" })).ExecuteAsync(context)));

app.MapGet("/orders/{id:int}", async (HttpRequest incoming, int id) =>
    Send(OrderHandlers.GetOrder(await ReadRequest(incoming), id)));
app.MapPost("/orders/{id:int}/items", async (HttpRequest incoming, int id) =>
    Send(OrderHandlers.AddItemToOrder(await ReadRequest(incoming), id)));
app.MapDelete("/orders/{id:int}/items/{sku}", async (HttpRequest incoming, int id, string sku) =>
    Send(OrderHandlers.RemoveItemFromOrder(await ReadRequest(incoming), id, sku)));
app.MapPost("/orders/{id:int}/discount", async (HttpRequest incoming, int id) =>
    Send(OrderHandlers.ApplyDiscountCode(await ReadRequest(incoming), id)));
app.MapGet("/orders/{id:int}/receipt", async (HttpRequest incoming, int id) =>
    Send(OrderHandlers.GetReceipt(await ReadRequest(incoming), id)));
app.MapGet("/warehouse/orders", async (HttpRequest incoming) =>
    Send(OrderHandlers.ListOrdersForWarehouse(await ReadRequest(incoming), incoming.Query["status"].FirstOrDefault() ?? "paid")));
app.MapFallback("{*path}", () => Send(new Response(404, new { error = "no such route" })));

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
app.Run($"http://*:{port}");

static IResult Send(Response res) => Results.Json(res.Body ?? new { }, statusCode: res.Status);
