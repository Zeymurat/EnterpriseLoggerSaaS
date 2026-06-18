import { useEffect, useState } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import type { TenantLoginOption, TenantRegistration } from '@/lib/api'
import { useAuth } from '@/contexts/AuthContext'
import { ApiKeySuccessDialog } from '@/components/auth/ApiKeySuccessDialog'
import { AuthShell } from '@/components/auth/AuthShell'
import type { AuthMode } from '@/components/auth/BrandPanel'
import { LoginForm, type PendingLogin } from '@/components/auth/LoginForm'
import { RegisterForm } from '@/components/auth/RegisterForm'
import { TenantPickerDialog } from '@/components/auth/TenantPickerDialog'

export function AuthPage() {
  const { isAuthenticated } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()

  const initialMode: AuthMode = location.pathname === '/register' ? 'register' : 'login'
  const [mode, setMode] = useState<AuthMode>(initialMode)
  const [pendingLogin, setPendingLogin] = useState<PendingLogin | null>(null)
  const [tenantOptions, setTenantOptions] = useState<TenantLoginOption[]>([])
  const [registration, setRegistration] = useState<TenantRegistration | null>(null)

  useEffect(() => {
    setMode(location.pathname === '/register' ? 'register' : 'login')
  }, [location.pathname])

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  const switchMode = (next: AuthMode) => {
    setMode(next)
    navigate(next === 'register' ? '/register' : '/login', { replace: true })
  }

  const handleAmbiguousTenant = (credentials: PendingLogin, options: TenantLoginOption[]) => {
    setPendingLogin(credentials)
    setTenantOptions(options)
  }

  const closeTenantPicker = () => {
    setPendingLogin(null)
    setTenantOptions([])
  }

  const handleRegistrationSuccess = (result: TenantRegistration) => {
    setRegistration(result)
  }

  const handleApiKeyContinue = () => {
    setRegistration(null)
    switchMode('login')
  }

  return (
    <>
      <AuthShell
        mode={mode}
        onSwitchMode={switchMode}
        loginPanel={
          <div className="mx-auto w-full max-w-lg sm:max-w-xl lg:max-w-2xl rounded-2xl border bg-card p-8 shadow-sm sm:p-10 lg:border-0 lg:bg-transparent lg:p-0 lg:shadow-none">
            <LoginForm
              onAmbiguousTenant={handleAmbiguousTenant}
              onSwitchToRegister={() => switchMode('register')}
            />
          </div>
        }
        registerPanel={
          <div className="mx-auto w-full max-w-lg sm:max-w-xl lg:max-w-2xl rounded-2xl border bg-card p-8 shadow-sm sm:p-10 lg:border-0 lg:bg-transparent lg:p-0 lg:shadow-none">
            <RegisterForm
              onSuccess={handleRegistrationSuccess}
              onSwitchToLogin={() => switchMode('login')}
            />
          </div>
        }
      />

      <TenantPickerDialog
        pendingLogin={pendingLogin}
        tenantOptions={tenantOptions}
        onClose={closeTenantPicker}
      />

      <ApiKeySuccessDialog
        registration={registration}
        onClose={() => setRegistration(null)}
        onContinue={handleApiKeyContinue}
      />
    </>
  )
}
