import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Lock, Mail } from 'lucide-react'
import { AmbiguousTenantError, ApiError } from '@/lib/api'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/components/ui/sonner'
import type { TenantLoginOption } from '@/lib/api'

const loginSchema = z.object({
  email: z.string().email('Geçerli bir e-posta giriniz'),
  password: z.string().min(1, 'Şifre gerekli'),
})

export type LoginFormValues = z.infer<typeof loginSchema>

export interface PendingLogin {
  email: string
  password: string
}

interface LoginFormProps {
  onAmbiguousTenant: (credentials: PendingLogin, options: TenantLoginOption[]) => void
  onSwitchToRegister: () => void
}

export function LoginForm({ onAmbiguousTenant, onSwitchToRegister }: LoginFormProps) {
  const { login } = useAuth()
  const [error, setError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  })

  const onSubmit = async (values: LoginFormValues) => {
    setError(null)
    const credentials = { email: values.email, password: values.password }

    try {
      await login(credentials)
      toast.success('Giriş başarılı', { description: 'Yönetim paneline yönlendiriliyorsunuz.' })
    } catch (err) {
      if (err instanceof AmbiguousTenantError) {
        onAmbiguousTenant(credentials, err.tenantOptions)
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

  return (
    <div className="w-full">
      <div className="mb-6 space-y-1">
        <h2 className="text-2xl font-bold tracking-tight">Hoş geldiniz</h2>
        <p className="text-sm text-muted-foreground">Hesabınıza giriş yapın</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
        <div className="space-y-2">
          <Label htmlFor="login-email">E-posta</Label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="login-email"
              type="email"
              autoComplete="email"
              className="pl-9"
              {...register('email')}
            />
          </div>
          {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
        </div>

        <div className="space-y-2">
          <Label htmlFor="login-password">Şifre</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="login-password"
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

        <p className="text-center text-sm text-muted-foreground lg:hidden">
          Hesabınız yok mu?{' '}
          <button
            type="button"
            className="font-medium text-primary underline-offset-4 hover:underline"
            onClick={onSwitchToRegister}
          >
            Şirket kaydı oluştur
          </button>
        </p>
      </form>
    </div>
  )
}
