import { useEffect, useState } from 'react'
import { Navigate, useSearchParams } from 'react-router-dom'
import { saveSession } from '@/lib/auth'
import type { UserInfo } from '@/lib/api'

export function ImpersonatePage() {
  const [searchParams] = useSearchParams()
  const [error, setError] = useState<string | null>(null)
  const [ready, setReady] = useState(false)

  useEffect(() => {
    const raw = searchParams.get('session')
    if (!raw) {
      setError('Oturum bilgisi bulunamadı.')
      return
    }

    try {
      const decoded = JSON.parse(atob(decodeURIComponent(raw))) as {
        accessToken: string
        user: UserInfo
      }

      if (!decoded.accessToken || !decoded.user) {
        setError('Geçersiz oturum verisi.')
        return
      }

      saveSession({
        accessToken: decoded.accessToken,
        user: decoded.user,
      })

      window.history.replaceState({}, '', '/impersonate')
      setReady(true)
    } catch {
      setError('Oturum açılamadı. Lütfen platform admin panelinden tekrar deneyin.')
    }
  }, [searchParams])

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <p className="text-sm text-destructive">{error}</p>
      </div>
    )
  }

  if (!ready) {
    return (
      <div className="flex min-h-screen items-center justify-center p-6">
        <p className="text-sm text-muted-foreground">Tenant paneline giriş yapılıyor…</p>
      </div>
    )
  }

  return <Navigate to="/" replace />
}
