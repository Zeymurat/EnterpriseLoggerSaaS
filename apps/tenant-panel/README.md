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

## Environment

| Variable | Default | Description |
|----------|---------|-------------|
| `VITE_API_URL` | `http://localhost:5247` | Backend API base URL |

Ensure API `CORS_ALLOWED_ORIGINS` includes `http://localhost:5173` (see root `.env.example`).

## UX patterns

| Situation | UI |
|-----------|-----|
| Tenant has never sent a log | `LogsOnboardingCard` on Dashboard and Logs (Rust/Cream card, sample `POST /api/logs`, doc link) |
| Filters/search return zero rows | `EmptyState` in the log table with *"Arama kriterlerine uygun log bulunamadı"* |
| API unreachable / network error | `ApiErrorCard` with retry; `api.ts` throws `ApiError` status `0` via `apiFetch` |
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
