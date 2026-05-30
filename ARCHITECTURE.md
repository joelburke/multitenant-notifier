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
        +int RateLimitPerMinute
        +bool IsActive
        +DateTimeOffset CreatedAt
        +DateTimeOffset UpdatedAt
        +ICollection~RoutingRule~ Rules
        +ICollection~NotificationLog~ Logs
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
        +Tenant Tenant
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
        +DateTimeOffset CreatedAt
        +Tenant Tenant
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
    class TenantNotFoundException {
        <<exception>>
    }
    class RoutingRuleNotFoundException {
        <<exception>>
    }
    class RateLimitExceededException {
        <<exception>>
    }
    class DuplicateTenantSlugException {
        <<exception>>
    }

    Tenant "1" --> "*" RoutingRule
    Tenant "1" --> "*" NotificationLog
    RoutingRule --> EventTypeMatchMode
    NotificationLog --> DispatchStatus
```

---

## Application Layer

Defines the contracts (interfaces) and orchestrates business logic (services). No infrastructure dependencies.

```mermaid
classDiagram
    class ITenantRepository {
        <<interface>>
        +GetAllAsync() Task~IEnumerable~
        +GetByIdAsync(Guid) Task~Tenant~
        +GetBySlugAsync(string) Task~Tenant~
        +SlugExistsAsync(string) Task~bool~
        +AddAsync(Tenant) Task
        +DeleteAsync(Tenant) Task
        +SaveChangesAsync() Task
    }
    class IRoutingRuleRepository {
        <<interface>>
        +GetByTenantAsync(Guid) Task~IEnumerable~
        +GetByIdAndTenantAsync(Guid, Guid) Task~RoutingRule~
        +AddAsync(RoutingRule) Task
        +DeleteAsync(RoutingRule) Task
        +SaveChangesAsync() Task
    }
    class INotificationLogRepository {
        <<interface>>
        +GetByTenantAsync(Guid, ...) Task~IEnumerable~
        +AddAsync(NotificationLog) Task
        +SaveChangesAsync() Task
    }
    class INotificationDispatcher {
        <<interface>>
        +string ChannelType
        +DispatchAsync(DispatchRequest, CancellationToken) Task~DispatchResult~
    }
    class IDispatcherRegistry {
        <<interface>>
        +Resolve(string channelType) INotificationDispatcher
        +RegisteredChannelTypes IReadOnlyList~string~
    }
    class IRateLimiter {
        <<interface>>
        +TryConsume(Guid tenantId, int limitPerMinute) bool
    }
    class TenantService {
        -ITenantRepository _repo
        +GetAllAsync()
        +GetByIdAsync(id)
        +CreateAsync(CreateTenantRequest)
        +UpdateAsync(id, UpdateTenantRequest)
        +DeleteAsync(id)
    }
    class RoutingRuleService {
        -IRoutingRuleRepository _ruleRepo
        -ITenantRepository _tenantRepo
        +GetByTenantAsync(tenantId)
        +GetByIdAsync(ruleId, tenantId)
        +CreateAsync(tenantId, request)
        +UpdateAsync(tenantId, ruleId, request)
        +DeleteAsync(tenantId, ruleId)
    }
    class EventIngestionService {
        -ITenantRepository _tenantRepo
        -IRoutingRuleRepository _ruleRepo
        -INotificationLogRepository _logRepo
        -IDispatcherRegistry _registry
        -IRateLimiter _rateLimiter
        +IngestAsync(IngestEventRequest)
        +GetLogsAsync(tenantId, ...)
    }
    class ChannelConfig {
        <<record>>
        +string Type
        +Dictionary~string,string~ Settings
    }
    class DispatchRequest {
        <<record>>
        +Guid TenantId
        +Guid RuleId
        +string EventType
        +Dictionary~string,object~ Payload
        +ChannelConfig Channel
    }
    class DispatchResult {
        <<record>>
        +bool Success
        +string? ErrorMessage
    }

    TenantService --> ITenantRepository
    RoutingRuleService --> IRoutingRuleRepository
    RoutingRuleService --> ITenantRepository
    EventIngestionService --> ITenantRepository
    EventIngestionService --> IRoutingRuleRepository
    EventIngestionService --> INotificationLogRepository
    EventIngestionService --> IDispatcherRegistry
    EventIngestionService --> IRateLimiter
    IDispatcherRegistry --> INotificationDispatcher
    INotificationDispatcher --> DispatchRequest
    INotificationDispatcher --> DispatchResult
    DispatchRequest --> ChannelConfig
