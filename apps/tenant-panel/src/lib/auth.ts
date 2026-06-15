import type { UserInfo } from '@/lib/api'

const TOKEN_KEY = 'enterprise_logger_token'
const USER_KEY = 'enterprise_logger_user'

export interface AuthSession {
  accessToken: string
  user: UserInfo
}

export function saveSession(session: AuthSession): void {
  localStorage.setItem(TOKEN_KEY, session.accessToken)
  localStorage.setItem(USER_KEY, JSON.stringify(session.user))
}

export function clearSession(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(USER_KEY)
}

export function getAccessToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function getStoredUser(): UserInfo | null {
  const raw = localStorage.getItem(USER_KEY)
  if (!raw) return null

  try {
    return JSON.parse(raw) as UserInfo
  } catch {
    return null
  }
}

export function hasPermission(user: UserInfo | null, permission: string): boolean {
  if (!user) return false
  if (user.role === 'Root' || user.role === 'Admin') return true
  return user.permissions.includes(permission)
}
