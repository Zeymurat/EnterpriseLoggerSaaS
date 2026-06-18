import { useState } from 'react'
import { Building2, Loader2 } from 'lucide-react'
import { ApiError, type TenantLoginOption } from '@/lib/api'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { UserRoleBadge } from '@/components/users/UserRoleBadge'
import { toast } from '@/components/ui/sonner'
import { cn } from '@/lib/utils'
import type { PendingLogin } from '@/components/auth/LoginForm'

interface TenantPickerDialogProps {
  pendingLogin: PendingLogin | null
  tenantOptions: TenantLoginOption[]
  onClose: () => void
}

export function TenantPickerDialog({
  pendingLogin,
  tenantOptions,
  onClose,
}: TenantPickerDialogProps) {
  const { login } = useAuth()
  const [selectedTenant, setSelectedTenant] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  const onTenantSelect = async (tenantName: string) => {
    if (!pendingLogin || isSubmitting) return

    setSelectedTenant(tenantName)
    setIsSubmitting(true)

    try {
      await login({
        email: pendingLogin.email,
        password: pendingLogin.password,
        tenantName,
      })
      onClose()
      toast.success('Giriş başarılı')
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.message
          : 'Giriş tamamlanamadı. Lütfen tekrar deneyin.'
      toast.error(message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Dialog
      open={pendingLogin !== null}
      onClose={onClose}
      title={pendingLogin ? `${pendingLogin.email} için Şirket seçin` : 'Şirket seçin'}
      description="Giriş yapmak istediğiniz şirkete tıklayın."
    >
      <div className="space-y-2" role="listbox" aria-label="Şirket listesi">
        {tenantOptions.map((option) => {
          const isActive = selectedTenant === option.tenantName && isSubmitting

          return (
            <button
              key={option.tenantName}
              type="button"
              role="option"
              aria-selected={isActive}
              disabled={isSubmitting}
              onClick={() => onTenantSelect(option.tenantName)}
              className={cn(
                'flex w-full items-center gap-3 rounded-xl border px-4 py-3.5 text-left transition-all',
                'hover:border-primary hover:bg-primary/5 hover:shadow-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
                isActive && 'border-primary bg-primary/5 shadow-sm',
                isSubmitting && !isActive && 'opacity-60',
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
        <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
          İptal
        </Button>
      </div>
    </Dialog>
  )
}
