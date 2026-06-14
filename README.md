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
└── EnterpriseLogger.Api.IntegrationTests/
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

### 5. Run tests

```bash
dotnet test
```

---

## API (current)

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/tenants` | — | Register a new tenant |
| `POST` | `/api/auth/login` | — | Panel login (email + password → JWT) |
| `POST` | `/api/logs` | `X-Api-Key` | Ingest a log entry (`Info`, `Warning`, `Error`) |
| `GET` | `/api/logs` | `X-Api-Key` | List logs for the authenticated tenant |

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

**Success:** `200 OK` with `Result<TenantResponseDto>` — `apiKey` is **generated by the server** (save it; shown once at registration). A **Root** user is created for the tenant owner. Phone is stored in E.164 format (e.g. `+905551234567`). The same email can belong to different tenants (`TenantId` + `Email` is unique, not email alone).

### Panel login (JWT)

**Request body**

```json
{
  "email": "owner@acme.com",
  "password": "SecurePass123!"
}
```

If the same email exists in multiple tenants, include `tenantName`:

```json
{
  "email": "consultant@shared.com",
  "password": "SecurePass123!",
  "tenantName": "Acme Corp"
}
```

**Success:** `200 OK` with `accessToken`, `expiresIn`, and `user` (role, permissions). Use Swagger **Authorize → Bearer** to paste the token. Log endpoints still use `X-Api-Key` until dual-auth (PR-6).

### Log ingestion & query

Send the tenant API key on every log request. In Swagger, click **Authorize** and paste the key into `X-Api-Key`.

**Create log example**

```json
{
  "message": "Payment processed",
  "logLevel": "Info",
  "source": "BillingService"
}
```

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

3. Open a **Pull Request** into `main`, run `dotnet test`, merge after review.

`main` is the stable branch; avoid committing directly when working in a team setting.

---

## Roadmap

- [x] Global exception handling (RFC 7807 Problem Details)
- [x] `POST /api/logs` with tenant-scoped ingestion
- [x] `GET /api/logs` with tenant-scoped query
- [x] API key authentication middleware (`X-Api-Key`)
- [x] Integration tests (`WebApplicationFactory`)
- [x] Global query filters for multi-tenant isolation
- [x] JWT login endpoint (`POST /api/auth/login`)
- [ ] Dual-auth pipeline (JWT + ApiKey on log endpoints)
- [ ] Tenant user management (invite, permissions)
- [ ] CORS for frontend clients
- [ ] Redis for rate limits / quotas
- [ ] GitHub Actions CI (`build` + `test`)

---

## Contributing

This repository is currently **private**. For internal work: branch → PR → merge to `main`.

---

## Author

**Zeymurat** — Enterprise SaaS learning & production-oriented backend practice.
