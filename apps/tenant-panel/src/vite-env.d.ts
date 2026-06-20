/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL: string
  readonly VITE_SESSION_IDLE_MINUTES?: string
  readonly VITE_SESSION_WARN_MINUTES?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
