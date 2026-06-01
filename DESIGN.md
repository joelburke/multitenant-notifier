# Design Document — Multi-Tenant Notification Platform

## Architecture Diagrams

See [ARCHITECTURE.md](ARCHITECTURE.md) for full diagrams with Mermaid source, and [docs/](docs/) for pre-rendered PDFs.

---

## Data Model

### Databases

**Catalog DB** (`NotificationPlatform_Catalog`) — always-on shared database managed by `CatalogDbContext`.

**Tenant DBs** (`NotificationPlatform_{slug}`) — one per tenant, provisioned at creation time, managed by `AppDbContext` with a per-request connection string.

### Entities

**Tenant** _(Catalog DB)_

- `Id` (Guid PK), `Name`, `Slug` (unique, lowercase), `RateLimitPerMinute`, `IsActive`, `CreatedAt`, `UpdatedAt`
- `ConnectionString` — the connection string to this tenant's isolated database. Stored in the catalog only; never exposed through the API.

**RoutingRule** _(Tenant DB)_

- `Id`, `TenantId` (plain Guid, no FK — Tenant lives in a different database), `Name`, `EventTypePattern`, `MatchMode` (enum), `ChannelsJson` (JSON column), `Priority`, `IsActive`, `CreatedAt`, `UpdatedAt`
- Channels are stored as JSON (`nvarchar(max)`) rather than a separate table. This avoids a join on every event ingestion and lets the channel schema evolve (new channel types, new settings fields) without schema migrations. The trade-off is that you can't query `WHERE channel.type = 'webhook'` without JSON path syntax — acceptable for this use case since channels are always loaded with the rule.
   - Note: Claude designed this and I feel it's pretty risky to store as JSON just from a maintainability standpoint.

**NotificationLog** _(Tenant DB)_

- `Id`, `TenantId` (plain Guid, no FK), `RuleId` (nullable Guid), `EventType`, `ChannelType`, `Status` (Sent/Failed/RateLimited), `ErrorMessage`, `PayloadJson`, `CreatedAt`
- Append-only audit log. `TenantId` is stored as a plain column (not a FK) for observability — the database itself is the tenant boundary.

### Where Tenant Scoping Lives

The **database is the tenant boundary**. Each tenant's `AppDbContext` points at a separate SQL Server database that contains only that tenant's rows. There is no cross-tenant WHERE clause needed — a bug that omits a filter cannot read another tenant's data because the data does not exist in the connected database.

`TenantId` columns are retained on `RoutingRule` and `NotificationLog` as a denormalization for observability (useful in logs, traces, and potential future cross-DB analytics).

---

## Isolation Strategy

**Chosen: Database-per-tenant**

Each tenant gets its own SQL Server database, provisioned at tenant-creation time by `DatabaseProvisioner`. A shared catalog database (`NotificationPlatform_Catalog`) stores tenant metadata including each tenant's connection string. At request time, `TenantDbContextFactory` resolves the connection string from the catalog (cached for 5 minutes in `IMemoryCache`) and creates an `AppDbContext` pointed at the correct database.

**What I considered:**

| Strategy | Data isolation | Resource isolation | Ops complexity |
|---|---|---|---|
| Shared DB + `TenantId` column | Code-enforced, filterable | Weak — shared locks, buffer pool, I/O | Low |
| Schema-per-tenant | Schema-enforced | Weak — same engine process | Medium |
| **Database-per-tenant** ✓ | Physical separation | Strong — separate files, logs, buffer pool | Higher |

**Why database-per-tenant wins:**

The requirement explicitly calls out resource isolation ("every tenant must be safely isolated from every other tenant, both in their data and in their ability to consume system resources"). A shared database satisfies data isolation through query filtering, but a long-running query from Tenant A can lock pages, pollute the buffer pool, and exhaust connections for Tenant B.

With database-per-tenant:
- A query from Tenant A only takes locks on `NotificationPlatform_acme` — Tenant B's database is untouched
- Buffer pool pages are per-database — heavy reads from one tenant do not evict another tenant's cached pages
- SQL Server Resource Governor can be layered on to cap CPU/memory per database if hard SLA guarantees are needed
- Offboarding a tenant is a single `DROP DATABASE` — no need to delete rows across shared tables

