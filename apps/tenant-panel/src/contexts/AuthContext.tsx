import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { login as apiLogin, refreshSession as apiRefreshSession, type LoginRequest } from '@/lib/api'
import {
  clearSession,
  getAccessToken,
  getStoredUser,
  hasPermission,
  saveSession,
  type AuthSession,
} from '@/lib/auth'
import { recordSessionActivity } from '@/lib/session-activity'
import type { UserInfo } from '@/lib/api'

interface AuthContextValue {
  user: UserInfo | null
  accessToken: string | null
  isAuthenticated: boolean
  login: (request: LoginRequest) => Promise<void>
  refreshSession: () => Promise<void>
  logout: () => void
  can: (permission: string) => boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(() => {
    const token = getAccessToken()
    const user = getStoredUser()
    if (!token || !user) return null
    return { accessToken: token, user }
  })

  const login = useCallback(async (request: LoginRequest) => {
    const response = await apiLogin(request)
    const next: AuthSession = {
      accessToken: response.accessToken,
      user: response.user,
    }
    saveSession(next)
    setSession(next)
    recordSessionActivity()
  }, [])

  const refreshSession = useCallback(async () => {
    const response = await apiRefreshSession()
    const next: AuthSession = {
      accessToken: response.accessToken,
      user: response.user,
    }
    saveSession(next)
    setSession(next)
    recordSessionActivity()
  }, [])

  const logout = useCallback(() => {
    clearSession()
    setSession(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      user: session?.user ?? null,
      accessToken: session?.accessToken ?? null,
      isAuthenticated: session !== null,
      login,
      refreshSession,
      logout,
      can: (permission: string) => hasPermission(session?.user ?? null, permission),
    }),
    [session, login, refreshSession, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}
