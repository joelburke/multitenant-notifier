# Architecture — Multi-Tenant Notification Platform

Pre-rendered PDFs of every diagram are in [docs/](docs/). The Mermaid source files (`.mmd`) are alongside them if you want to regenerate.

| Diagram | PDF | Description |
|---|---|---|
| Project Dependency Flow | [01-project-dependency-flow.pdf](docs/01-project-dependency-flow.pdf) | How the five projects depend on each other |
| Domain Layer | [02-domain-layer.pdf](docs/02-domain-layer.pdf) | Entities, enums, and domain exceptions |
| Application Layer | [03-application-layer.pdf](docs/03-application-layer.pdf) | Interfaces, services, and DTOs |
| Infrastructure Layer | [04-infrastructure-layer.pdf](docs/04-infrastructure-layer.pdf) | EF Core, repositories, dispatchers, rate limiter |
| API Layer | [05-api-layer.pdf](docs/05-api-layer.pdf) | Controllers, routes, and exception middleware |
| Event Ingestion Flow | [06-event-ingestion-flow.pdf](docs/06-event-ingestion-flow.pdf) | End-to-end sequence for `POST /api/events` |

---

## Project Dependency Flow

The solution follows Clean Architecture. Arrows indicate "depends on."

```mermaid
graph LR
    Api["NotificationPlatform.Api\n(Controllers, Middleware, DI wiring)"]
    Application["NotificationPlatform.Application\n(Services, Interfaces, DTOs)"]
    Infrastructure["NotificationPlatform.Infrastructure\n(EF Core, Repositories, Dispatchers, Rate Limiter)"]
    Domain["NotificationPlatform.Domain\n(Entities, Enums, Exceptions)"]
    Frontend["frontend/\n(React + TypeScript + Vite)"]

    Api --> Application
    Api --> Infrastructure
    Infrastructure --> Application
    Application --> Domain
    Infrastructure --> Domain
    Frontend -->|HTTP / REST| Api
```

---

## Domain Layer

Zero external dependencies. Pure entities, enums, and domain exceptions.

```mermaid
classDiagram
    class Tenant {
        +Guid Id
        +string Name
        +string Slug
        +string ConnectionString
        +int RateLimitPerMinute
        +bool IsActive
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +Create(name, slug, rateLimit, connStr) Tenant$
        +Update(name, rateLimit)
        +Deactivate()
        +Activate()
    }
    class RoutingRule {
        +Guid Id
        +Guid TenantId
        +string Name
        +string EventTypePattern
        +EventTypeMatchMode MatchMode
        +string ChannelsJson
        +int Priority
        +bool IsActive
        +Create(...) RoutingRule$
        +Update(...)
        +Matches(eventType) bool
    }
    class NotificationLog {
        +Guid Id
        +Guid TenantId
        +Guid? RuleId
        +string EventType
        +string ChannelType
        +DispatchStatus Status
        +string? ErrorMessage
        +string PayloadJson
        +DateTime CreatedAt
        +Create(...) NotificationLog$
    }
    class EventTypeMatchMode {
        <<enumeration>>
        Exact
        Prefix
        Contains
    }
    class DispatchStatus {
        <<enumeration>>
        Sent
        Failed
        RateLimited
    }
    class TenantNotFoundException { <<exception>> }
    class RoutingRuleNotFoundException { <<exception>> }
    class RateLimitExceededException { <<exception>> }
    class DuplicateTenantSlugException { <<exception>> }

    RoutingRule --> EventTypeMatchMode
    NotificationLog --> DispatchStatus
```

_`Tenant` has no navigation properties to `RoutingRule` or `NotificationLog` — those entities live in a physically separate per-tenant database._

---

## Application Layer

Defines the contracts (interfaces) and orchestrates business logic (services). No infrastructure dependencies.

