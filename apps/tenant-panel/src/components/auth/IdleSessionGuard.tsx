import { useCallback, useEffect, useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { SessionExtendDialog } from '@/components/auth/SessionExtendDialog'
import { useAuth } from '@/contexts/AuthContext'
import { SESSION_IDLE_MS, SESSION_WARN_MS } from '@/lib/session-config'
import {
  bindGlobalSessionActivityListeners,
  registerSessionActivityListener,
} from '@/lib/session-activity'

export function IdleSessionGuard() {
  const { isAuthenticated, refreshSession, logout } = useAuth()
  const navigate = useNavigate()
  const queryClient = useQueryClient()

  const lastActivityRef = useRef(Date.now())
  const idleLogoutTriggeredRef = useRef(false)
  const [showWarning, setShowWarning] = useState(false)
  const [remainingSeconds, setRemainingSeconds] = useState(0)
  const [isExtending, setIsExtending] = useState(false)

  const bumpActivity = useCallback(() => {
    lastActivityRef.current = Date.now()
    idleLogoutTriggeredRef.current = false
    setShowWarning(false)
  }, [])

  const forceIdleLogout = useCallback(() => {
    logout()
    queryClient.clear()
    navigate('/login?session=expired&reason=idle', { replace: true })
  }, [logout, navigate, queryClient])

  const handleExtend = useCallback(async () => {
    setIsExtending(true)
    try {
      await refreshSession()
      bumpActivity()
    } catch {
      forceIdleLogout()
    } finally {
      setIsExtending(false)
    }
  }, [refreshSession, bumpActivity, forceIdleLogout])

  useEffect(() => {
    if (!isAuthenticated) {
      setShowWarning(false)
      return
    }

    bumpActivity()
    const unregisterActivity = registerSessionActivityListener(bumpActivity)
    const unbindGlobal = bindGlobalSessionActivityListeners(bumpActivity)

    return () => {
      unregisterActivity()
      unbindGlobal()
    }
  }, [isAuthenticated, bumpActivity])

  useEffect(() => {
    if (!isAuthenticated) return

    const tick = () => {
      const idleMs = Date.now() - lastActivityRef.current
      const warnAtMs = Math.max(SESSION_IDLE_MS - SESSION_WARN_MS, 0)

      if (idleMs >= SESSION_IDLE_MS) {
        setShowWarning(false)
        if (!idleLogoutTriggeredRef.current) {
          idleLogoutTriggeredRef.current = true
          forceIdleLogout()
        }
        return
      }

      if (idleMs >= warnAtMs) {
        setShowWarning(true)
        setRemainingSeconds(Math.max(1, Math.ceil((SESSION_IDLE_MS - idleMs) / 1000)))
        return
      }

      setShowWarning(false)
    }

    tick()
    const intervalId = window.setInterval(tick, 1000)
    return () => window.clearInterval(intervalId)
  }, [isAuthenticated, forceIdleLogout])

  if (!isAuthenticated) return null

  return (
    <SessionExtendDialog
      open={showWarning}
      remainingSeconds={remainingSeconds}
      isExtending={isExtending}
      onExtend={() => void handleExtend()}
      onLogout={forceIdleLogout}
    />
  )
}
