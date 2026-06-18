import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Checkbox } from '@/components/ui/checkbox'
import { Sheet } from '@/components/ui/sheet'
import { INVITE_PERMISSION_OPTIONS } from '@/lib/permissions'

const inviteSchema = z
  .object({
    email: z.string().email('Geçerli e-posta giriniz'),
    phone: z.string().min(10, 'Geçerli telefon giriniz'),
    role: z.enum(['User', 'Admin']),
    permissions: z.array(z.string()),
  })
  .superRefine((data, ctx) => {
    if (data.role === 'User' && data.permissions.length === 0) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'User rolü için en az bir izin seçin',
        path: ['permissions'],
      })
    }
  })

export type InviteFormValues = z.infer<typeof inviteSchema>

const INVITE_FORM_DEFAULTS: InviteFormValues = {
  email: '',
  phone: '',
  role: 'User',
  permissions: ['logs:read'],
}

interface InviteSheetProps {
  open: boolean
  onClose: () => void
  isRoot: boolean
  isSubmitting: boolean
  onSubmit: (data: InviteFormValues) => void
}

export function InviteSheet({
  open,
  onClose,
  isRoot,
  isSubmitting,
  onSubmit,
}: InviteSheetProps) {
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors },
  } = useForm<InviteFormValues>({
    resolver: zodResolver(inviteSchema),
    defaultValues: INVITE_FORM_DEFAULTS,
  })

  useEffect(() => {
    if (open) reset(INVITE_FORM_DEFAULTS)
  }, [open, reset])

  const role = watch('role')
  const selectedPermissions = watch('permissions')

  const togglePermission = (code: string) => {
    const next = selectedPermissions.includes(code)
      ? selectedPermissions.filter((p) => p !== code)
      : [...selectedPermissions, code]
    setValue('permissions', next, { shouldValidate: true })
  }

  return (
    <Sheet
      open={open}
      onClose={onClose}
      title="Kullanıcı davet et"
      description="Davet edilen kullanıcıya geçici şifre bir kez gösterilir."
      side="right"
    >
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="invite-email">E-posta</Label>
          <Input id="invite-email" type="email" {...register('email')} />
          {errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
        </div>

        <div className="space-y-2">
          <Label htmlFor="invite-phone">Telefon</Label>
          <Input id="invite-phone" placeholder="05551234567" {...register('phone')} />
          {errors.phone && <p className="text-sm text-destructive">{errors.phone.message}</p>}
        </div>

        <div className="space-y-2">
          <Label htmlFor="invite-role">Rol</Label>
          <Select id="invite-role" {...register('role')}>
            <option value="User">User</option>
            {isRoot && <option value="Admin">Admin</option>}
          </Select>
        </div>

        {role === 'User' && (
          <div className="space-y-2">
            <Label>İzinler</Label>
            <div className="space-y-1 rounded-xl border bg-muted/20 p-2">
              {INVITE_PERMISSION_OPTIONS.map((p) => (
                <Checkbox
                  key={p.code}
                  id={`invite-${p.code}`}
                  checked={selectedPermissions.includes(p.code)}
                  onChange={() => togglePermission(p.code)}
                  label={
                    <span>
                      {p.label}{' '}
                      <span className="text-xs text-muted-foreground">({p.code})</span>
                    </span>
                  }
                />
              ))}
            </div>
            {errors.permissions && (
              <p className="text-sm text-destructive">{errors.permissions.message}</p>
            )}
          </div>
        )}

        <div className="flex justify-end gap-2 pt-2">
          <Button type="button" variant="outline" onClick={onClose}>
            İptal
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Davet ediliyor...' : 'Davet et'}
          </Button>
        </div>
      </form>
    </Sheet>
  )
}
