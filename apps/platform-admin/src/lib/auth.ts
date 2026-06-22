import type { PlatformAdminInfo } from '@/lib/api'

const TOKEN_KEY = 'enterprise_logger_platform_token'
const ADMIN_KEY = 'enterprise_logger_platform_admin'

export interface PlatformAuthSession {
  accessToken: string
  admin: PlatformAdminInfo
}

export function saveSession(session: PlatformAuthSession): void {
  localStorage.setItem(TOKEN_KEY, session.accessToken)
  localStorage.setItem(ADMIN_KEY, JSON.stringify(session.admin))
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(ADMIN_KEY)
}

export function getAccessToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function getStoredAdmin(): PlatformAdminInfo | null {
  const raw = localStorage.getItem(ADMIN_KEY)
  if (!raw) return null

  try {
    return JSON.parse(raw) as PlatformAdminInfo
  } catch {
    return null
  }
}