```mermaid
classDiagram
    class ITenantRepository {
        <<interface>>
        +GetAllAsync() Task~IReadOnlyList~
        +GetByIdAsync(Guid) Task~Tenant?~
        +GetBySlugAsync(string) Task~Tenant?~
        +SlugExistsAsync(string) Task~bool~
        +AddAsync(Tenant) Task
        +DeleteAsync(Tenant) Task
        +SaveChangesAsync() Task
    }
    class IRoutingRuleRepository {
        <<interface>>
        +GetByTenantAsync(Guid) Task~IReadOnlyList~
        +GetByIdAndTenantAsync(Guid, Guid) Task~RoutingRule?~
        +AddAsync(RoutingRule) Task
        +UpdateAsync(RoutingRule) Task
        +DeleteAsync(RoutingRule) Task
    }
    class INotificationLogRepository {
        <<interface>>
        +GetByTenantAsync(Guid, page, size) Task~IReadOnlyList~
        +AddAsync(NotificationLog) Task
    }
    class IDatabaseProvisioner {
        <<interface>>
        +ProvisionAsync(slug) Task~string~
        +DeprovisionAsync(connStr) Task
    }
    class INotificationDispatcher {
        <<interface>>
        +string ChannelType
        +DispatchAsync(DispatchRequestDto, ct) Task~DispatchResultDto~
    }
    class IDispatcherRegistry {
        <<interface>>
        +Resolve(channelType) INotificationDispatcher?
        +RegisteredChannelTypes IReadOnlyList~string~
    }
    class IRateLimiter {
        <<interface>>
        +TryConsume(tenantId, limitPerMinute) bool
    }
    class TenantService {
        -ITenantRepository
        -IDatabaseProvisioner
        +GetAllAsync()
        +GetByIdAsync(id)
        +CreateAsync(request)
        +UpdateAsync(id, request)
        +DeleteAsync(id)
    }
    class RoutingRuleService {
        -IRoutingRuleRepository
        -ITenantRepository
        +GetByTenantAsync(tenantId)
        +GetByIdAsync(id, tenantId)
        +CreateAsync(tenantId, request)
        +UpdateAsync(id, tenantId, request)
        +DeleteAsync(id, tenantId)
    }
    class EventIngestionService {
        -ITenantRepository
        -IRoutingRuleRepository
        -INotificationLogRepository
        -IDispatcherRegistry
        -IRateLimiter
        +IngestAsync(request)
        +GetLogsAsync(tenantId, ...)
    }

    TenantService --> ITenantRepository
    TenantService --> IDatabaseProvisioner
    RoutingRuleService --> IRoutingRuleRepository
    RoutingRuleService --> ITenantRepository
    EventIngestionService --> ITenantRepository
    EventIngestionService --> IRoutingRuleRepository
    EventIngestionService --> INotificationLogRepository
    EventIngestionService --> IDispatcherRegistry
    EventIngestionService --> IRateLimiter
    IDispatcherRegistry --> INotificationDispatcher
```

---

## Infrastructure Layer

Implements the Application interfaces. Split into two EF contexts: `CatalogDbContext` (static connection) and `AppDbContext` (per-tenant connection resolved at runtime).

```mermaid
classDiagram
    class CatalogDbContext {
        +DbSet~Tenant~ Tenants
        #OnModelCreating(ModelBuilder)
    }
    class AppDbContext {
        +DbSet~RoutingRule~ RoutingRules
        +DbSet~NotificationLog~ NotificationLogs
        #OnModelCreating(ModelBuilder)
    }
    class AppDbContextFactory {
        <<IDesignTimeDbContextFactory>>
        +CreateDbContext(args) AppDbContext
    }
    class TenantDbContextFactory {
        -CatalogDbContext _catalog
        -IMemoryCache _cache
        +CreateAsync(tenantId) Task~AppDbContext~
        +InvalidateCache(tenantId)
    }
    class DatabaseProvisioner {
        -IConfiguration _config
        +ProvisionAsync(slug) Task~string~
        +DeprovisionAsync(connStr) Task
    }
    class TenantMigrationRunner {
        <<IHostedService>>
        +StartAsync(ct) Task
    }
    class TenantRepository {
        -CatalogDbContext _catalog
    }
    class RoutingRuleRepository {
        -TenantDbContextFactory _factory
    }
    class NotificationLogRepository {
        -TenantDbContextFactory _factory
    }
    class LogDispatcher {
        +ChannelType = "log"
        +DispatchAsync(request, ct)
    }
    class WebhookDispatcher {
        +ChannelType = "webhook"
        +DispatchAsync(request, ct)
    }
    class DispatcherRegistry {
        -Dictionary~string,INotificationDispatcher~ _map
        +Resolve(channelType)
    }
    class SlidingWindowRateLimiter {
        -ConcurrentDictionary~Guid,Queue~DateTime~~ _windows
        +TryConsume(tenantId, limit)
    }

    TenantRepository ..|> ITenantRepository : implements
    RoutingRuleRepository ..|> IRoutingRuleRepository : implements
    NotificationLogRepository ..|> INotificationLogRepository : implements
    DatabaseProvisioner ..|> IDatabaseProvisioner : implements
    LogDispatcher ..|> INotificationDispatcher : implements
    WebhookDispatcher ..|> INotificationDispatcher : implements
    DispatcherRegistry ..|> IDispatcherRegistry : implements
    SlidingWindowRateLimiter ..|> IRateLimiter : implements

    TenantRepository --> CatalogDbContext
    TenantDbContextFactory --> CatalogDbContext
    RoutingRuleRepository --> TenantDbContextFactory
    NotificationLogRepository --> TenantDbContextFactory
    TenantDbContextFactory ..> AppDbContext : creates
    TenantMigrationRunner --> CatalogDbContext
    TenantMigrationRunner ..> AppDbContext : migrates per tenant
    DispatcherRegistry --> INotificationDispatcher
```

