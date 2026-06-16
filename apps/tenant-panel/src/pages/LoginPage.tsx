import { useState } from 'react'
import { Navigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Activity, Building2, Loader2, Lock, Mail } from 'lucide-react'
import { AmbiguousTenantError, ApiError, type TenantLoginOption } from '@/lib/api'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Dialog } from '@/components/ui/dialog'
import { UserRoleBadge } from '@/components/users/UserRoleBadge'
import { toast } from '@/components/ui/sonner'
import { cn } from '@/lib/utils'

const loginSchema = z.object({
  email: z.string().email('Geçerli bir e-posta giriniz'),
  password: z.string().min(1, 'Şifre gerekli'),
})

type LoginForm = z.infer<typeof loginSchema>

interface PendingLogin {
  email: string
  password: string
}

export function LoginPage() {
  const { login, isAuthenticated } = useAuth()
  const [error, setError] = useState<string | null>(null)
  const [pendingLogin, setPendingLogin] = useState<PendingLogin | null>(null)
  const [tenantOptions, setTenantOptions] = useState<TenantLoginOption[]>([])
  const [selectedTenant, setSelectedTenant] = useState('')
  const [isTenantSubmitting, setIsTenantSubmitting] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  const completeLogin = async (credentials: PendingLogin, tenantName?: string) => {
    await login({
      email: credentials.email,
      password: credentials.password,
      tenantName,
    })
  }

  const onSubmit = async (values: LoginForm) => {
    setError(null)
    const credentials = { email: values.email, password: values.password }

    try {
      await completeLogin(credentials)
      toast.success('Giriş başarılı', { description: 'Yönetim paneline yönlendiriliyorsunuz.' })
    } catch (err) {
      if (err instanceof AmbiguousTenantError) {
        setPendingLogin(credentials)
        setTenantOptions(err.tenantOptions)
        setSelectedTenant(err.tenantOptions[0]?.tenantName ?? '')
        return
      }

      const message =
        err instanceof ApiError
          ? err.message
          : 'Giriş yapılamadı. API çalışıyor mu kontrol edin.'
      setError(message)
      toast.error('Giriş başarısız', { description: message })
    }
  }

  const onTenantSelect = async (tenantName: string) => {
    if (!pendingLogin || isTenantSubmitting) return

    setSelectedTenant(tenantName)
    setIsTenantSubmitting(true)
    setError(null)

    try {
      await completeLogin(pendingLogin, tenantName)
      setPendingLogin(null)
      setTenantOptions([])
      toast.success('Giriş başarılı')
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : 'Giriş tamamlanamadı. Lütfen tekrar deneyin.'
      setError(message)
      toast.error(message)
    } finally {
      setIsTenantSubmitting(false)
    }
  }

  const closeTenantPicker = () => {
    setPendingLogin(null)
    setTenantOptions([])
    setSelectedTenant('')
  }

  return (
    <>
      <div className="flex min-h-screen">
        <div className="relative hidden w-1/2 flex-col justify-between overflow-hidden bg-sidebar p-10 text-sidebar-foreground lg:flex">
          <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,_hsl(221_83%_53%_/_0.35),_transparent_55%)]" />
          <div className="relative flex items-center gap-3">
            <div className="flex h-11 w-11 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-lg">
              <Activity className="h-6 w-6" />
            </div>
            <div>
              <p className="text-lg font-semibold">EnterpriseLogger</p>
              <p className="text-sm text-sidebar-muted">Tenant Yönetim Paneli</p>
            </div>
          </div>

          <div className="relative space-y-4">
            <h1 className="text-4xl font-bold leading-tight tracking-tight">
              Loglarınızı ve ekibinizi tek yerden yönetin.
            </h1>
            <p className="max-w-md text-sidebar-muted">
              Çoklu tenant desteği, rol tabanlı erişim ve gerçek zamanlı log izleme — hepsi bir arada.
            </p>
          </div>

          <p className="relative text-xs text-sidebar-muted">© EnterpriseLogger SaaS</p>
        </div>

        <div className="flex flex-1 items-center justify-center p-6 sm:p-10">
          <div className="w-full max-w-md space-y-8">
            <div className="space-y-2 lg:hidden">
              <div className="flex items-center gap-2">
                <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-primary-foreground">
                  <Activity className="h-5 w-5" />
                </div>
                <span className="text-lg font-semibold">EnterpriseLogger</span>
              </div>
              <p className="text-sm text-muted-foreground">Yönetim paneline giriş yapın</p>
            </div>

            <div className="rounded-2xl border bg-card p-8 shadow-sm">
              <div className="mb-6 space-y-1">
                <h2 className="text-2xl font-bold tracking-tight">Hoş geldiniz</h2>
                <p className="text-sm text-muted-foreground">Hesabınıza giriş yapın</p>
              </div>

              <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                <div className="space-y-2">
                  <Label htmlFor="email">E-posta</Label>
                  <div className="relative">
                    <Mail className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      id="email"
                      type="email"
                      autoComplete="email"
                      className="pl-9"
                      {...register('email')}
                    />
                  </div>
                  {errors.email && (
                    <p className="text-sm text-destructive">{errors.email.message}</p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="password">Şifre</Label>
                  <div className="relative">
                    <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                    <Input
                      id="password"
                      type="password"
                      autoComplete="current-password"
                      className="pl-9"
                      {...register('password')}
                    />
                  </div>
                  {errors.password && (
                    <p className="text-sm text-destructive">{errors.password.message}</p>
                  )}
                </div>

                {error && (
                  <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-sm text-destructive">
                    {error}
                  </div>
                )}

                <Button type="submit" className="w-full" size="lg" disabled={isSubmitting}>
                  {isSubmitting ? 'Giriş yapılıyor...' : 'Giriş yap'}
                </Button>
              </form>
            </div>
          </div>
        </div>
      </div>

      <Dialog
        open={pendingLogin !== null}
        onClose={closeTenantPicker}
        title={pendingLogin ? `${pendingLogin.email} için Şirket seçin` : 'Şirket seçin'}
        description="Giriş yapmak istediğiniz şirkete tıklayın."
      >
        <div className="space-y-2" role="listbox" aria-label="Şirket listesi">
          {tenantOptions.map((option) => {
            const isActive = selectedTenant === option.tenantName && isTenantSubmitting

            return (
              <button
                key={option.tenantName}
                type="button"
                role="option"
                aria-selected={isActive}
                disabled={isTenantSubmitting}
                onClick={() => onTenantSelect(option.tenantName)}
                className={cn(
                  'flex w-full items-center gap-3 rounded-xl border px-4 py-3.5 text-left transition-all',
                  'hover:border-primary hover:bg-primary/5 hover:shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
                  isActive && 'border-primary bg-primary/5 shadow-sm',
                  isTenantSubmitting && !isActive && 'opacity-60',
                )}
              >
                <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary/10">
                  <Building2 className="h-5 w-5 text-primary" />
                </div>

                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium">{option.tenantName}</p>
                  {option.displayName && (
                    <p className="truncate text-sm text-muted-foreground">{option.displayName}</p>
                  )}
                  <div className="mt-1.5">
                    <UserRoleBadge role={option.role} />
                  </div>
                </div>

                {isActive && <Loader2 className="h-4 w-4 shrink-0 animate-spin text-primary" />}
              </button>
            )
          })}
        </div>

        <div className="mt-4 flex justify-end">
          <Button type="button" variant="outline" onClick={closeTenantPicker} disabled={isTenantSubmitting}>
            İptal
          </Button>
        </div>
      </Dialog>
    </>
  )
}
