# Platform Admin

React SPA for platform operators (billing, tenants, packages, payments).

## Stack

- Vite + React 18 + TypeScript
- Tailwind CSS + shadcn/ui-style components
- React Router, TanStack Query

## Local development

```bash
# From repo root — API must be running on :5247
cd apps/platform-admin
cp .env.example .env
npm install
npm run dev
```

Open **http://localhost:5174**

Copy root `.env.example` → `.env` for API/JWT/platform admin bootstrap settings.

## Environment

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_API_URL` | `http://localhost:5247` | Backend API base URL |
| `VITE_TENANT_PANEL_URL` | `http://localhost:5173` | Login-as redirect target |
| `VITE_SESSION_IDLE_MINUTES` | `15` | Idle timeout before warning |
| `VITE_SESSION_WARN_MINUTES` | `2` | Minutes before idle limit to show extend dialog |

Ensure API `CORS_ALLOWED_ORIGINS` includes `http://localhost:5174` (see root `.env.example`).

## Pages

| Route | Description |
|-------|-------------|
| `/dashboard` | KPIs, high-quota tenants |
| `/customers` | Tenant list, filters, CSV export |
| `/packages` | Package CRUD |
| `/payments` | Havale/EFT payment workflow |
| `/renewals` | Pending / upcoming subscriptions |
| `/audit-logs` | Platform admin action history |
