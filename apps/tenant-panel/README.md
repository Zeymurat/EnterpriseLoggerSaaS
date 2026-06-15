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

## Scripts

| Command | Description |
|---------|-------------|
| `npm run dev` | Dev server with HMR |
| `npm run build` | Production build to `dist/` |
| `npm run preview` | Preview production build |
