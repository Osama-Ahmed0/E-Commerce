# ECommerce API

A .NET / ASP.NET Core e-commerce REST API built as a portfolio project to demonstrate production-quality backend engineering patterns — not just CRUD endpoints. It covers authentication, payments, caching, rate limiting, and a layered service architecture with an emphasis on testability and deliberate, justified design decisions.

## Features

- **Authentication & Authorization** — ASP.NET Core Identity with role-based access control (Admin / Customer), JWT bearer tokens, and refresh token rotation with automatic pruning of expired/revoked tokens.
- **Catalog** — Product and category management with hierarchical categories (self-referencing parent/child), pagination, filtering, and sorting.
- **Cart & Checkout** — Per-user cart with stock validation, converted to an order on checkout with server-side stock reservation.
- **Payments** — Stripe integration: server-side `PaymentIntent` creation with amount recalculated from the order (never trusted from the client), idempotency keys to guard against retry duplication, and a signature-verified webhook handler that reconciles order status and restores stock/cart on payment failure.
- **Reviews** — One review per user per product, enforced at both the application layer and via a database unique index.
- **Output Caching** — Tag-based cache invalidation on product and category endpoints, with query-string variance so filtered/paginated results don't collide.
- **Rate Limiting** — Per-IP sliding-window limiting on authentication endpoints to slow down brute-force attempts.
- **Robust Error Handling** — A `ServiceResult<T>` pattern for uniform success/failure signaling from services, paired with a global exception handler that maps EF Core exceptions (`DbUpdateConcurrencyException`, `DbUpdateException`) to appropriate HTTP status codes.
- **API Key Gate** — A lightweight `X-API-Key` middleware in front of the API, with explicit exclusions for docs, health checks, and the Stripe webhook route (which authenticates via signature instead).

## Architecture & Design Decisions

This project intentionally avoids some "textbook" patterns where they'd add ceremony without adding value, and includes others as a deliberate testability seam:

- **No repository / unit-of-work layer over `DbContext`.** `DbContext` already _is_ a unit of work, and `DbSet<T>` already _is_ a repository abstraction. Wrapping them again duplicates functionality EF Core provides for free and adds an extra layer to maintain without a corresponding benefit.
- **Validators as the testability seam, not full repositories.** Business rule validation is extracted into small, focused interfaces (`IProductValidator`, `ICategoryValidator`, `ICartValidator`, `IOrderValidator`, `IReviewValidator`) that can be mocked in service unit tests. This gives the benefits of dependency inversion for the logic that actually varies and needs isolated testing, without abstracting away EF Core itself.
- **`ServiceResult<T>` over exceptions for expected failure paths.** Not-found, validation, and conflict outcomes are modeled as data, not exceptions, keeping control flow explicit in services and letting a single extension method (`ToActionResult`) translate outcomes to HTTP responses consistently across controllers.
- **Cache eviction lives in controllers, not services.** `IOutputCacheStore` is injected into controllers rather than services, so business logic stays framework-agnostic and free of ASP.NET Core hosting concerns, and service unit tests don't need to mock a caching dependency they have no business knowing about.
- **Two-layer idempotency for payments.** Stripe's `IdempotencyKey` protects against duplicate `PaymentIntent` creation on network retries; a check against `order.Status` in the webhook handler protects against duplicate processing of redelivered webhook events. Different failure modes, different layers.
- **Database constraints as a backstop, not a replacement, for application checks.** Uniqueness (e.g. category name scoped to parent) is checked at the application level for a clean error message, _and_ enforced with a database unique index, with `SaveChangesAsync` wrapped in a `try/catch` to catch the race condition an app-level check alone can't close.

## Tech Stack

| Layer       | Technology                                        |
| ----------- | ------------------------------------------------- |
| Framework   | .NET 10 / ASP.NET Core                            |
| Data access | EF Core, `IEntityTypeConfiguration<T>` per entity |
| Identity    | ASP.NET Core Identity                             |
| Auth        | JWT bearer tokens + refresh tokens                |
| Payments    | Stripe.net                                        |
| Mapping     | AutoMapper (`ProjectTo` for query projections)    |
| API docs    | Scalar (OpenAPI UI)                               |
| Testing     | xUnit, Moq, EF Core InMemory, SQLite in-memory    |
| Database    | SQL Server                                        |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB is fine for local development)
- A [Stripe](https://dashboard.stripe.com/register) account (test mode) if you want to exercise payment flows
- [Stripe CLI](https://docs.stripe.com/stripe-cli) for forwarding webhooks locally (optional but recommended)

### Setup

1. **Clone and restore**

   ```bash
   git clone https://github.com/Osama-Ahmed0/E-Commerce.git
   cd Ecommerce
   dotnet restore
   ```

2. **Configure secrets**

   Sensitive values are left blank in `appsettings.json` and should be supplied via [user-secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) rather than committed:

   ```bash
   cd src/ECommerce
   dotnet user-secrets set "Jwt:SigningKey" "<a long random string>"
   dotnet user-secrets set "ApiKey" "<any string you'll send as X-API-Key>"
   dotnet user-secrets set "AdminSeed:UserName" "admin"
   dotnet user-secrets set "AdminSeed:Password" "<a strong password>"
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
   dotnet user-secrets set "Stripe:WebhookSecret" "whsec_..."
   ```

3. **Apply migrations**

   ```bash
   dotnet ef database update
   ```

   On first run, the app also seeds Identity roles, an admin user (from `AdminSeed` config), and a product/category catalog from `Data/SeedData/Data.json`.

4. **Run the API**

   ```bash
   dotnet run
   ```

   The app launches to the Scalar API reference at `/scalar` by default.

5. **(Optional) Forward Stripe webhooks locally**

   ```bash
   stripe listen --forward-to https://localhost:7080/api/payments/webhook
   ```

   Copy the webhook signing secret it prints into `Stripe:WebhookSecret` if it differs from your dashboard value.

## Exploring the API

Interactive, authenticated-aware API documentation is available at `/scalar` when running in Development.

Every request (aside from `/scalar`, `/openapi`, `/health/*`, and the Stripe webhook) requires an `X-API-Key` header matching your configured `ApiKey`. Endpoints under `[Authorize]` additionally require a JWT bearer token obtained from `POST /api/auth/login`.

Major endpoint groups:

| Group | Purpose |
| --- | --- |
| `/api/auth` | Register, login, refresh, revoke |
| `/api/product` | Browse, filter, and (Admin) manage the catalog |
| `/api/category` | Browse and (Admin) manage hierarchical categories |
| `/api/cart` | View and modify the current user's cart |
| `/api/order` | Checkout, order history, status transitions |
| `/api/products/{id}/reviews` | Read and submit product reviews |
| `/api/payments/webhook` | Stripe webhook receiver (signature-authenticated, no API key) |

## Testing

Unit tests cover the validator layer and core service logic using EF Core's InMemory provider with Moq for dependency isolation. InMemory is intentionally _not_ used where relational behavior — unique indexes, transaction rollback — is under test; those cases use SQLite in-memory instead, since InMemory silently ignores both.

```bash
dotnet test
```

## License

This project is licensed under the [MIT License](LICENSE).
