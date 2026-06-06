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
| Tenant isolation | Shared database, row-level `TenantId` (+ planned global query filters) |
| Secure configuration | Secrets in `.env` (local) / environment variables (production) |
| Maintainable features | Vertical slices under `Application/Features/` |
| Quality | xUnit unit tests + conventional commits |

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
| **Api** | REST controllers, composition root (`Program.cs`) |

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
| Tests | xUnit, Moq |
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
│   ├── Common/Interfaces/    # IApplicationDbContext
│   ├── Common/Models/        # Result<T>
│   └── Features/Tenants/     # Create tenant slice
├── EnterpriseLogger.Infrastructure/
│   └── Persistence/          # DbContext, migrations
├── EnterpriseLogger.Api/
│   └── Controllers/          # TenantsController
└── EnterpriseLogger.Application.Tests/
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

| Method | Route | Description |
|--------|-------|-------------|
| `POST` | `/api/tenants` | Register a new tenant |

**Request body**

```json
{
  "name": "Acme Corp",
  "apiKey": "EL_SECRET_KEY_123456"
}
```

**Success:** `200 OK` with `Result<TenantResponseDto>`  
**Validation / business rule failure:** `400 Bad Request` with error message

---

## Configuration

| Source | When |
|--------|------|
| `.env` (solution root) | Local Development (`DotNetEnv` loads via `EnvFileLoader`) |
| Environment variables | Production / CI (`DB_CONNECTION_STRING`, etc.) |
| `appsettings.json` | Non-secret defaults only (connection string empty) |

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

- [ ] Global exception handling (RFC 7807 Problem Details)
- [ ] `POST /api/logs` with tenant-scoped ingestion
- [ ] API key authentication middleware
- [ ] Redis for rate limits / quotas
- [ ] Integration tests (`WebApplicationFactory`)
- [ ] GitHub Actions CI (`build` + `test`)
- [ ] Global query filters for multi-tenant isolation

---

## Contributing

This repository is currently **private**. For internal work: branch → PR → merge to `main`.

---

## Author

**Zeymurat** — Enterprise SaaS learning & production-oriented backend practice.
