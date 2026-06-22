# EnterpriseLogger SaaS

Multi-tenant enterprise logging platform backend built with **.NET 8**, **Clean Architecture**, **PostgreSQL**, and **Docker**.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)](https://www.postgresql.org/)
[![License](https://img.shields.io/badge/license-Private-red)]()

---

## Overview

**EnterpriseLoggerSaaS** lets organizations (tenants) send application logs (Info, Warning, Error) to a centralized API using an **API key**, without running their own ELK/Graylog stack.

| Goal | Approach |
|------|----------|
| Tenant isolation | Shared database, row-level `TenantId` + EF Core global query filters |
| Machine auth | `X-Api-Key` header (server-generated at tenant registration) |
| Secure configuration | Secrets in `.env` (local) / environment variables (production) |
| Maintainable features | Vertical slices under `Application/Features/` |
| Quality | xUnit unit + integration tests, conventional commits |

---

## Architecture

Dependencies point **inward**. Domain has no infrastructure references.

```
┌─────────────────────────────────────────────────────────┐
│  EnterpriseLogger.Api          (HTTP, Swagger, DI root) │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│  EnterpriseLogger.Infrastructure  (EF Core, PostgreSQL) │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│  EnterpriseLogger.Application   (use cases, validation) │
└───────────────────────────┬─────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────┐
│  EnterpriseLogger.Domain          (entities, rules)     │
└─────────────────────────────────────────────────────────┘
```

| Layer | Responsibility |
|-------|----------------|
| **Domain** | `Tenant`, `SystemLog`, `Package`, `TenantSubscription` |
| **Application** | `IApplicationDbContext`, `Result<T>`, FluentValidation, tenant registration |
| **Infrastructure** | `ApplicationDbContext`, EF migrations, design-time factory |
| **Api** | REST controllers, middleware, global exception handler, composition root (`Program.cs`) |

---

## Tech stack

| Area | Choice |
|------|--------|
| Runtime | .NET 8 |
| API | ASP.NET Core Web API + Swagger (Development only) |
| Database | PostgreSQL 16 (Docker) |
| ORM | Entity Framework Core 8 (code-first) |
| Validation | FluentValidation 12 |
| Cache | Redis 7 — tenant-scoped `POST /api/logs` rate limiting |
| Tests | xUnit, Moq, `WebApplicationFactory` integration tests |
| Local secrets | DotNetEnv + `.env` |

---

## Repository layout

```
EnterpriseLoggerSaaS/
├── .env.example              # Template for local secrets (copy → .env)
├── docker-compose.yml        # PostgreSQL + Redis
├── EnterpriseLogger.sln
├── EnterpriseLogger.Domain/
├── EnterpriseLogger.Application/
│   ├── Common/Interfaces/    # IApplicationDbContext, ICurrentTenantProvider
│   ├── Common/Models/        # Result<T>
│   └── Features/             # Tenants, Logs (commands & queries)
├── EnterpriseLogger.Infrastructure/
│   ├── Persistence/          # DbContext, migrations, query filters
│   └── RateLimiting/         # Redis + in-memory rate limiter
├── EnterpriseLogger.Api/
│   ├── Controllers/          # TenantsController, LogsController
│   ├── Middleware/           # LogIngestRateLimitMiddleware
│   └── Infrastructure/       # GlobalExceptionHandler, ApiProblemDetails
├── EnterpriseLogger.Application.Tests/
├── EnterpriseLogger.Api.IntegrationTests/
└── apps/
    ├── tenant-panel/         # React SPA — tenant users (Vite, port 5173)
    └── platform-admin/       # React SPA — platform operators (Vite, port 5174)
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Git

Optional: [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

```bash
dotnet tool install --global dotnet-ef
```

---

## Quick start

### 1. Clone and configure secrets

```bash
git clone https://github.com/Zeymurat/EnterpriseLoggerSaaS.git
cd EnterpriseLoggerSaaS
cp .env.example .env
```

Edit `.env` for **local** development (example):

```env
POSTGRES_USER=saas_admin
POSTGRES_PASSWORD=<your-password>
POSTGRES_DB=EnterpriseLoggerDb
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=EnterpriseLoggerDb;Username=saas_admin;Password=<your-password>
REDIS_CONNECTION_STRING=localhost:6379
LOG_INGEST_RATE_LIMIT_PER_MINUTE=1000
LOG_INGEST_RATE_LIMIT_WINDOW_SECONDS=60
```

> `.env` is gitignored. Never commit passwords.

### 2. Start infrastructure

```bash
docker compose up -d
```

### 3. Apply database migrations

```bash
dotnet ef database update \
  --project EnterpriseLogger.Infrastructure \
  --startup-project EnterpriseLogger.Api
```

### 4. Run the API

```bash
dotnet run --project EnterpriseLogger.Api
```

Swagger UI (Development): **http://localhost:5247/swagger**

### 5. Run tenant panel (optional)

```bash
cd apps/tenant-panel
cp .env.example .env
npm install
npm run dev
```

Panel: **http://localhost:5173** — requires API running and `CORS_ALLOWED_ORIGINS` including `http://localhost:5173` in root `.env`.

### 6. Run platform admin (optional)

```bash
cd apps/platform-admin
cp .env.example .env
npm install
npm run dev
```

Platform admin: **http://localhost:5174** — separate login (`POST /api/platform/auth/login`). On first Development startup, set `PLATFORM_ADMIN_EMAIL` and `PLATFORM_ADMIN_PASSWORD` in root `.env`; the API seeds the first platform admin if the table is empty.

### 7. Run tests

```bash
dotnet test
```

---

## API (current)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/tenants` | — | Register a new tenant |
| `POST` | `/api/tenants/me/api-key/rotate` | Bearer JWT (Root, `apikeys:rotate`) | Generate or rotate tenant API key (shown once) |
| `POST` | `/api/auth/login` | — | Panel login (email + password → JWT) |
| `POST` | `/api/platform/auth/login` | — | Platform admin login (separate JWT, no `tenantId`) |
| `GET` | `/api/platform/tenants` | Bearer JWT (`platform_admin=true`) | List all tenants with user/log counts |
| `GET` | `/api/platform/tenants/{id}` | Bearer JWT (`platform_admin=true`) | Tenant detail + subscription history |
| `POST` | `/api/platform/tenants/{id}/subscription` | Bearer JWT (`platform_admin=true`) | Assign or change tenant package (closes previous subscription) |
| `GET` | `/api/platform/packages` | Bearer JWT (`platform_admin=true`) | List subscription packages |
| `PUT` | `/api/platform/packages/{id}` | Bearer JWT (`platform_admin=true`) | Update package quotas and pricing |
| `POST` | `/api/auth/refresh` | Bearer JWT | Extend panel session (new access token, same `session_started_at` claim) |
| `GET` | `/api/users` | Bearer JWT | List tenant users (`users:read`) |
| `POST` | `/api/users/invite` | Bearer JWT | Invite Admin or User (`users:invite`) |
| `PATCH` | `/api/users/{id}/permissions` | Bearer JWT | Update User role permissions (`users:manage`) |
| `PATCH` | `/api/users/{id}/role` | Bearer JWT (Root) | Promote User → Admin |
| `PATCH` | `/api/users/{id}/deactivate` | Bearer JWT | Deactivate user (`users:manage`) |
| `POST` | `/api/logs` | `X-Api-Key` **or** `Bearer JWT` | Ingest a log entry (`Info`, `Warning`, `Error`). Tenant rate limit applies — `429` when exceeded |
| `GET` | `/api/logs` | `X-Api-Key` **or** `Bearer JWT` | List logs for the authenticated tenant (paginated; query: `page`, `pageSize`, `logLevel`, `search`, `from`, `to`, `applicationName`) |

### Register tenant

**Request body**

```json
{
  "name": "Acme Corp",
  "ownerEmail": "owner@acme.com",
  "ownerPhone": "+905551234567",
  "ownerPassword": "SecurePass123!"
}
```

**Success:** `200 OK` with `Result<TenantResponseDto>` — tenant metadata only (`id`, `name`, `ownerEmail`, etc.). **No API key in the response.** A **Root** user is created for the tenant owner. After login, generate the API key from the tenant panel (`POST /api/tenants/me/api-key/rotate`, Root + `apikeys:rotate`).

### Panel login (JWT)

**Request body**

```json
{
  "email": "owner@acme.com",
  "password": "SecurePass123!"
}
```

**Success:** `200 OK` with `accessToken`, `expiresIn`, and `user` (role, permissions). Use Swagger **Authorize → Bearer** to paste the token.

### Panel session model (idle + refresh)

The tenant panel uses **three independent timers** — do not confuse them:

| Layer | Config | Default | Meaning |
|-------|--------|---------|---------|
| **Access token (JWT)** | `JWT_ACCESS_TOKEN_EXPIRY_MINUTES` (API `.env`) | 60 min | Crypto lifetime of each token. Renewed on login and on **refresh**. |
| **Absolute max session** | `JWT_MAX_SESSION_HOURS` (API `.env`) | 8 h | Time since **first login** (`session_started_at` claim). After this, `/api/auth/refresh` returns 401 — user must log in again. |
| **Idle timeout (UX)** | `VITE_SESSION_IDLE_MINUTES` (panel `.env`) | 15 min | No mouse/keyboard/scroll/touch and no successful authenticated API call → warning, then logout. |
| **Idle warning** | `VITE_SESSION_WARN_MINUTES` (panel `.env`) | 2 min | How long before idle limit the *"Oturumu uzat"* dialog appears. |

**Idle is not “15 minutes since login”.** Live log polling (`GET /api/logs` every 30s), page navigation, and filters all count as activity and reset the idle clock.

**“Oturumu uzat”** calls `POST /api/auth/refresh` → new JWT (another `JWT_ACCESS_TOKEN_EXPIRY_MINUTES` window) while preserving `session_started_at` until `JWT_MAX_SESSION_HOURS` is reached.

**401 handling:** expired or invalid JWT on any authenticated request → panel clears session and redirects to `/login?session=expired`.

If the same email exists in multiple tenants and `tenantName` is omitted, the API returns **400 Bad Request** with `errorCode: "AmbiguousTenantContext"` and `tenantOptions` (tenant name + role per match). The tenant panel shows a picker and retries login with the selected `tenantName`:

```json
{
  "data": null,
  "isSuccess": false,
  "errorCode": "AmbiguousTenantContext",
  "errorMessage": "Multiple tenants match this email. Please specify tenantName.",
  "tenantOptions": [
    { "tenantName": "zeymurat", "role": "Root", "displayName": null },
    { "tenantName": "Zeymurat-Global", "role": "User", "displayName": null }
  ]
}
```

Retry with an explicit tenant:

```json
{
  "email": "consultant@shared.com",
  "password": "SecurePass123!",
  "tenantName": "Acme Corp"
}
```

### Log ingestion & query (dual-channel auth)

| Channel | Header | Who | Permission check |
|---------|--------|-----|------------------|
| **Machine** | `X-Api-Key` | Customer app / backend | None (tenant scope only) |
| **Human (panel)** | `Authorization: Bearer <JWT>` | Root / Admin / User | `logs:read` (GET), `logs:write` (POST) |

Send the tenant API key for machine integration, or a JWT from login for the admin panel. In Swagger, use **Authorize** for either `X-Api-Key` or `Bearer`.

**Rate limiting:** `POST /api/logs` is limited per tenant via Redis (fixed window). Default: **1000 requests / 60 seconds** (`LOG_INGEST_RATE_LIMIT_PER_MINUTE`, `LOG_INGEST_RATE_LIMIT_WINDOW_SECONDS`). `POST /api/auth/login` and `POST /api/tenants` are limited per client IP (defaults: **20** and **5** requests / 60 seconds). **Login lockout:** after repeated failed attempts per email or IP, accounts are locked (default **10** email / **30** IP failures → **15 min** lockout) with exponential backoff from the 3rd failure. Exceeding limits returns `429 Too Many Requests` with RFC 7807 Problem Details and a `Retry-After` header. `GET /api/logs` is not rate limited.

### User management (JWT only)

**Invite user** (`POST /api/users/invite`) — response includes `temporaryPassword` (shown once; no email server yet).

```json
{
  "email": "dev@acme.com",
  "phone": "05551234567",
  "role": "User",
  "permissions": ["logs:read"]
}
```

| Rule | Detail |
|------|--------|
| Root | Full tenant control; only Root can invite Admin or change roles |
| Admin | Can invite Users, manage User permissions, deactivate Users |
| User | Custom permissions via `UserPermissions` table |
| Root | Cannot be deactivated or have role changed |
| Inactive user | Can be re-invited with same email (new `temporaryPassword`, reactivated) |
| Duplicate email/phone | `409 Conflict` within the same tenant (active users) |

**Permission changes:** After `PATCH /api/users/{id}/permissions`, the affected user must **log in again** to receive a JWT with updated permissions (existing tokens keep old claims until expiry).

**Create log example**

Required fields: `applicationName`, `logLevel`, `message`. Optional request context (sent by the customer app):

```json
{
  "applicationName": "BillingService",
  "logLevel": "Error",
  "message": "Payment provider timeout after 30s",
  "httpMethod": "POST",
  "requestPath": "/api/checkout",
  "statusCode": 504,
  "correlationId": "req_8f2a1b",
  "actorIdentifier": "customer@acme.com",
  "exceptionType": "TimeoutException"
}
```

Context fields are optional — existing integrations that send only the three required fields keep working.

**GET /api/logs** returns a paginated envelope. Each item includes context fields when present.

| Query | Description |
|-------|-------------|
| `page` | Page number (default `1`) |
| `pageSize` | Items per page (default `25`, max `100`) |
| `logLevels` | `Info`, `Warning`, `Error` (repeat or multi-value) |
| `search` | Case-insensitive match in **message** |
| `from` / `to` | UTC timestamp range (inclusive). Supports full ISO datetimes for live windows (e.g. last 10 minutes) |
| `applicationNames` | One or more exact application names |
| `httpMethods` | One or more HTTP methods (e.g. `GET`, `POST`) |
| `statusCodes` | One or more HTTP status codes (e.g. `200`, `404`) |
| `correlationId` | Exact match on request trace / correlation id |

**GET /api/logs/export** returns a UTF-8 CSV of matching logs (same query params as list, without pagination). Up to **10,000** rows per export; response headers `X-Export-Count`, `X-Export-Total-Matching`, `X-Export-Truncated` indicate how many rows were exported.

```json
{
  "data": {
    "items": [
      {
        "id": 42,
        "applicationName": "BillingService",
        "logLevel": "Error",
        "message": "Payment provider timeout after 30s",
        "timestamp": "2026-06-04T14:30:00Z",
        "httpMethod": "POST",
        "requestPath": "/api/checkout",
        "statusCode": 504
      }
    ],
    "totalCount": 250,
    "page": 1,
    "pageSize": 25,
    "summary": { "total": 250, "info": 116, "warning": 95, "error": 39 },
    "overallSummary": { "total": 250, "info": 116, "warning": 95, "error": 39 },
    "isDateFiltered": true
  },
  "isSuccess": true,
  "errorMessage": null
}
```

In the tenant panel:
- **Logs** page supports live time presets (10m / 30m / 1h / 3h / 12h), date presets, CSV export, and auto-refresh on live presets.
- **Dashboard** uses the same time range selector and summary cards for the selected window.
- Click a log row to open the **log detail sheet** (request type, URL, status, actor, correlation id, exception type when available).
- **Empty & error UX:** new tenants with zero logs see an onboarding card (API key + integration docs). Filtered searches with no matches show a clear empty table state. API/network failures surface a reusable error card with retry — network outages map to a friendly *"Sunucuya ulaşılamadı"* message from the shared `apiFetch` layer (no Axios interceptors).
- **Correlation trace:** log detail sheet → *İlişkili istekleri filtrele* opens `/logs?correlationId=…` and lists the full request chain (`GET /api/logs?correlationId=` exact match).

### Error responses

| Situation | Format |
|-----------|--------|
| FluentValidation / business rule failure | `400 Bad Request` — `Result<T>` with `isSuccess: false` |
| Missing or invalid API key | `401 Unauthorized` — RFC 7807 Problem Details |
| Inactive tenant | `403 Forbidden` — RFC 7807 Problem Details |
| Log ingestion rate limit exceeded | `429 Too Many Requests` — RFC 7807 Problem Details + `Retry-After` header |
| Unhandled server error | `500 Internal Server Error` — RFC 7807 Problem Details |

In **Production**, Problem Details responses do **not** include stack traces or `exceptionDetail` (CWE-209 safe). Full exception detail is only attached when `IsDevelopment()` is true.

---

## Configuration

| Source | When |
|--------|------|
| `.env` (solution root) | Local Development (`DotNetEnv` loads via `EnvFileLoader`) |
| Environment variables | Production / CI (`DB_CONNECTION_STRING`, `JWT_SECRET`, etc.) |
| `appsettings.json` | Non-secret defaults only (connection string empty) |

**JWT variables** (see `.env.example`): `JWT_SECRET` (min 32 chars), `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_ACCESS_TOKEN_EXPIRY_MINUTES`, `JWT_MAX_SESSION_HOURS`.

**Redis & rate limiting** (see `.env.example`): `REDIS_CONNECTION_STRING`; log ingestion (`LOG_INGEST_RATE_LIMIT_*`, per tenant on `POST /api/logs`); public endpoints (`AUTH_LOGIN_RATE_LIMIT_*`, `TENANT_REGISTER_RATE_LIMIT_*`, per IP); login lockout (`LOGIN_MAX_FAILED_ATTEMPTS_EMAIL`, `LOGIN_MAX_FAILED_ATTEMPTS_IP`, `LOGIN_LOCKOUT_MINUTES`, `LOGIN_BACKOFF_START_AFTER`, etc.). Set `TRUST_FORWARDED_HEADERS=true` only behind a trusted reverse proxy — otherwise clients can spoof `X-Forwarded-For` to bypass IP limits. Integration tests use in-memory implementations (no Redis in CI).

**CORS** (tenant panel): `CORS_ALLOWED_ORIGINS` — comma-separated origins; default `http://localhost:5173`.

**Frontend** (`apps/tenant-panel/.env`): `VITE_API_URL`, `VITE_SESSION_IDLE_MINUTES`, `VITE_SESSION_WARN_MINUTES` (see `apps/tenant-panel/.env.example`).

---

## Development workflow

1. Create a feature branch from `main`:

   ```bash
   git checkout main
   git pull
   git checkout -b feature/your-feature-name
   ```

2. Commit with [Conventional Commits](https://www.conventionalcommits.org/):

   ```text
   feat(tenant): add tenant registration endpoint
   ```

3. Open a **Pull Request** into `main`. GitHub Actions runs **CI** automatically (`dotnet test` + tenant-panel `npm run build`). Merge after checks pass.

`main` is the stable branch. Solo development may commit directly to `main`; PRs remain optional but useful as a CI checkpoint and history marker.

---

## Roadmap

- [x] Global exception handling (RFC 7807 Problem Details)
- [x] `POST /api/logs` with tenant-scoped ingestion
- [x] `GET /api/logs` with tenant-scoped query
- [x] API key authentication (`X-Api-Key` via authentication handler)
- [x] Integration tests (`WebApplicationFactory`)
- [x] Global query filters for multi-tenant isolation
- [x] JWT login endpoint (`POST /api/auth/login`)
- [x] Dual-auth pipeline (JWT + ApiKey on log endpoints)
- [x] Tenant user management (invite, permissions, role, deactivate)
- [x] CORS for frontend clients
- [x] Tenant panel skeleton (React + Vite + Tailwind)
- [x] Logs & users UI in tenant panel
- [x] Log request context fields + log detail sheet (tenant panel)
- [x] Paginated log listing with filters, live time presets, CSV export (tenant panel)
- [x] Empty state & error UX (onboarding card, API error card, network fallback)
- [x] Idle session warning + JWT refresh (`POST /api/auth/refresh`)
- [x] Redis for rate limits / quotas
- [x] GitHub Actions CI (`build` + `test`)

---

## Contributing

This repository is currently **private**. For internal work: branch → PR → merge to `main`.

---

## Author

**Zeymurat** — Enterprise SaaS learning & production-oriented backend practice.
