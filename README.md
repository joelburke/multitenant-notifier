# Multi-Tenant Notification Platform

A notification platform that ingests events from multiple customer organizations ("tenants"), applies per-tenant routing rules, and dispatches notifications on configured channels.

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- That's it — .NET and Node are only needed if you want to run locally without Docker

## Quick Start (single command)

```bash
docker compose up
```

- **Admin UI**: http://localhost:3000
- **API / Swagger**: http://localhost:5000/swagger

The API applies database migrations automatically on startup. Wait ~20 seconds for SQL Server to be ready on first run.

---

## Exercising the API

### 1 — Create a tenant

```bash
curl -s -X POST http://localhost:5000/api/tenants \
  -H "Content-Type: application/json" \
  -d '{"name":"Acme Corp","slug":"acme","rateLimitPerMinute":100}' | jq
```

Save the returned `id` as `$TENANT_ID`.

### 2 — Create a routing rule

```bash
curl -s -X POST http://localhost:5000/api/tenants/$TENANT_ID/rules \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Log all user events",
    "eventTypePattern": "user.",
    "matchMode": 1,
    "channels": [{"type":"log","settings":{}}],
    "priority": 0
  }' | jq
```

`matchMode`: `0` = Exact, `1` = Prefix, `2` = Contains

### 3 — Ingest an event

```bash
curl -s -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "tenantId": "'$TENANT_ID'",
    "eventType": "user.signup",
    "payload": {"email": "alice@example.com"}
  }' | jq
```

The response shows how many channels were dispatched. Check `docker compose logs api` to see the structured log notification.

### 4 — View notification logs

```bash
curl -s http://localhost:5000/api/tenants/$TENANT_ID/logs | jq
```

### 5 — Demonstrate tenant isolation

Create a second tenant, then show that its logs and rules are independent:

```bash
TENANT_B=$(curl -s -X POST http://localhost:5000/api/tenants \
  -H "Content-Type: application/json" \
  -d '{"name":"Globex","slug":"globex","rateLimitPerMinute":10}' | jq -r .id)

# Tenant B has no rules — ingesting an event dispatches nothing
curl -s -X POST http://localhost:5000/api/events \
  -H "Content-Type: application/json" \
  -d '{"tenantId":"'$TENANT_B'","eventType":"user.signup","payload":{}}' | jq

# Tenant B's logs are empty; Tenant A's logs are unaffected
curl -s http://localhost:5000/api/tenants/$TENANT_B/logs | jq
```

### 6 — Demonstrate rate limiting

```bash
# Exhaust a low-limit tenant (10/min)
for i in $(seq 1 11); do
  curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5000/api/events \
    -H "Content-Type: application/json" \
    -d '{"tenantId":"'$TENANT_B'","eventType":"test","payload":{}}'
done
# The 11th request returns 429
```

---

## Running locally (without Docker)

**Prerequisites:** .NET 10 SDK, SQL Server (or Docker for just the DB)

```bash
# Start just the database
docker compose up db

# Run migrations + API
cd src/NotificationPlatform.Api
dotnet run

# Run frontend dev server (in a separate terminal)
cd frontend
npm install && npm run dev
```

## Running tests

```bash
dotnet test
```

25 tests covering: routing rule matching, sliding-window rate limiter isolation, event ingestion service logic, and repository-level tenant isolation.

---

## Architecture Overview

See [DESIGN.md](DESIGN.md) for full architectural decisions and trade-offs.

**Stack:** React + TypeScript (Vite) · ASP.NET Core 10 · Entity Framework Core 9 · SQL Server 2022

**Layer structure (Clean Architecture):**

```
Domain       — entities, exceptions, no dependencies
Application  — interfaces, DTOs, use-case services; depends on Domain only
Infrastructure — EF, repositories, dispatchers, rate limiter; implements Application interfaces
Api          — controllers, middleware, DI wiring; depends on Application + Infrastructure
```

**Key design decisions:**