---

## API Layer

ASP.NET Core controllers and middleware. Depends on Application services directly; Infrastructure is wired via DI in `Program.cs`.

```mermaid
classDiagram
    class TenantsController {
        -TenantService
        +GET    api/tenants
        +GET    api/tenants/:id
        +POST   api/tenants
        +PUT    api/tenants/:id
        +DELETE api/tenants/:id
    }
    class RoutingRulesController {
        -RoutingRuleService
        +GET    api/tenants/:tenantId/rules
        +GET    api/tenants/:tenantId/rules/:id
        +POST   api/tenants/:tenantId/rules
        +PUT    api/tenants/:tenantId/rules/:id
        +DELETE api/tenants/:tenantId/rules/:id
    }
    class EventsController {
        -EventIngestionService
        +POST api/events
    }
    class NotificationLogsController {
        -EventIngestionService
        +GET api/tenants/:tenantId/logs
    }
    class ExceptionMiddleware {
        +InvokeAsync(HttpContext)
    }

    TenantsController --> TenantService
    RoutingRulesController --> RoutingRuleService
    EventsController --> EventIngestionService
    NotificationLogsController --> EventIngestionService
    ExceptionMiddleware ..> TenantNotFoundException : 404
    ExceptionMiddleware ..> RoutingRuleNotFoundException : 404
    ExceptionMiddleware ..> RateLimitExceededException : 429
    ExceptionMiddleware ..> DuplicateTenantSlugException : 409
```

---

## Event Ingestion Request Flow

End-to-end flow for `POST /api/events`. The catalog is hit once per request (5-min cached); the tenant DB handles rules and log writes.

```mermaid
sequenceDiagram
    participant Client
    participant ExceptionMiddleware
    participant EventsController
    participant EventIngestionService
    participant CatalogDB as Catalog DB
    participant TenantDB as Tenant DB
    participant IRateLimiter
    participant IDispatcherRegistry

    Client->>ExceptionMiddleware: POST /api/events {tenantId, eventType, payload}
    ExceptionMiddleware->>EventsController: pass through
    EventsController->>EventIngestionService: IngestAsync(request)

    EventIngestionService->>CatalogDB: GetByIdAsync(tenantId)
    alt Tenant not found
        CatalogDB-->>EventIngestionService: null
        EventIngestionService-->>ExceptionMiddleware: throw TenantNotFoundException
        ExceptionMiddleware-->>Client: 404 Not Found
    end

    EventIngestionService->>IRateLimiter: TryConsume(tenantId, limit)
    alt Rate limit exceeded
        IRateLimiter-->>EventIngestionService: false
        EventIngestionService->>TenantDB: AddAsync(log Status=RateLimited)
        EventIngestionService-->>ExceptionMiddleware: throw RateLimitExceededException
        ExceptionMiddleware-->>Client: 429 Too Many Requests (Retry-After 60)
    else Within limit
        IRateLimiter-->>EventIngestionService: true
        EventIngestionService->>TenantDB: GetByTenantAsync(tenantId) rules
        loop For each active matching rule and channel
            EventIngestionService->>IDispatcherRegistry: Resolve(channelType)
            IDispatcherRegistry-->>EventIngestionService: INotificationDispatcher
            EventIngestionService->>IDispatcherRegistry: DispatchAsync(request)
            IDispatcherRegistry-->>EventIngestionService: DispatchResultDto
            EventIngestionService->>TenantDB: AddAsync(log Status=Sent or Failed)
        end
        EventIngestionService-->>EventsController: IngestEventResponseDto
        EventsController-->>Client: 200 OK
    end
```
