import { useEffect, useState } from 'react'
import { Navigate, useSearchParams } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { consumeImpersonationTicket } from '@/lib/api'

export function ImpersonatePage() {
  const [searchParams] = useSearchParams()
  const { establishSession } = useAuth()
  const [error, setError] = useState<string | null>(null)
  const [ready, setReady] = useState(false)

  useEffect(() => {
    const ticket = searchParams.get('ticket')?.trim()
    if (!ticket) {
      setError('Login-as bağlantısı geçersiz.')
      return
    }

    let cancelled = false

    void consumeImpersonationTicket(ticket)
      .then((session) => {
        if (cancelled) return

        establishSession({
          accessToken: session.accessToken,
          user: session.user,
        })
        window.history.replaceState({}, '', '/impersonate')
        setReady(true)
      })
      .catch(() => {
        if (cancelled) return
        setError('Oturum açılamadı. Lütfen platform admin panelinden tekrar deneyin.')
      })

    return () => {
      cancelled = true
    }
  }, [searchParams, establishSession])

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
