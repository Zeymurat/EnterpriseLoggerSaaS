# Tenant Panel

React SPA for tenant administrators (Root / Admin / User).

## Stack

- Vite + React 18 + TypeScript
- Tailwind CSS + shadcn/ui-style components
- React Router, TanStack Query, React Hook Form + Zod

## Local development

```bash
# From repo root — API must be running on :5247
cd apps/tenant-panel
cp .env.example .env
npm install
npm run dev
```

Open **http://localhost:5173**

Copy root `.env.example` → `.env` for API/JWT settings when running the backend locally.

## Environment

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_API_URL` | `http://localhost:5247` | Backend API base URL |
| `VITE_SESSION_IDLE_MINUTES` | `15` | Hareketsizlik limiti (dk). Aşılınca uyarı, sonra logout. |
| `VITE_SESSION_WARN_MINUTES` | `2` | Idle limitinden kaç dk önce *Oturumu uzat* dialogu açılır. |

Ensure API `CORS_ALLOWED_ORIGINS` includes `http://localhost:5173` (see root `.env.example`).

### Session behaviour (panel)

Three timers work together (see root `README.md` → *Panel session model*):

1. **JWT access token** — API `JWT_ACCESS_TOKEN_EXPIRY_MINUTES` (default 60 min per token).
2. **Absolute cap** — API `JWT_MAX_SESSION_HOURS` (default 8 h since first login); refresh cannot extend past this.
3. **Idle UX** — panel env above; activity = mouse/keyboard/scroll/touch **or** any successful authenticated API response (including live log polling).

Flow:

```
13 dk idle → SessionExtendDialog ("Oturumu uzat" / "Çıkış yap")
[Evet] → POST /api/auth/refresh → new JWT, idle sıfırlanır
[Hayır / süre biter] → /login?session=expired&reason=idle
```

Components: `IdleSessionGuard`, `SessionExtendDialog`, `SessionExpiredBridge` (401 redirect).

## UX patterns

| Situation | UI |
|-----------|-----|
| Tenant has never sent a log | `LogsOnboardingCard` on Dashboard and Logs (Rust/Cream card, sample `POST /api/logs`, doc link) |
| Filters/search return zero rows | `EmptyState` in the log table with *"Arama kriterlerine uygun log bulunamadı"* |
| API unreachable / network error | `ApiErrorCard` with retry; `api.ts` throws `ApiError` status `0` via `apiFetch` |
| Expired JWT / missing session | `authFetch` 401 → logout + `/login?session=expired` |
| Idle timeout | Warning dialog → extend via refresh or logout → `/login?session=expired&reason=idle` |
| Date range with no rows (but tenant has logs) | Empty state: *Seçili zaman aralığında log bulunamadı* |
| Correlation trace from detail sheet | `/logs?correlationId=…` → trace banner + all-time filter |
| Multi-log correlation chain in list | Rust `GitBranch` badge with count — one-click trace filter |

Shared components live under `src/components/layout/` (`ApiErrorCard`, `EmptyState`), `src/components/logs/LogsOnboardingCard.tsx`, and `LogsTraceBanner.tsx`.

## Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Dev server with HMR |
| `npm run build` | Production build to `dist/` |
| `npm run preview` | Preview production build |