```

---

## Infrastructure Layer

Implements the Application interfaces. All EF Core, HTTP, and in-memory concerns live here.

```mermaid
classDiagram
    class AppDbContext {
        +DbSet~Tenant~ Tenants
        +DbSet~RoutingRule~ RoutingRules
        +DbSet~NotificationLog~ NotificationLogs
        #OnModelCreating(ModelBuilder)
    }
    class TenantConfiguration {
        <<IEntityTypeConfiguration>>
    }
    class RoutingRuleConfiguration {
        <<IEntityTypeConfiguration>>
    }
    class NotificationLogConfiguration {
        <<IEntityTypeConfiguration>>
    }
    class TenantRepository {
        -AppDbContext _db
    }
    class RoutingRuleRepository {
        -AppDbContext _db
    }
    class NotificationLogRepository {
        -AppDbContext _db
    }
    class LogDispatcher {
        -ILogger _logger
        +ChannelType = "log"
        +DispatchAsync(request, ct)
    }
    class WebhookDispatcher {
        -IHttpClientFactory _httpClientFactory
        -ILogger _logger
        +ChannelType = "webhook"
        +DispatchAsync(request, ct)
    }
    class DispatcherRegistry {
        -Dictionary~string,INotificationDispatcher~ _map
        +Resolve(channelType)
        +RegisteredChannelTypes
    }
    class SlidingWindowRateLimiter {
        -ConcurrentDictionary~Guid,Queue~DateTime~~ _windows
        +TryConsume(tenantId, limitPerMinute)
    }

    TenantRepository ..|> ITenantRepository : implements
    RoutingRuleRepository ..|> IRoutingRuleRepository : implements
    NotificationLogRepository ..|> INotificationLogRepository : implements
    LogDispatcher ..|> INotificationDispatcher : implements
    WebhookDispatcher ..|> INotificationDispatcher : implements
    DispatcherRegistry ..|> IDispatcherRegistry : implements
    SlidingWindowRateLimiter ..|> IRateLimiter : implements

    TenantRepository --> AppDbContext
    RoutingRuleRepository --> AppDbContext
    NotificationLogRepository --> AppDbContext
    AppDbContext --> TenantConfiguration
    AppDbContext --> RoutingRuleConfiguration
    AppDbContext --> NotificationLogConfiguration
    DispatcherRegistry --> INotificationDispatcher
```

---

## API Layer

ASP.NET Core controllers and middleware. Depends on Application services directly; Infrastructure is wired via DI in `Program.cs`.

```mermaid
classDiagram
    class TenantsController {
        -TenantService _tenantService
        +GET    api/tenants
        +GET    api/tenants/:id
        +POST   api/tenants
        +PUT    api/tenants/:id
        +DELETE api/tenants/:id
    }
    class RoutingRulesController {
        -RoutingRuleService _ruleService
        +GET    api/tenants/:tenantId/rules
        +GET    api/tenants/:tenantId/rules/:ruleId
        +POST   api/tenants/:tenantId/rules
        +PUT    api/tenants/:tenantId/rules/:ruleId
        +DELETE api/tenants/:tenantId/rules/:ruleId
    }
    class EventsController {
        -EventIngestionService _ingestionService
        +POST api/events
    }
    class NotificationLogsController {
        -EventIngestionService _ingestionService
        +GET api/tenants/:tenantId/logs
    }
    class ExceptionMiddleware {
        -RequestDelegate _next
        -ILogger _logger
        +InvokeAsync(HttpContext)
    }

    TenantsController --> TenantService
    RoutingRulesController --> RoutingRuleService
    EventsController --> EventIngestionService
    NotificationLogsController --> EventIngestionService
    ExceptionMiddleware ..> TenantNotFoundException : maps to 404
    ExceptionMiddleware ..> RoutingRuleNotFoundException : maps to 404
    ExceptionMiddleware ..> RateLimitExceededException : maps to 429
    ExceptionMiddleware ..> DuplicateTenantSlugException : maps to 409
```

---

## Event Ingestion Request Flow

End-to-end flow for `POST /api/events`.

```mermaid
sequenceDiagram
    participant Client
    participant ExceptionMiddleware
    participant EventsController
    participant EventIngestionService
    participant ITenantRepository
    participant IRateLimiter
    participant IRoutingRuleRepository
    participant IDispatcherRegistry
    participant INotificationDispatcher
    participant INotificationLogRepository

    Client->>ExceptionMiddleware: POST /api/events (tenantId, eventType, payload)
    ExceptionMiddleware->>EventsController: pass through
    EventsController->>EventIngestionService: IngestAsync(request)
    EventIngestionService->>ITenantRepository: GetByIdAsync(tenantId)
    alt Tenant not found
        ITenantRepository-->>EventIngestionService: null
        EventIngestionService-->>ExceptionMiddleware: throw TenantNotFoundException
        ExceptionMiddleware-->>Client: 404 Not Found
    end
    EventIngestionService->>IRateLimiter: TryConsume(tenantId, limit)
    alt Rate limit exceeded
        IRateLimiter-->>EventIngestionService: false
        EventIngestionService->>INotificationLogRepository: AddAsync(Status=RateLimited)
        EventIngestionService-->>ExceptionMiddleware: throw RateLimitExceededException
        ExceptionMiddleware-->>Client: 429 Too Many Requests (Retry-After 60)
    else Within limit
        IRateLimiter-->>EventIngestionService: true
        EventIngestionService->>IRoutingRuleRepository: GetByTenantAsync(tenantId)
        loop For each active rule matching eventType
            loop For each channel in rule
                EventIngestionService->>IDispatcherRegistry: Resolve(channelType)
                IDispatcherRegistry-->>EventIngestionService: INotificationDispatcher
                EventIngestionService->>INotificationDispatcher: DispatchAsync(DispatchRequest)
                INotificationDispatcher-->>EventIngestionService: DispatchResult
                EventIngestionService->>INotificationLogRepository: AddAsync(Status=Sent or Failed)
            end
        end
        EventIngestionService-->>EventsController: IngestEventResponse
        EventsController-->>Client: 200 OK
    end
```
