# Notification Platform — Admin UI

React + TypeScript + Vite admin interface for the Multi-Tenant Notification Platform.

## Pages

| Page | Route | Purpose |
|---|---|---|
| Tenants | `/` | Create, list, and delete tenants |
| Routing Rules | `/rules` | Create and delete per-tenant routing rules |
| Ingest Event | `/ingest` | Send a test event and see dispatch results |
| Notification Logs | `/logs` | Browse per-tenant notification history |

## Running

**With Docker (recommended):**
```bash
docker compose up
# UI available at http://localhost:3000
```

**Standalone dev server:**
```bash
npm install
npm run dev
# Proxies /api/* to http://localhost:5000
```

**Build for production:**
```bash
npm run build
# Output in dist/ — served by nginx in Docker
```

## Stack

- React 19 + TypeScript
- Vite 8 (dev server + bundler)
- Plain fetch API — no extra HTTP client libraries
- Plain CSS — no UI framework

## Proxy config

In dev mode (`npm run dev`), Vite proxies `/api/*` to `http://localhost:5000` so the frontend and API can run on different ports without CORS issues. In production (Docker), nginx handles the proxy directly — see [nginx.conf](nginx.conf).
