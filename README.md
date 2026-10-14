# Order service · C#

A small order service: customers, orders, a bulk discount and shipping, with a web page to look at an order.

It needs the .NET 10 SDK.

```bash
dotnet restore
dotnet test                        # unit tests
dotnet run --project src/Orders    # http://localhost:3000
```

Open http://localhost:3000 for the order page. The API:

```bash
curl -H 'X-Customer-Id: 1' localhost:3000/orders/1002
curl -H 'X-Customer-Id: 1' -H 'Content-Type: application/json' \
     -d '{"sku":"CAP","qty":1}' localhost:3000/orders/1002/items
```

The data is in memory and starts from the same seed every time the server starts. The warehouse endpoints need `STAFF_TOKEN` in the environment.

## Layout

- `src/Orders/Program.cs` one line per endpoint, and the page from `src/Orders/wwwroot/`
- `src/Orders/Orders/OrderHandlers.cs` one method per endpoint; every order handler loads the order through `src/Orders/Orders/Access.cs`
- `src/Orders/Orders/OrderRepository.cs`, `src/Orders/Customers/CustomerRepository.cs` all SQL, always parameterised
- `src/Orders/Orders/Pricing.cs` the price rules, `src/Orders/Money.cs` cents and rounding (see `docs/money.md`)
- `src/Orders/Config.cs` settings and secrets, from the environment only
- `src/Orders/wwwroot/` the web page: `app.js` holds the state, `components/` one file per component, `api.js` the calls
- `tests/Orders.Tests/` one test class per module; the ones that use the database share the `Database` collection, so they run one at a time

The open ticket is `docs/TICKET-142.md`.