**Trade-offs accepted:**
- Cross-tenant reporting requires querying N databases or an ETL pipeline into a warehouse
- Connection pool pressure grows with tenant count (`Min Pool Size=0` mitigates idle connections)
- The catalog DB becomes a single point of failure for tenant resolution — it needs HA in production (Always On AG or Azure SQL)
- `DatabaseProvisioner` requires the SA login to have `dbcreator` rights — in production this would be a dedicated provisioning principal with scoped permissions

---

## Rate Limiting

**Algorithm: Sliding Window (per tenant, global)**

Each tenant gets their own independent window. The `SlidingWindowRateLimiter` maintains a `Queue<DateTime>` of request timestamps per tenant in a `ConcurrentDictionary`. On each request:

1. Evict timestamps older than 60 seconds.
2. If `queue.Count >= limit` → reject (return `false`).
3. Otherwise enqueue now and allow.

**Why sliding window over fixed window?**  
Fixed window has a "burst at boundary" problem: a tenant can fire 2× their limit in a short window by loading the tail of one window and the head of the next. Sliding window prevents that. Token bucket would also work and is slightly more forgiving of burst traffic, but sliding window is simpler to reason about and audit.

**Granularity: global per tenant**  
The limit applies to all events from a tenant regardless of event type or channel. Per-event-type or per-channel granularity would give tenants more flexibility but would require a more complex key scheme and makes the limit less predictable to reason about. Given this is a 2–4hr project, global-per-tenant is the right call.

**Where state is stored:** In-memory (`ConcurrentDictionary`). The window is accurate within a single process.

**Trade-off documented:** In-memory state is not shared across instances. In a horizontally-scaled deployment, each instance maintains its own window, meaning a tenant could exceed their limit by a factor equal to the replica count. Fixing this requires a distributed counter (Redis `INCR` with expiry, or an atomic sliding window in Redis Lua). I've documented this in the README's Known Limitations section.

**What happens at the limit:** The ingestion endpoint returns `429 Too Many Requests` with a `Retry-After: 60` header. The rejection is logged as a `NotificationLog` with `Status = RateLimited` so tenants can observe their own throttling.

---

## Dispatcher Abstraction

### Interface

```csharp
public interface INotificationDispatcher
{
    string ChannelType { get; }
    Task<DispatchResult> DispatchAsync(DispatchRequest request, CancellationToken ct = default);
}
```

`DispatchRequest` carries `TenantId`, `RuleId`, `EventType`, `Payload`, and the `ChannelConfig` (type + settings dictionary). The dispatcher receives everything it needs to render and send the notification — it has no dependency on the domain layer.

### How a new channel is added

1. Implement `INotificationDispatcher` in `Infrastructure/Dispatchers/`.
2. Register it in `DependencyInjection.cs`: `services.AddScoped<INotificationDispatcher, MyNewDispatcher>()`.

That's it. The `DispatcherRegistry` discovers all registered `INotificationDispatcher` implementations automatically via `IEnumerable<INotificationDispatcher>` injection and builds a type→dispatcher map. The routing engine (`EventIngestionService`) calls `_registry.Resolve(channelType)` and is completely unaware of what dispatchers exist.

### Current implementations

| Channel Type | Behavior                                                                          |
| ------------ | --------------------------------------------------------------------------------- |
| `log`        | Structured log line via `ILogger` — visible in Docker logs and any log aggregator |
| `webhook`    | HTTP POST to a configured URL; supports custom headers in `Settings`              |

### Adding email (example)

```csharp
public class EmailDispatcher(ISmtpClient smtp) : INotificationDispatcher
{
    public string ChannelType => "email";
    public async Task<DispatchResult> DispatchAsync(DispatchRequest request, CancellationToken ct)
    {
        var to = request.Channel.Settings["to"];
        // ... send email
        return DispatchResult.Ok();
    }
}
```

Register in DI. No other files change.

---

## Known Limitations & Next Steps

- **Rate limiter is in-memory**: distributed deployments need Redis.
- **No authentication**: `tenant_id` in the request body is not production-safe.
- **Webhook retries**: currently no retry logic on failed POSTs — an exponential backoff queue (Hangfire, background worker) would be needed.
- **Rule conditions**: the current model supports event type matching only. A payload field condition engine (e.g. `payload.severity == "critical"`) would make rules much more powerful.
- **Soft delete vs. hard delete for tenants**: currently a hard delete cascades everything. A soft-delete approach with data retention period would be more production-appropriate.