- **Isolation**: Database-per-tenant — each tenant gets its own SQL Server database provisioned at creation time. A catalog database (`NotificationPlatform_Catalog`) holds tenant metadata and connection strings. Tenant databases share no tables, transaction logs, or buffer pool entries.
- **Rate limiting**: In-memory sliding window, fully isolated per tenant
- **Dispatchers**: `INotificationDispatcher` interface; new channels require only a new class + one DI line

---

## Known Limitations

- Rate limiter state is in-memory — not suitable for multi-instance deployments (Redis needed)
- No authentication — `tenant_id` in the request body is not production-safe
- No webhook retry logic — failed POSTs are logged but not retried
- Rule conditions match on event type only — no payload field filtering
- Tenant database connection strings are stored in plaintext in the catalog DB — production would use a secrets store (Azure Key Vault, AWS Secrets Manager)

---

## AI Tool Usage

This project was built with Claude Code (Anthropic). Claude generated the majority of the boilerplate (project scaffolding, EF configurations, React pages, Dockerfiles) and test structure. I gave claude guidelines on architecture by feeding it your requirements documents and providing my own: a .net API, react front end, SQL with Entity Framework, good testing with docker files driven by SOLID and Clean code and architecture because that's important to me.

### Additional Notes

1. It proposed a lot of the rule model design, isolation strategy, rate limiting algorithms and I reviewed and liked what I saw.
1. Testing swagger resulted in 404's so I asked claude to review. It caught a mismatch in the dockercompose (env = prod) and the swagger middleware was originally only running in env = dev so I removed that and got it working.
1. I prompted claude to create a vscode.workplace file for easy reuse. It created a `/root` folder duplicating a lot of the other projects which I could take or leave.
1. Because I requested a clean architecture and clean code impementation, the solution was a little more complex than I'd like and the `desisgn.md` left some to be desired. I requested claude add at least one architectural diagram in `design.md` to at least explain each project, the purposoe of it, what is contained in it, the dependency flow and the classes. I also used it to correct a mermaid syntax error it made when rendering them on github.com. It is now much simpler to understand.
1. Requested provide a recommendation to refactor so tenants would have system resources isolated. Created a branch to apply tenant-per-db refactors including updates to readme's
2. I noticed the application was vulnerable to sql injection from the UI because of how claude designed the db creation scripts, I pointed it out and had it refactor to sql parameterization
1. Debugged and resolved an issue with databases shutting down and starting up multiple times a min, resolved by setting auto_close ON after db creation to keep db warm

## Given More Time

I would

- [ ] Refactor 1m sliding timer constant to a configuration and have both the front end and back end utilize it. There were multiple places depending on magic strings
- [ ] Review Project dependecies to determine if any dependencies shuold be broken with absractions.
- [ ] Refactor controller layer to depend on service interfaces instead of concrete services
- [x] Refactor some of the more complex services to be more human readable
- [ ] Make the UI prettier
- [ ] Add React and typescript automated tests
- [ ] See if I could simplify the code and design to only what's needed for the requirements, it's relatively complex I think due to my requirements of clean code/architecture as it's important not to over architect.
- [ ] Remove credentials like SQL creds from plaintext `appsettings.json` files and ideally put in a credentials store or something, or at least get them out of being committed in plaintext to git and put them in env files gitignored for docker.
- [ ] Enhance the error messages displayed to the user in the admin UI to share what went wrong and the action the user needs to take to resolve.
- [x] Update `design.md` to have some architectural diagrams. At least a visual representation of each project, their dependency flow, and an overview of the classes in each project is importan
- [x] Update `claude.md` to ensure it more accurately represents the initial requirements and the design choices I've made.
- [x] Run some CURLs in for loops to test rate limiting
- [ ] Consider refactoring Channels in the db, currently they are stored as JSON (nvarchar(max)) rather than a separate table which seems difficult to maintain
- [x] Refactored to database-per-tenant for true resource isolation — each tenant's queries, locks, and buffer pool usage are physically isolated. A long-running query from Tenant A cannot block Tenant B.
