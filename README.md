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
| Cache (infra ready) | Redis 7 in `docker-compose` (app integration planned) |
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
│   └── Persistence/          # DbContext, migrations, query filters
├── EnterpriseLogger.Api/
│   ├── Controllers/          # TenantsController, LogsController
│   ├── Middleware/           # TenantMappingMiddleware (API key)
│   └── Infrastructure/       # GlobalExceptionHandler, ApiProblemDetails
├── EnterpriseLogger.Application.Tests/
├── EnterpriseLogger.Api.IntegrationTests/
└── apps/
    └── tenant-panel/         # React SPA (Vite + Tailwind + shadcn-style UI)
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

Panel: **http://localhost:5173** — requires API running and `CORS_ALLOWED_ORIGINS=http://localhost:5173` in root `.env`.

### 6. Run tests

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
| `GET` | `/api/users` | Bearer JWT | List tenant users (`users:read`) |
| `POST` | `/api/users/invite` | Bearer JWT | Invite Admin or User (`users:invite`) |
| `PATCH` | `/api/users/{id}/permissions` | Bearer JWT | Update User role permissions (`users:manage`) |
| `PATCH` | `/api/users/{id}/role` | Bearer JWT (Root) | Promote User → Admin |
| `PATCH` | `/api/users/{id}/deactivate` | Bearer JWT | Deactivate user (`users:manage`) |
| `POST` | `/api/logs` | `X-Api-Key` **or** `Bearer JWT` | Ingest a log entry (`Info`, `Warning`, `Error`) |
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

### Error responses

| Situation | Format |
|-----------|--------|
| FluentValidation / business rule failure | `400 Bad Request` — `Result<T>` with `isSuccess: false` |
| Missing or invalid API key | `401 Unauthorized` — RFC 7807 Problem Details |
| Inactive tenant | `403 Forbidden` — RFC 7807 Problem Details |
| Unhandled server error | `500 Internal Server Error` — RFC 7807 Problem Details |

In **Production**, Problem Details responses do **not** include stack traces or `exceptionDetail` (CWE-209 safe). Full exception detail is only attached when `IsDevelopment()` is true.

---

## Configuration

| Source | When |
|--------|------|
| `.env` (solution root) | Local Development (`DotNetEnv` loads via `EnvFileLoader`) |
| Environment variables | Production / CI (`DB_CONNECTION_STRING`, `JWT_SECRET`, etc.) |
| `appsettings.json` | Non-secret defaults only (connection string empty) |

**JWT variables** (see `.env.example`): `JWT_SECRET` (min 32 chars), `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_ACCESS_TOKEN_EXPIRY_MINUTES`.

**CORS** (tenant panel): `CORS_ALLOWED_ORIGINS` — comma-separated origins; default `http://localhost:5173`.

**Frontend** (`apps/tenant-panel/.env`): `VITE_API_URL=http://localhost:5247`

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
- [ ] Redis for rate limits / quotas
- [x] GitHub Actions CI (`build` + `test`)

---

## Contributing

This repository is currently **private**. For internal work: branch → PR → merge to `main`.

---

## Author

**Zeymurat** — Enterprise SaaS learning & production-oriented backend practice.
