# CLAUDE.md — Multi-Tenant Notification Platform

## Project Overview

A multi-tenant notification platform built in .NET 10 + React. Ingests events from tenant organizations, routes them through configurable rules, and dispatches to notification channels.

## Architecture

**Clean Architecture** with four layers:
- `NotificationPlatform.Domain` — entities and exceptions, zero dependencies
- `NotificationPlatform.Application` — interfaces, DTOs, use-case services; depends on Domain only
- `NotificationPlatform.Infrastructure` — EF Core, repositories, dispatchers, rate limiter; implements Application interfaces
- `NotificationPlatform.Api` — ASP.NET Core controllers, middleware, DI wiring
- `NotificationPlatform.Tests` — xUnit tests (unit + integration)
- `frontend/` — React + TypeScript + Vite admin UI

**Key principles:** SOLID, Clean Code, no magic strings, all isolation enforced at the repository layer.

## How to run

```bash
docker compose up          # full stack
dotnet test                # 25 tests
```

## Regenerating architecture diagrams

Diagrams live in `ARCHITECTURE.md` (Mermaid source) and `docs/` (pre-rendered PDFs + `.mmd` source files).

**Prerequisites:** Node.js, `npx` available, and Google Chrome installed at `C:/Program Files/Google/Chrome/Application/chrome.exe`.

**Steps:**

1. Edit the `.mmd` files in `docs/` (or update the matching blocks in `ARCHITECTURE.md` and copy the changes across).

2. Write a temporary Puppeteer config pointing at the system Chrome (avoids downloading a headless shell):

   ```bash
   cat > /tmp/puppeteer.json << 'EOF'
   {
     "executablePath": "C:/Program Files/Google/Chrome/Application/chrome.exe",
     "args": ["--no-sandbox"]
   }
   EOF
   ```

3. Render all diagrams to PDF:

   ```bash
   for f in docs/*.mmd; do
     npx @mermaid-js/mermaid-cli -i "$f" -o "${f%.mmd}.pdf" -p /tmp/puppeteer.json --pdfFit
   done
   ```

   To render a single diagram: `npx @mermaid-js/mermaid-cli -i docs/01-project-dependency-flow.mmd -o docs/01-project-dependency-flow.pdf -p /tmp/puppeteer.json --pdfFit`

**Files:**

| File | Purpose |
|---|---|
| `ARCHITECTURE.md` | Canonical source — Mermaid diagrams rendered inline on GitHub |
| `docs/*.mmd` | Individual diagram source files (one per diagram) |
| `docs/*.pdf` | Pre-rendered PDFs for offline/interview use |

## Key design decisions

- **Isolation**: Shared database, `TenantId` column on all tables, all repo queries filter by it
- **Rate limiting**: In-memory sliding window, per-tenant `ConcurrentDictionary<Guid, Queue<DateTime>>`
- **Dispatchers**: `INotificationDispatcher` interface; `DispatcherRegistry` resolves by `ChannelType` string; adding a channel = new class + one DI registration
- **Rules**: `EventTypePattern` + `MatchMode` (Exact/Prefix/Contains); channels stored as JSON column

## Original Requirements

---

### Take-Home Project: Multi-Tenant Notification Platform

**Time budget:** 2–4 hours  
**Suggested submission window:** 5–7 business days from receipt

#### Context

You'll be building a small but representative slice of a real product: a notification platform that ingests events from multiple customer organizations ("tenants"), applies their routing rules, and dispatches notifications on the appropriate channels.

The shape of the problem is familiar — what makes it interesting is that **every tenant must be safely isolated from every other tenant**, both in their data and in their ability to consume system resources.

This project is intentionally scoped so that completing it without AI assistance in the time budget would be very difficult. **You are expected to use AI tools** (Copilot, Claude, Cursor, etc.) as part of your normal workflow. We care that you understand, can defend, and can extend the code you submit — not that you typed every character yourself.

#### The System

You will build an end-to-end service with four moving parts:

1. **Ingestion** — an HTTP endpoint that accepts events tagged with a `tenant_id`
2. **Routing** — a rule engine that determines, per tenant, which channels (if any) should receive a notification for a given event
3. **Dispatch** — the mechanism that actually sends the notification on the matched channel(s)
4. **Admin API** — endpoints for managing tenants and routing rules

Authentication is **out of scope**. Tenants identify themselves by sending `tenant_id` in the request body.

#### Functional Requirements

**Event Ingestion**
- Expose an HTTP endpoint that accepts an event payload including `tenant_id`, an event type, and arbitrary additional fields.
- Validate the request and return appropriate error responses for malformed input or unknown tenants.
- Apply the requesting tenant's routing rules and dispatch notifications accordingly.

**Routing Rules**
- Routing rules are **stored in the database** and are **tenant-scoped** — a rule belongs to exactly one tenant and is only applied to that tenant's events.
- The rule model itself is your design decision. At minimum it must let a tenant say "for events matching X, dispatch to channel(s) Y."
- Be prepared to defend your choice in the design doc and the interview.

**Dispatch**
- Implement a clean dispatcher abstraction so that adding a new channel (email, Slack, webhook, SMS, etc.) does not require changes to the routing engine.
- The actual delivery mechanism is your call. The **abstraction** is what we'll evaluate.

**Tenant Isolation**
- Choose your isolation strategy (shared database with `tenant_id` column, schema-per-tenant, database-per-tenant, etc.) and defend it in the design doc.
- Whatever strategy you pick, **tenant A must not be able to read, modify, or affect tenant B's data or operations** under any code path exposed by your service.

**Rate Limiting**
- Each tenant has its own rate limit. Both the algorithm and the granularity are your design decisions.
- Tenant A exhausting their limit must not degrade tenant B's experience.
- Document your choices and trade-offs in the design doc.

**Admin API**
- CRUD endpoints for **tenants** (create, read, update, delete).
- CRUD endpoints for **routing rules**, scoped to a tenant.
- Standard error handling and validation.

#### Deliverables

1. **Runnable Service** — single command: `docker compose up`
2. **README** — setup instructions, curl examples, architectural overview, known limitations
3. **Design Document** — data model, isolation strategy, rate limiting, dispatcher abstraction
4. **Tests** (encouraged) — isolation and routing are the natural targets

#### What We Evaluate

- **Multi-tenancy thinking** — Does isolation hold under realistic failure modes?
- **API design** — Are the endpoints coherent, well-shaped, and well-validated?
- **Abstraction quality** — Is the dispatcher genuinely extensible?
- **Trade-off reasoning** — Can you articulate what you traded away?
- **Code you can defend** — Walk through it line by line.
