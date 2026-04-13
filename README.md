# CraftMarket - Serverless Order Processing 🛒

A full-stack e-commerce application for handcrafted products, built with **ASP.NET Core MVC** and **Azure Functions (Durable)**. Customers browse a product catalog, place orders, and confirm them through a serverless order-processing pipeline backed by Entity Framework Core and SQL Server.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Database & Migrations](#database--migrations)
- [API Reference](#api-reference)
- [Deployment](#deployment)

---

## Architecture Overview

The solution is composed of two projects that work together:

| Project             | Type                                 | Responsibility                                                                                                       |
| ------------------- | ------------------------------------ | -------------------------------------------------------------------------------------------------------------------- |
| **ServerlessFunc**           | ASP.NET Core MVC Web App             | Product catalog, user authentication (Identity), order placement, email notifications, admin dashboard               |
| **OrderProcessing** | Azure Functions v4 (Isolated Worker) | Serverless HTTP APIs for order/payment confirmation, Durable Functions orchestrator for long-running order workflows |

**Flow:**

```text
Browser ──► ServerlessFunc (MVC) ──► Places order & sends confirmation email
                │
                ▼
         User clicks link
                │
                ▼
         ServerlessFunc calls ──► OrderProcessing (Azure Functions)
                              ├── ConfirmOrder
                              ├── ConfirmPayment
                              └── OrderProcessingOrchestrator (Durable)
                                       │
                                       ▼
                                   SQL Server (EF Core)
```

---

## Tech Stack

| Layer              | Technology                        | Version  |
| ------------------ | --------------------------------- | -------- |
| **Runtime**        | .NET                              | 8.0      |
| **Web Framework**  | ASP.NET Core MVC                  | 8.0      |
| **Authentication** | ASP.NET Core Identity             | 8.0.6    |
| **Serverless**     | Azure Functions (Isolated Worker) | v4       |
| **Orchestration**  | Durable Functions                 | 1.1.4    |
| **ORM**            | Entity Framework Core             | 8.0.6    |
| **Database**       | SQL Server                        | —        |
| **Monitoring**     | Application Insights              | 2.22.0   |
| **Front-end**      | Bootstrap 5, jQuery               | Vendored |
| **Serialization**  | Newtonsoft.Json                   | 13.0.3   |

---

## Project Structure

```text
EF-ServerlessFunc/
├── ServerlessFunc.sln                          # Visual Studio solution
│
├── ServerlessFunc/                             # ASP.NET Core MVC Web Application
│   ├── Program.cs                     # App startup, DI, Identity, seed data
│   ├── Controllers/
│   │   ├── HomeController.cs          # Landing page & product listing
│   │   ├── MyWorkController.cs        # Catalog browsing, order placement, email & confirmation
│   │   ├── AdminController.cs         # Admin-only order management
│   │   ├── AboutUsController.cs       # Static about page
│   │   └── ContactUsController.cs     # Contact form
│   ├── Models/
│   │   ├── CraftMarketContext.cs      # EF Core DbContext (business entities)
│   │   ├── Product.cs                 # Product entity
│   │   ├── Category.cs               # Category entity
│   │   ├── Order.cs                   # Order entity (status tracking)
│   │   └── SeedData.cs               # Initial categories & products
│   ├── Data/
│   │   ├── ApplicationDbContext.cs    # Identity DbContext
│   │   └── Migrations/               # Identity schema migrations
│   ├── Migrations/                    # Business entity migrations
│   ├── Views/                         # Razor views & layouts
│   └── wwwroot/                       # Static assets (CSS, JS, vendor libs)
│
└── OrderProcessing/                   # Azure Functions v4 (Isolated Worker)
    ├── Program.cs                     # Functions host configuration
    ├── OrderConfirmationFunction.cs   # HTTP POST — confirm an order
    ├── PaymentProcessingFunction.cs   # HTTP POST — confirm a payment
    └── OrderProcessingOrchestrator.cs # Durable orchestrator for order workflows
```

---

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/sql-server) (local, Docker, or Azure SQL)
- [Azure Functions Core Tools v4](https://learn.microsoft.com/azure/azure-functions/functions-run-local) (for local Functions development)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/) with the C# extension

---

## Getting Started

### 1. Clone the repository

```bash
git clone <repository-url>
cd EF-ServerlessFunc
```

### 2. Configure the web app

Update connection strings and the Functions base URL in `ServerlessFunc/appsettings.json` (or preferably use [User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<your-server>;Database=CraftMarket;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "FunctionAppBaseUrl": "http://localhost:7235"
}
```

### 3. Configure the Functions app

Create `OrderProcessing/local.settings.json` (this file is gitignored):

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "SqlConnectionString": "Server=<your-server>;Database=CraftMarket;Trusted_Connection=True;TrustServerCertificate=True;",
    "FunctionAppBaseUrl": "http://localhost:7235"
  }
}
```

### 4. Apply database migrations

```bash
cd ServerlessFunc
dotnet ef database update --context ApplicationDbContext
dotnet ef database update --context CraftMarketContext
```

On first run the application also seeds initial **categories** and **products** automatically.

### 5. Run both projects

**Terminal 1 - Web App:**

```bash
cd ServerlessFunc
dotnet run
```

The MVC app starts at `https://localhost:7275` (or `http://localhost:5063`).

**Terminal 2 - Azure Functions:**

```bash
cd OrderProcessing
func start --port 7235
```

The Functions app starts at `http://localhost:7235`.

### 6. Default admin account

On first startup the app creates a default admin user:

| Field        | Value                          |
| ------------ | ------------------------------ |
| **Role**     | Admin                          |
| **Email**    | _(configured in `Program.cs`)_ |
| **Password** | _(configured in `Program.cs`)_ |

---

## Configuration

### Web App (`ServerlessFunc`)

| Setting                               | Location                          | Description                                        |
| ------------------------------------- | --------------------------------- | -------------------------------------------------- |
| `ConnectionStrings:DefaultConnection` | `appsettings.json` / User Secrets | SQL Server connection string                       |
| `FunctionAppBaseUrl`                  | `appsettings.json`                | Base URL of the Azure Functions app                |
| `ASPNETCORE_ENVIRONMENT`              | `launchSettings.json` / env var   | Runtime environment (`Development` / `Production`) |

### Azure Functions (`OrderProcessing`)

| Setting                                 | Location                             | Description                                                         |
| --------------------------------------- | ------------------------------------ | ------------------------------------------------------------------- |
| `SqlConnectionString`                   | `local.settings.json` / App Settings | SQL Server connection string (same database as web app)             |
| `FunctionAppBaseUrl`                    | `local.settings.json` / App Settings | Self-referencing base URL (used by orchestrator)                    |
| `AzureWebJobsStorage`                   | `local.settings.json` / App Settings | Azure Storage connection (use `UseDevelopmentStorage=true` locally) |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | App Settings                         | Application Insights telemetry (optional locally)                   |

---

## Database & Migrations

The application uses **two EF Core contexts** against the same SQL Server database:

| Context                | Purpose                                          | Migrations Path          |
| ---------------------- | ------------------------------------------------ | ------------------------ |
| `ApplicationDbContext` | ASP.NET Core Identity (users, roles, claims)     | `ServerlessFunc/Data/Migrations/` |
| `CraftMarketContext`  | Business entities (Products, Categories, Orders) | `ServerlessFunc/Migrations/`      |

### Key entities

```text
Category ──1:N──► Product
Order (OrderStatus, PaymentStatus, OrderDate, UserEmail, ProductId)
```

### Adding a new migration

```bash
cd ServerlessFunc
dotnet ef migrations add <MigrationName> --context CraftMarketContext
dotnet ef database update --context CraftMarketContext
```

---

## API Reference

### Azure Functions HTTP Endpoints

All endpoints use **anonymous** authentication.

#### Confirm Order

```bash
POST /api/ConfirmOrder?orderId={id}
```

Sets the `OrderStatus` of the specified order to **Confirmed**.

| Parameter | Type  | In    | Required |
| --------- | ----- | ----- | -------- |
| `orderId` | `int` | Query | Yes      |

**Response:** `200 OK` with confirmation message, or `404 Not Found`.

---

#### Confirm Payment

```bash
POST /api/ConfirmPayment?orderId={id}
```

Sets the `PaymentStatus` of the specified order to **Confirmed**.

| Parameter | Type  | In    | Required |
| --------- | ----- | ----- | -------- |
| `orderId` | `int` | Query | Yes      |

**Response:** `200 OK` with confirmation message, or `404 Not Found`.

---

#### Durable Orchestrator

The `OrderProcessingOrchestrator` implements a long-running workflow using Azure Durable Functions:

1. Waits for an **OrderConfirmed** external event (24-hour timeout)
2. Waits for a **PaymentConfirmed** external event (24-hour timeout)
3. Calls `UpdateOrderStatus` to finalize the order

> **Note:** The orchestrator is partially implemented — external event triggers and the `UpdateOrderStatus` endpoint are not yet wired up.

---

### MVC Routes

| Route                            | Auth          | Description                                    |
| -------------------------------- | ------------- | ---------------------------------------------- |
| `/`                              | Public        | Home page with product listing                 |
| `/MyWork`                        | Public        | Full product catalog                           |
| `/MyWork/OrderProduct`           | Authenticated | Place an order                                 |
| `/MyWork/ConfirmOrderAndPayment` | Authenticated | Confirm order via Functions API                |
| `/Admin`                         | Admin role    | Order management dashboard                     |
| `/AboutUs`                       | Public        | About page                                     |
| `/ContactUs`                     | Public        | Contact form                                   |
| `/Identity/*`                    | —             | ASP.NET Identity pages (login, register, etc.) |

---

## Deployment

Both projects include Visual Studio **Azure publish profiles** for deployment:

- **ServerlessFunc** → Azure Web App (`ServerlessFunc-WebApp`)
- **OrderProcessing** → Azure Function App (`OrderProcessing-Function`)

### Manual deployment

```bash
# Publish the web app
cd ServerlessFunc
dotnet publish -c Release -o ./publish

# Publish the Functions app
cd ../OrderProcessing
dotnet publish -c Release -o ./publish
```

Deploy the published output to your Azure resources via the Azure CLI, Visual Studio, or your preferred CI/CD pipeline.

### Azure resource requirements

- **App Service** (or App Service Plan) for the MVC web app
- **Function App** (Consumption or Premium plan) with .NET 8 isolated worker
- **Azure SQL Database**
- **Storage Account** (required by Azure Functions)
- **Application Insights** (recommended)
