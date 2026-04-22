# Subscription Billing System

A senior-level C# sample demonstrating **DDD**, **Clean Architecture**, and **CQRS** against a realistic subscription-billing domain.

## Run it

Prerequisites: **.NET 8 SDK**.

```bash
# From the solution root
dotnet restore
dotnet build
dotnet test                                       # runs domain tests
dotnet run --project src/SubscriptionBilling.Api  # starts the API at http://localhost:5080
```

Open `http://localhost:5080/swagger` for the UI.

### Quick end-to-end via curl

```bash
# 1. Create a customer
curl -s -X POST http://localhost:5080/customers \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: create-cust-1' \
  -d '{"name":"Acme","email":"ops@acme.com"}'

# 2. Create + activate subscription (generates the first invoice)
curl -s -X POST http://localhost:5080/subscriptions \
  -H 'Content-Type: application/json' \
  -H 'Idempotency-Key: create-sub-1' \
  -d '{"customerId":"<CUSTOMER_ID>","planName":"Pro","price":9.99,"currency":"USD","billingCycle":"Monthly"}'

# 3. List invoices for the customer
curl -s http://localhost:5080/customers/<CUSTOMER_ID>/invoices

# 4. Pay the first invoice
curl -s -X POST http://localhost:5080/invoices/<INVOICE_ID>/pay \
  -H 'Idempotency-Key: pay-inv-1'

# 5. Cancel the subscription
curl -s -X POST http://localhost:5080/subscriptions/<SUB_ID>/cancel \
  -H 'Idempotency-Key: cancel-sub-1'
```

## Solution layout

```
src/
  SubscriptionBilling.Domain/          Aggregates, value objects, domain events, repository interfaces
  SubscriptionBilling.Application/     CQRS handlers (MediatR), validation + idempotency pipeline behaviors
  SubscriptionBilling.Infrastructure/  EF Core (InMemory), repositories, outbox interceptor, background job
  SubscriptionBilling.Api/             Minimal API, exception-to-ProblemDetails middleware, Swagger
tests/
  SubscriptionBilling.Domain.Tests/    xUnit + FluentAssertions over pure domain logic
```

### Dependency direction (Clean Architecture)

```
Api ──▶ Application ──▶ Domain
Api ──▶ Infrastructure ──▶ Application ──▶ Domain
```

`Domain` has **zero** framework dependencies. `Application` depends only on `Domain` and `MediatR`/`FluentValidation`. `Infrastructure` is the only project that references EF Core. `Api` wires everything together.

## Design decisions

### Aggregates and their boundaries

Three aggregates: `Customer`, `Subscription`, `Invoice`. They're kept separate (rather than making `Invoice` a child of `Subscription`) so that paying an invoice doesn't require loading the entire subscription history — the transactional boundary stays small. The `Subscription` aggregate is the only place allowed to *construct* a new `Invoice` (via `Invoice.Issue`, which is `internal`), which preserves the rule that invoices are always created through a subscription lifecycle event.

Cross-aggregate references are by **ID only** (`CustomerId`, `SubscriptionId`), never by navigation property. This keeps the aggregates independently loadable and independently persistable.

### Value objects

`Money`, `Email`, `BillingCycle`, and strongly-typed IDs are all value objects with structural equality and validation at construction (static `Of(...)`/`From(...)` factories). The domain layer never accepts raw `decimal`, `string`, or `Guid` for these concepts — once you're past the boundary, you're dealing with already-validated values.

### Rich domain model

Aggregates expose behavior, not property setters. `Activate`, `GenerateNextInvoice`, `Cancel`, `Pay` live on the aggregates themselves. No service layer reaches in to mutate fields — that would collapse into an anemic model. Commands orchestrate; aggregates enforce.

### Domain events

`SubscriptionActivated`, `InvoiceGenerated`, `PaymentReceived` are raised on the aggregate root via `Raise(...)`. The infrastructure picks them up *inside* the same `SaveChanges` that persists the state change (`DomainEventsToOutboxInterceptor`) and writes them to the **outbox** table — so state + events are atomic. A separate dispatcher (not in this sample — see "What's intentionally omitted") would later read the outbox and publish.

### CQRS

Every command and query is a MediatR `IRequest`. Commands return a small response DTO; queries return read-model DTOs (`InvoiceDto`) that are shaped for API consumption, keeping the domain clean of serialization concerns. Two pipeline behaviors wrap every request:

