import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useQueryClient } from '@tanstack/react-query'
import { useAuth } from '@/contexts/AuthContext'
import { registerUnauthorizedHandler } from '@/lib/unauthorized-session'

export function SessionExpiredBridge() {
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const { logout } = useAuth()

  useEffect(() => {
    registerUnauthorizedHandler(() => {
      logout()
      queryClient.clear()
      navigate('/login?session=expired', { replace: true })
    })

    return () => registerUnauthorizedHandler(null)
  }, [logout, navigate, queryClient])

  return null
}
