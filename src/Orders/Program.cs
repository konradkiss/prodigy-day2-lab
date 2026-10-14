using System.Text.Encodings.Web;
using Orders;
using Orders.Orders;
using static Orders.Http;

/* The content root is the build output, where the page in wwwroot is copied: it works from any working directory. */
var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, ContentRootPath = AppContext.BaseDirectory });
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
var app = builder.Build();

app.UseExceptionHandler(errors => errors.Run(context => Send(new Response(500, new { error = "internal error" })).ExecuteAsync(context)));

/* The web page: plain files from wwwroot, nothing generated. Served before routing, so the API's 404 doesn't hide them. */
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

/* One line per endpoint; the handlers live in Orders/OrderHandlers.cs. */
app.MapGet("/orders/{id:int}", async (HttpRequest incoming, int id) =>
    Send(OrderHandlers.GetOrder(await ReadRequest(incoming), id)));
app.MapPost("/orders/{id:int}/items", async (HttpRequest incoming, int id) =>
    Send(OrderHandlers.AddItemToOrder(await ReadRequest(incoming), id)));
app.MapDelete("/orders/{id:int}/items/{sku}", async (HttpRequest incoming, int id, string sku) =>
    Send(OrderHandlers.RemoveItemFromOrder(await ReadRequest(incoming), id, sku)));
app.MapGet("/orders/{id:int}/receipt", async (HttpRequest incoming, int id) =>
    Send(OrderHandlers.GetReceipt(await ReadRequest(incoming), id)));
app.MapGet("/warehouse/orders", async (HttpRequest incoming) =>
    Send(OrderHandlers.ListOrdersForWarehouse(await ReadRequest(incoming), incoming.Query["status"].FirstOrDefault() ?? OrderStatus.Paid)));
app.MapFallback("{*path}", () => Send(new Response(404, new { error = "no such route" })));

app.Run($"http://*:{Config.Port}");

static IResult Send(Response res) => Results.Json(res.Body ?? new { }, statusCode: res.Status);
