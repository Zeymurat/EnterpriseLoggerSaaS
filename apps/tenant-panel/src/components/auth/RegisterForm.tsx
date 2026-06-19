import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Building2, Lock, Mail, Phone } from 'lucide-react'
import { ApiError, registerTenant } from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/components/ui/sonner'

const registerSchema = z.object({
  name: z.string().min(2, 'Şirket adı en az 2 karakter olmalı'),
  ownerEmail: z.string().email('Geçerli bir e-posta giriniz'),
  ownerPhone: z.string().min(10, 'Geçerli bir telefon giriniz'),
  ownerPassword: z
    .string()
    .min(8, 'Şifre en az 8 karakter olmalı')
    .regex(/[A-Z]/, 'En az bir büyük harf gerekli')
    .regex(/[a-z]/, 'En az bir küçük harf gerekli')
    .regex(/[0-9]/, 'En az bir rakam gerekli'),
})

export type RegisterFormValues = z.infer<typeof registerSchema>

interface RegisterFormProps {
  onRegistered: () => void
  onSwitchToLogin: () => void
}

export function RegisterForm({ onRegistered, onSwitchToLogin }: RegisterFormProps) {
  const [error, setError] = useState<string | null>(null)

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterFormValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      name: '',
      ownerEmail: '',
      ownerPhone: '',
      ownerPassword: '',
    },
  })

  const onSubmit = async (values: RegisterFormValues) => {
    setError(null)

    try {
      await registerTenant(values)
      toast.success('Şirket kaydı başarılı', {
        description:
          'Giriş yaptıktan sonra dashboard üzerinden API anahtarınızı üretebilirsiniz.',
        duration: 8000,
      })
      onRegistered()
    } catch (err) {
      const message =
        err instanceof ApiError ? err.message : 'Kayıt tamamlanamadı. Lütfen tekrar deneyin.'
      setError(message)
      toast.error('Kayıt başarısız', { description: message })
    }
  }

  return (
    <div className="w-full">
      <div className="mb-6 space-y-1">
        <h2 className="text-2xl font-bold tracking-tight">Şirket kaydı</h2>
        <p className="text-sm text-muted-foreground">Yeni bir tenant oluşturun ve Root olun</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="tenant-name">Şirket adı</Label>
          <div className="relative">
            <Building2 className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input id="tenant-name" className="pl-9" {...register('name')} />
          </div>
          {errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
        </div>

        <div className="space-y-2">
          <Label htmlFor="owner-email">Sahip e-postası</Label>
          <div className="relative">
            <Mail className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input id="owner-email" type="email" autoComplete="email" className="pl-9" {...register('ownerEmail')} />
          </div>
          {errors.ownerEmail && (
            <p className="text-sm text-destructive">{errors.ownerEmail.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="owner-phone">Telefon</Label>
          <div className="relative">
            <Phone className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input id="owner-phone" placeholder="05551234567" className="pl-9" {...register('ownerPhone')} />
          </div>
          {errors.ownerPhone && (
            <p className="text-sm text-destructive">{errors.ownerPhone.message}</p>
          )}
        </div>

        <div className="space-y-2">
          <Label htmlFor="owner-password">Şifre</Label>
          <div className="relative">
            <Lock className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              id="owner-password"
              type="password"
              autoComplete="new-password"
              className="pl-9"
              {...register('ownerPassword')}
            />
          </div>
          {errors.ownerPassword && (
            <p className="text-sm text-destructive">{errors.ownerPassword.message}</p>
          )}
        </div>

        {error && (
          <div className="rounded-lg border border-destructive/30 bg-destructive/5 px-3 py-2 text-sm text-destructive">
            {error}
          </div>
        )}

        <Button type="submit" className="w-full" size="lg" disabled={isSubmitting}>
          {isSubmitting ? 'Kayıt oluşturuluyor...' : 'Şirket oluştur'}
        </Button>

        <p className="text-center text-sm text-muted-foreground lg:hidden">
          Zaten hesabınız var mı?{' '}
          <button
            type="button"
            className="font-medium text-primary underline-offset-4 hover:underline"
            onClick={onSwitchToLogin}
          >
            Giriş yap
          </button>
        </p>
      </form>
    </div>
  )
}
