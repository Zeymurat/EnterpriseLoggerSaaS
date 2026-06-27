# EnterpriseLogger SaaS

Multi-tenant enterprise logging platform — .NET 8 backend, PostgreSQL, Redis, iki React paneli.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D)](https://redis.io/)
[![React](https://img.shields.io/badge/React-18-61DAFB)](https://react.dev/)

---

## İçindekiler

1. [Proje nedir?](#proje-nedir)
2. [Neden yapıldı?](#neden-yapıldı)
3. [Teknik kazanımlar](#teknik-kazanımlar)
4. [Performans](#performans)
5. [Güvenlik](#güvenlik)
6. [Teknoloji yığını](#teknoloji-yığını)
7. [Mimari](#mimari)
8. [Hızlı başlangıç — Docker](#hızlı-başlangıç--docker)
9. [Geliştirici kurulumu (isteğe bağlı)](#geliştirici-kurulumu-isteğe-bağlı)
10. [Uygulama adresleri](#uygulama-adresleri)
11. [CI/CD](#cicd)

---

## Proje nedir?

**EnterpriseLogger SaaS**, kurumların uygulama loglarını (Info, Warning, Error) merkezi bir API'ye göndermesini sağlayan **çok kiracılı (multi-tenant)** bir SaaS platformudur. Her müşteri (tenant) kendi veritabanı satırlarında izole edilir; makine entegrasyonu **API key**, panel erişimi **JWT** ile yapılır.

Platform operatörleri için ayrı bir **Platform Admin** arayüzü; tenant yöneticileri için **Tenant Panel** vardır. Manuel havale/EFT billing modeli, paket kotası, abonelik yenileme ve denetim kaydı desteklenir.

---

## Neden yapıldı?

Bu proje üç amaçla geliştirildi:

1. **CV / portfolyo** — Gerçek bir SaaS ürün iskeleti: multi-tenant, billing, güvenlik, test, CI, Docker.
2. **.NET öğrenimi** — Django deneyiminden .NET ekosistemine geçiş: Clean Architecture, EF Core, DI, middleware, background services.
3. **Üretim kalitesi pratiği** — Rate limit, audit log, integration test, Problem Details, migration otomasyonu.

---

## Teknik kazanımlar

### Distributed Multi-Tenant Architecture

Kiracı verileri PostgreSQL'de **EF Core Global Query Filter** ile satır düzeyinde izole edildi. `TenantId` çözülmeden log sorgusu sıfır satır döner; platform admin sorguları kontrollü `IgnoreQueryFilters()` kullanır. API key hash'lenerek saklanır; tenant başına tek Root kullanıcı kuralı veritabanı indeksi ile zorlanır.

**Elde edilen:** Veri sızıntısı riskinin minimize edildiği, paylaşımlı veritabanında tenant izolasyonu.

### High-Throughput Rate Limiting & Quota Management

Redis tabanlı **dakikalık burst** (`MaxLogsPerMinute`, paket bazlı) ve **aylık toplam log kotası** (`MonthlyRequestLimit`, `CreateLogCommand` içinde) motoru kurgulandı. Her tenant kendi paket kotasına tabidir.

**Elde edilen:** Adil kaynak paylaşımı, noisy-neighbor önleme, `429` + `Retry-After` ile öngörülebilir limit davranışı.

### Katmanlı Güvenlik & SecOps

Brute-force ve kimlik avına karşı **e-posta + IP tabanlı account lockout**, **exponential backoff**, **IP-scoped public endpoint rate limit** (login, kayıt), **X-Forwarded-For** spoofing koruması (`TRUST_FORWARDED_HEADERS` yalnızca güvenilir proxy arkasında). JWT oturum tavanı (`JWT_MAX_SESSION_HOURS`), ayrı platform admin JWT'si, RBAC permission kodları.

**Elde edilen:** Auth katmanında savunma derinliği; production'da Problem Details ile stack trace sızıntısı yok.

### Automated Billing Engine

Banka havalesi / EFT modeline uygun: **7 günlük grace period**, otomatik dönem yenileme (`SubscriptionRenewalHostedService`), ödeme yapılmazsa **Free pakete düşürme**, platform admin ödeme onay/red akışı, tenant billing overview sayfası.

**Elde edilen:** Manuel faturalama operasyonları için uçtan uca billing yaşam döngüsü.

### Observability & Operations

Platform **dashboard** (KPI), **renewals** görünümü, **audit log** (tüm platform admin mutasyonları), log **retention** job (paket `StorageRetentionDays`), isteğe bağlı **SMTP bildirimleri** (ödeme, grace, kota uyarısı).

**Elde edilen:** Operasyonel görünürlük ve denetlenebilirlik.

### Enterprise-Grade Testing

**xUnit** + **WebApplicationFactory** ile **110+** unit ve integration test; GitHub Actions CI (`dotnet test`, her iki frontend `npm run build`).

**Elde edilen:** Regresyon koruması, merge öncesi otomatik doğrulama.

---

## Performans

| Konu | Yaklaşım |
|------|----------|
| Log ingest | Redis fixed-window rate limit; paket `MaxLogsPerMinute` |
| Sorgular | EF Core indeksler (`TenantId`, `LogLevel`, `Timestamp`, `CorrelationId`) |
| Liste API'leri | Sunucu tarafı sayfalama, filtreler, CSV export |
| Log retention | Günlük batch delete; paket saklama süresine göre |
| Frontend | Vite production build, nginx gzip, TanStack Query cache |
| Benchmark | `scripts/benchmark/` — ingest/load senaryoları |

---

## Güvenlik

| Katman | Detay |
|--------|--------|
| Tenant izolasyonu | Global query filter + API key / JWT tenant claim |
| Kimlik doğrulama | Dual-auth (JWT + `X-Api-Key`), ayrı platform admin JWT |
| Yetkilendirme | Root / Admin / User + granular permission codes |
| Login koruması | Redis lockout, backoff, IP rate limit |
| API key | SHA-256 hash, rotate endpoint, tek seferlik gösterim |
| Hata yanıtları | RFC 7807 Problem Details; prod'da exception detayı yok |
| Audit | Platform admin işlem logu (impersonate, ödeme, tenant silme vb.) |
| CORS | Explicit origin listesi |
| Proxy | Forwarded header trust flag |

---

## Teknoloji yığını

| Alan | Teknoloji | Sürüm |
|------|-----------|-------|
| Runtime | .NET | 8.0 |
| API | ASP.NET Core Web API | 8.0 |
| ORM | Entity Framework Core | 8.0.11 |
| Veritabanı | PostgreSQL | 16 (Alpine) |
| Cache / limit | Redis | 7 (Alpine) |
| Validation | FluentValidation | 12.x |
| Auth | JWT Bearer + custom API key handler | — |
| Frontend | React + Vite + TypeScript | 18 / 5.4 |
| UI | Tailwind CSS, shadcn/ui pattern | 3.4 |
| State | TanStack Query | 5.x |
| Test | xUnit, WebApplicationFactory, Moq | — |
| Container | Docker, Docker Compose | — |
| CI | GitHub Actions | — |
| Reverse proxy (FE) | nginx | 1.27 Alpine |

---

## Mimari

Bağımlılıklar **içe** doğru (Clean Architecture):

```
EnterpriseLogger.Api          → HTTP, middleware, DI root, Swagger (Dev)
EnterpriseLogger.Infrastructure → EF Core, Redis, SMTP, background processors
EnterpriseLogger.Application  → Commands/Queries, validators, DTOs
EnterpriseLogger.Domain       → Entities, enums (sıfır altyapı bağımlılığı)
```

**Django karşılığı (özet):** `models.py` → Domain; `views` + services → Application Commands; `urls.py` → Controllers; `migrate` → EF migrations; Celery → `BackgroundService`.

---

## Hızlı başlangıç — Docker

Tam yığın: PostgreSQL + Redis + API + Tenant Panel + Platform Admin.

### Ön koşullar

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) veya Docker Engine + Compose v2
- Git

### Adımlar

```bash
# 1. Repoyu klonla
git clone https://github.com/Zeymurat/EnterpriseLoggerSaaS.git
cd EnterpriseLoggerSaaS

# 2. Ortam dosyasını oluştur
cp .env.example .env

# 3. .env içinde en az şunları düzenle:
#    POSTGRES_PASSWORD=...
#    JWT_SECRET=...          (min 32 karakter)
#    PLATFORM_ADMIN_EMAIL=...
#    PLATFORM_ADMIN_PASSWORD=...  (min 12 karakter)

# 4. Tüm servisleri derle ve başlat (ilk sefer 5–10 dk sürebilir)
docker compose up -d --build

# 5. Durumu kontrol et
docker compose ps
curl http://localhost:5247/health
```

API ilk açılışta **migration** uygular ve boşsa **platform admin** + paket seed verilerini yükler.

### Durdurma

```bash
docker compose down          # veriler volume'da kalır
docker compose down -v       # postgres/redis volume'larını da siler
```

### Sorun giderme

| Belirti | Çözüm |
|---------|--------|
| API `connection refused` | `docker compose logs enterprise-api` — postgres healthy mi? |
| Panel API'ye ulaşamıyor | `.env` içinde `CORS_ALLOWED_ORIGINS` = `http://localhost:5173,http://localhost:5174` |
| Platform admin giriş yok | `PLATFORM_ADMIN_EMAIL` / `PASSWORD` `.env`'de dolu mu? DB boşsa seed bir kez çalışır |
| Port çakışması | `docker-compose.yml` portlarını değiştir; frontends'i yeniden build et (`VITE_API_URL`) |

---

## Geliştirici kurulumu (isteğe bağlı)

Terminalden ayrı ayrı çalıştırmak için:

```bash
docker compose up -d enterprise-postgres enterprise-redis
cp .env.example .env   # localhost connection strings
dotnet run --project EnterpriseLogger.Api

cd apps/tenant-panel && cp .env.example .env && npm ci && npm run dev
cd apps/platform-admin && cp .env.example .env && npm ci && npm run dev
```

Migration **Development** ortamında API başlarken otomatik uygulanır.

---

## Uygulama adresleri

| Servis | URL | Açıklama |
|--------|-----|----------|
| Tenant Panel | http://localhost:5173 | Tenant admin / kullanıcı paneli |
| Platform Admin | http://localhost:5174 | Operatör: müşteri, paket, ödeme |
| API | http://localhost:5247 | REST API |
| Swagger | http://localhost:5247/swagger | Yalnızca Development |
| Health | http://localhost:5247/health | Docker / load balancer probe |

---

## CI/CD

`.github/workflows/ci.yml` — her `main` push/PR:

- `dotnet test` (Release)
- `apps/tenant-panel` → `npm ci && npm run build`
- `apps/platform-admin` → `npm ci && npm run build`

---

## Repository layout

```
EnterpriseLoggerSaaS/
├── docker-compose.yml          # Tam yığın (DB, Redis, API, 2 FE)
├── .env.example
├── EnterpriseLogger.Api/       # Dockerfile, Controllers, Middleware
├── EnterpriseLogger.Application/
├── EnterpriseLogger.Domain/
├── EnterpriseLogger.Infrastructure/
├── EnterpriseLogger.*.Tests/
├── apps/tenant-panel/          # Dockerfile + nginx
└── apps/platform-admin/
```

---

## Contributing

Private repo — internal: branch → PR → `main`. Solo geliştirmede doğrudan `main` commit mümkün.

---

## Author

**Zeymurat** — Enterprise SaaS, .NET backend pratiği ve multi-tenant platform mühendisliği.
