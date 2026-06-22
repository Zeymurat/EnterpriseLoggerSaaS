import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { platformLogin, type PlatformAdminInfo } from '@/lib/api'
import {
  clearSession,
  getAccessToken,
  getStoredAdmin,
  saveSession,
  type PlatformAuthSession,
} from '@/lib/auth'

interface AuthContextValue {
  admin: PlatformAdminInfo | null
  accessToken: string | null
  isAuthenticated: boolean
  login: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<PlatformAuthSession | null>(() => {
    const token = getAccessToken()
    const admin = getStoredAdmin()
    if (!token || !admin) return null
    return { accessToken: token, admin }
  })

  const login = useCallback(async (email: string, password: string) => {
    const response = await platformLogin({ email, password })
    const next: PlatformAuthSession = {
      accessToken: response.accessToken,
      admin: response.admin,
    }
    saveSession(next)
    setSession(next)
  }, [])

  const logout = useCallback(() => {
    clearSession()
    setSession(null)
  }, [])

  const value = useMemo<AuthContextValue>(
    () => ({
      admin: session?.admin ?? null,
      accessToken: session?.accessToken ?? null,
      isAuthenticated: session !== null,
      login,
      logout,
    }),
    [session, login, logout],
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