1. `IdempotencyBehavior` — short-circuits duplicate commands by idempotency key.
2. `ValidationBehavior` — runs FluentValidation before the handler.

### Idempotent commands

Any command implementing `IIdempotentCommand<T>` supplies an `IdempotencyKey` (delivered via the `Idempotency-Key` HTTP header). The store records the serialized response the first time; subsequent calls with the same key return the cached response without re-running the handler. This is the well-known pattern for making non-idempotent operations like "create customer" / "pay invoice" safe to retry.

### Background billing job

`BillingCycleHostedService` is a `BackgroundService` that invokes the `RunBillingCycleCommand` on an interval. The command loads every active subscription whose `CurrentPeriodStartUtc` has rolled over and calls `GenerateNextInvoice` — which is intrinsically safe to re-run because the aggregate itself decides whether a new invoice is due. The interval is one minute for demo purposes; in production you'd use a scheduled trigger.

### Persistence

EF Core with the InMemory provider is wired in `Infrastructure/DependencyInjection.cs`. Swapping to Postgres/SQL Server is a one-line change: `options.UseInMemoryDatabase(...)` becomes `options.UseNpgsql(...)` / `options.UseSqlServer(...)`. Value objects are mapped via `OwnsOne` (for `Money` / `Email`) or `HasConversion` (for strongly-typed IDs and `BillingCycle`).

## Invariants (and the tests that prove them)

| Rule | Enforced in | Test |
| --- | --- | --- |
| Activating a subscription generates the first invoice | `Subscription.Activate` | `Activate_GeneratesFirstInvoice` |
| `SubscriptionActivated` raised on activation | `Subscription.Activate` | `Activate_RaisesSubscriptionActivatedEvent` |
| `InvoiceGenerated` raised on every issued invoice | `Invoice.Issue` | `Activate_FirstInvoice_RaisesInvoiceGeneratedEvent` |
| Each billing cycle generates a new invoice | `Subscription.GenerateNextInvoice` | `GenerateNextInvoice_WhenDue_IssuesInvoiceForNextPeriod` |
| Paying an invoice marks it Paid and raises `PaymentReceived` | `Invoice.Pay` | `Pay_MarksInvoiceAsPaid`, `Pay_RaisesPaymentReceivedEvent` |
| An invoice cannot be paid twice | `Invoice.Pay` | `Pay_Twice_Throws` |
| Canceling stops future invoices (history stays) | `Subscription.Cancel`, `GenerateNextInvoice` | `Cancel_PreventsFutureInvoices`, `Cancel_DoesNotVoidExistingInvoices` |
| Activating twice is an error | `Subscription.Activate` | `Activate_Twice_Throws` |
| Canceling twice is an error | `Subscription.Cancel` | `Cancel_Twice_Throws` |

## API

| Method | Route | Command/Query |
| --- | --- | --- |
| POST | `/customers` | `CreateCustomerCommand` |
| POST | `/subscriptions` | `CreateSubscriptionCommand` (creates + activates) |
| POST | `/subscriptions/{id}/cancel` | `CancelSubscriptionCommand` |
| POST | `/invoices/{id}/pay` | `PayInvoiceCommand` |
| GET | `/customers/{customerId}/invoices` | `GetInvoicesQuery` |

All command endpoints accept an `Idempotency-Key` header. Domain-rule violations return HTTP 409 with a ProblemDetails body; validation failures return HTTP 400.

## What's intentionally omitted

These are obvious next steps, left out to keep the sample readable:

* **Outbox dispatcher**: the outbox *table* and writes are in place; a dispatcher hosted service polling unprocessed rows and publishing via `IPublisher` is a small addition.
* **Relational DB migrations**: InMemory provider doesn't need them. Swap provider and add `dotnet ef migrations`.
* **Optimistic concurrency tokens**: add `[Timestamp]` or `IsRowVersion()` on each aggregate when moving off InMemory.
* **API integration tests**: the `Program` class is marked `partial` ready for `WebApplicationFactory`.
* **AuthN/AuthZ**: assumed handled by the hosting gateway.

## Conventions

* `Directory.Build.props` sets `TargetFramework=net8.0`, `Nullable=enable`, `TreatWarningsAsErrors=true` across all projects.
* Records used for DTOs and domain events; classes for aggregates and value objects (value objects use a custom base for explicit equality control).
* Factories on aggregates (`Customer.Create`, `Subscription.Create`, `Invoice.Issue`) keep construction invariants co-located with the type.
