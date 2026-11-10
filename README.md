# CraftMarket

An ASP.NET Core 10 MVC store for handcrafted goods whose order confirmation and payment steps run as a Durable Functions orchestration in an Azure Functions app.

[![CI](https://github.com/blaisedegier/craft-store-mvc-azure-functions/actions/workflows/ci.yml/badge.svg)](https://github.com/blaisedegier/craft-store-mvc-azure-functions/actions/workflows/ci.yml)

## What it does

- Shows a catalogue of handcrafted products by category, seeded on first run.
- Lets a registered user (ASP.NET Core Identity) order a product. The order starts as `Pending` and the
  user is emailed a link to a page where they confirm the order and then the payment.
- Confirming the order starts a workflow in the Functions app that marks the order `Confirmed` and waits up
  to 24 hours for payment. Confirming payment in that window marks the order `Completed`; otherwise it
  becomes `Expired`.
- Gives users in the `Admin` role an Orders page listing every order with its product, customer and
  statuses.
- Includes About and Contact pages.

## How it works

The solution has two projects that share one SQL Server database:

| Project | Type | Role |
| --- | --- | --- |
| `CraftMarket` | ASP.NET Core 10 MVC | Catalogue, Identity, ordering, the confirmation page, admin orders list |
| `OrderProcessing` | Azure Functions v4, .NET 10 isolated worker | `ConfirmOrder` and `ConfirmPayment` HTTP functions, the order orchestrator and its activity |

```text
browser -> CraftMarket: place order       -> Orders row (Pending, Pending) + email with confirmation link
browser -> CraftMarket: Confirm Order     -> POST /api/ConfirmOrder?orderId=N
                                             starts orchestration "order-N"
                                               activity: OrderStatus = Confirmed
                                               wait for PaymentConfirmed event, or a 24 hour timer
browser -> CraftMarket: Confirm Payment   -> POST /api/ConfirmPayment?orderId=N
                                             raises PaymentConfirmed on "order-N"
                                               activity: OrderStatus = Completed, PaymentStatus = Confirmed
                         (timer fires first)   activity: OrderStatus = Expired
```

Decisions worth knowing about:

- **The HTTP functions never write to the database.** `ConfirmOrder` schedules the orchestration through
  `DurableTaskClient` and `ConfirmPayment` raises an event on it. Every status change happens in the
  `UpdateOrderStatus` activity, because orchestrator code is replayed and must not do I/O. Each order gets a
  predictable instance id (`order-<id>`), so `ConfirmPayment` can find the running workflow from the order id
  alone, and a second `ConfirmOrder` for the same order is refused with 409. That holds even when two arrive
  at once and both pass the existence check: `host.json` sets `overridableExistingInstanceStates` to
  `NonRunningStates`, so the host will not start a second instance under a running instance's id, and
  `ConfirmOrder` turns that error into the same 409.
- **The Functions app does not reference the web project.** It maps only the three columns it touches
  (`OrderID`, `OrderStatus`, `PaymentStatus`) of the `Orders` table in its own small `OrdersContext`. The web
  app owns the schema through EF Core migrations.
- **One `DbContext` in the web app.** `CraftMarketContext` derives from `IdentityDbContext`, so the Identity
  tables and the store tables (`Category`, `Product`, `Orders`) come from one migration history, and an
  order's `UserId` is a foreign key to `AspNetUsers`. Pending migrations are applied at startup before the
  catalogue is seeded.
- **Users can only see and act on their own orders.** The confirmation page and both confirm actions require
  sign-in and look the order up by id and the signed-in user's id together.

`Order` has `OrderId`, `ProductId`, `UserId`, `OrderStatus` (`Pending`, `Confirmed`, `Completed`, `Expired`)
and `PaymentStatus` (`Pending`, `Confirmed`).

## Running it

Prerequisites:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- SQL Server. The default connection string uses LocalDB, `(localdb)\MSSQLLocalDB`, which is installed with
  Visual Studio on Windows; set `ConnectionStrings:DefaultConnection` to use any other server.
- For the order workflow: [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local)
  and [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite), which Durable Functions
  uses for its state when `AzureWebJobsStorage` is `UseDevelopmentStorage=true`.

### Web app

```bash
git clone https://github.com/blaisedegier/craft-store-mvc-azure-functions.git
cd craft-store-mvc-azure-functions
dotnet build CraftMarket.sln -c Release
dotnet user-secrets set "SeedAdmin:Password" "Choose-a-Passw0rd!" --project CraftMarket
dotnet run --project CraftMarket/CraftMarket.csproj
```

The app listens on <http://localhost:5063>. On first start it creates the `CraftMarket` database, seeds the
catalogue and the `Admin` and `User` roles, and creates `admin@example.com` (change `SeedAdmin:Email` to use
another address) with the password you set. Without `SeedAdmin:Password` no admin account is created.

Settings in `CraftMarket/appsettings.json`; keep anything secret in user secrets or environment variables:

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server database (LocalDB by default) |
| `Smtp:Host`, `Smtp:Port`, `Smtp:EnableSsl`, `Smtp:Username`, `Smtp:Password`, `Smtp:From` | Mail server for the confirmation email. When `Smtp:Host` is empty no email is sent and the confirmation link is shown on the order page instead. |
| `SeedAdmin:Email`, `SeedAdmin:Password` | Admin account created at startup |
| `FunctionAppBaseUrl` | Base URL of the Functions app (`http://localhost:7235` locally) |
| `FunctionAppKey` | Function key sent as `x-functions-key`; leave empty locally, where keys are not checked |

### Functions app

```bash
cp OrderProcessing/local.settings.json.example OrderProcessing/local.settings.json
azurite --silent --location .azurite        # in a separate terminal
cd OrderProcessing
func start
```

`local.settings.json` is gitignored. It points `SqlConnectionString` at the same LocalDB database as the web
app and sets the local port to 7235, which is where the web app's `FunctionAppBaseUrl` points. Start the web
app first so the database exists.

## Tech

ASP.NET Core 10 MVC, ASP.NET Core Identity, EF Core 10 with SQL Server, Azure Functions v4 (isolated worker),
Durable Functions, Bootstrap 5.

## Known limitations

- There is no payment provider: **Confirm Payment** records that payment was made without taking one.
- Order statuses change in the background, so the confirmation page shows the new status after a refresh.
- An order that has expired cannot be confirmed again; place a new order instead.
- The contact form validates its input but does not store or send the message.
- Products and categories come only from the seed data; there are no screens to manage them.
- Prices are whole rand amounts.
- There are no automated tests.
