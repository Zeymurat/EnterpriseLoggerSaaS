import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { RefreshCw, Search, Shield, UserCheck, UserMinus, UserPlus, Users } from 'lucide-react'
import {
  deactivateUser,
  getUsers,
  inviteUser,
  updateUserPermissions,
  updateUserRole,
  useMockUsersOnly,
  type InviteUserRequest,
  type TenantUser,
} from '@/lib/api'
import { isMockUser, mergeWithMockUsers, MOCK_ADDITIONAL_USERS } from '@/lib/mock-users'
import { INVITE_PERMISSION_OPTIONS, permissionLabel } from '@/lib/permissions'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { Checkbox } from '@/components/ui/checkbox'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog, ConfirmDialog } from '@/components/ui/dialog'
import { UserRoleBadge } from '@/components/users/UserRoleBadge'
import {
  AccessDeniedCard,
  MockDataBanner,
  PageHeader,
  StatCard,
} from '@/components/layout/PageShell'
import { toast } from '@/components/ui/sonner'
import { cn } from '@/lib/utils'

type RoleFilter = 'All' | 'Root' | 'Admin' | 'User'

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

type InviteForm = z.infer<typeof inviteSchema>

const INVITE_FORM_DEFAULTS: InviteForm = {
  email: '',
  phone: '',
  role: 'User',
  permissions: ['logs:read'],
}

function filterUsers(users: TenantUser[], role: RoleFilter, search: string): TenantUser[] {
  const q = search.trim().toLowerCase()
  return users.filter((u) => {
    if (role !== 'All' && u.role !== role) return false
    if (!q) return true
    return (
      u.email.toLowerCase().includes(q) ||
      u.phone.toLowerCase().includes(q) ||
      u.role.toLowerCase().includes(q)
    )
  })
}

export function UsersPage() {
  const { user: currentUser, can } = useAuth()
  const queryClient = useQueryClient()
  const [search, setSearch] = useState('')
  const [roleFilter, setRoleFilter] = useState<RoleFilter>('All')
  const [inviteOpen, setInviteOpen] = useState(false)
  const [permissionsUser, setPermissionsUser] = useState<TenantUser | null>(null)
  const [deactivateTarget, setDeactivateTarget] = useState<TenantUser | null>(null)

  const usersQuery = useQuery({
    queryKey: ['users'],
    queryFn: getUsers,
    enabled: can('users:read') && !useMockUsersOnly(),
  })

  const { displayUsers, showingMockFallback, dataSource } = useMemo(() => {
    if (useMockUsersOnly()) {
      return {
        displayUsers: [
          {
            id: 1,
            email: currentUser?.email ?? 'root@demo.local',
            phone: currentUser?.phone ?? '+905550000000',
            role: 'Root',
            isActive: true,
            permissions: [],
          },
          ...MOCK_ADDITIONAL_USERS,
        ],
        showingMockFallback: true,
        dataSource: 'mock' as const,
      }
    }

    if (usersQuery.isLoading || usersQuery.isError) {
      const fallbackRoot: TenantUser = {
        id: currentUser?.id ?? 1,
        email: currentUser?.email ?? 'root@demo.local',
        phone: currentUser?.phone ?? '+905550000000',
        role: currentUser?.role ?? 'Root',
        isActive: true,
        permissions: currentUser?.permissions ?? [],
      }
      return {
        displayUsers: mergeWithMockUsers([fallbackRoot]),
        showingMockFallback: true,
        dataSource: 'mock' as const,
      }
    }

    const apiUsers = usersQuery.data ?? []
    if (apiUsers.length <= 1) {
      return {
        displayUsers: mergeWithMockUsers(apiUsers),
        showingMockFallback: true,
        dataSource: 'mock-fallback' as const,
      }
    }

    return {
      displayUsers: apiUsers,
      showingMockFallback: false,
      dataSource: 'api' as const,
    }
  }, [usersQuery.data, usersQuery.isLoading, usersQuery.isError, currentUser])

  const filteredUsers = useMemo(
    () => filterUsers(displayUsers, roleFilter, search),
    [displayUsers, roleFilter, search],
  )

  const inviteMutation = useMutation({
    mutationFn: (data: InviteUserRequest) => inviteUser(data),
    onSuccess: (res) => {
      setInviteOpen(false)
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast.success('Kullanıcı davet edildi', {
        description: `Geçici şifre (yalnızca bir kez): ${res.temporaryPassword}`,
        duration: 20_000,
      })
    },
    onError: (err: Error) => toast.error('Davet başarısız', { description: err.message }),
  })

  const permissionsMutation = useMutation({
    mutationFn: ({ id, permissions }: { id: number; permissions: string[] }) =>
      updateUserPermissions(id, permissions),
    onSuccess: () => {
      setPermissionsUser(null)
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast.success('İzinler güncellendi')
    },
    onError: (err: Error) => toast.error(err.message),
  })

  const roleMutation = useMutation({
    mutationFn: ({ id, role }: { id: number; role: string }) => updateUserRole(id, role),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast.success('Kullanıcı Admin yapıldı')
    },
    onError: (err: Error) => toast.error(err.message),
  })

  const deactivateMutation = useMutation({
    mutationFn: (id: number) => deactivateUser(id),
    onSuccess: () => {
      setDeactivateTarget(null)
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast.success('Kullanıcı pasife alındı')
    },
    onError: (err: Error) => toast.error(err.message),
  })

  const canInvite = can('users:invite')
  const canManage = can('users:manage')
  const isRoot = currentUser?.role === 'Root'

  if (!can('users:read')) {
    return <AccessDeniedCard permission="users:read" />
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Kullanıcılar"
        description="Ekip üyelerini davet edin, rollerini ve izinlerini yönetin."
        actions={
          <>
            <Button
              variant="outline"
              size="sm"
              onClick={() => usersQuery.refetch()}
              disabled={useMockUsersOnly() || usersQuery.isFetching}
            >
              <RefreshCw className={cn('h-4 w-4', usersQuery.isFetching && 'animate-spin')} />
              Yenile
            </Button>
            {canInvite && (
              <Button size="sm" onClick={() => setInviteOpen(true)}>
                <UserPlus className="h-4 w-4" />
                Davet et
              </Button>
            )}
          </>
        }
      />

      {showingMockFallback && (
        <MockDataBanner
          title="Demo kullanıcılar gösteriliyor"
          description={
            dataSource === 'mock-fallback'
              ? "Tenant'ta yalnızca Root var — örnek satırlar eklendi. Davet ile gerçek kullanıcı ekleyebilirsiniz."
              : 'Mock mod veya API yüklenemedi — demo satırlar salt okunur.'
          }
        />
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Toplam" value={displayUsers.length} icon={Users} />
        <StatCard
          label="Aktif"
          value={displayUsers.filter((u) => u.isActive).length}
          tone="success"
          icon={UserCheck}
        />
        <StatCard
          label="Admin"
          value={displayUsers.filter((u) => u.role === 'Admin').length}
          tone="info"
        />
        <StatCard
          label="User"
          value={displayUsers.filter((u) => u.role === 'User').length}
          tone="violet"
        />
      </div>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <CardTitle className="text-lg">Ekip listesi</CardTitle>
              <CardDescription>
                {filteredUsers.length} / {displayUsers.length} kayıt
              </CardDescription>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row">
              <div className="relative min-w-[220px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  className="pl-9"
                  placeholder="E-posta veya telefon ara..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
              <Select
                className="min-w-[140px]"
                value={roleFilter}
                onChange={(e) => setRoleFilter(e.target.value as RoleFilter)}
              >
                <option value="All">Tüm roller</option>
                <option value="Root">Root</option>
                <option value="Admin">Admin</option>
                <option value="User">User</option>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/30 text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-6 py-3 font-medium">Kullanıcı</th>
                  <th className="px-6 py-3 font-medium">Rol</th>
                  <th className="px-6 py-3 font-medium">Durum</th>
                  <th className="px-6 py-3 font-medium">İzinler</th>
                  <th className="px-6 py-3 font-medium">İşlemler</th>
                </tr>
              </thead>
              <tbody>
                {filteredUsers.map((u) => (
                  <UserRow
                    key={u.id}
                    user={u}
                    currentUserId={currentUser?.id}
                    isMock={isMockUser(u)}
                    showingMock={showingMockFallback}
                    canManage={canManage}
                    isRoot={isRoot}
                    onEditPermissions={() => setPermissionsUser(u)}
                    onPromote={() => roleMutation.mutate({ id: u.id, role: 'Admin' })}
                    onDeactivate={() => setDeactivateTarget(u)}
                    isPending={
                      roleMutation.isPending ||
                      deactivateMutation.isPending ||
                      permissionsMutation.isPending
                    }
                  />
                ))}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      <InviteDialog
        open={inviteOpen}
        onClose={() => setInviteOpen(false)}
        isRoot={isRoot}
        isSubmitting={inviteMutation.isPending}
        onSubmit={(data) =>
          inviteMutation.mutate({
            email: data.email,
            phone: data.phone,
            role: data.role,
            permissions: data.role === 'User' ? data.permissions : undefined,
          })
        }
      />

      <EditPermissionsDialog
        user={permissionsUser}
        onClose={() => setPermissionsUser(null)}
        isSubmitting={permissionsMutation.isPending}
        onSave={(permissions) => {
          if (!permissionsUser) return
          permissionsMutation.mutate({ id: permissionsUser.id, permissions })
        }}
      />

      <ConfirmDialog
        open={deactivateTarget !== null}
        onClose={() => setDeactivateTarget(null)}
        onConfirm={() => deactivateTarget && deactivateMutation.mutate(deactivateTarget.id)}
        title="Kullanıcıyı pasife al"
        description={
          deactivateTarget
            ? `${deactivateTarget.email} hesabı pasife alınacak. Bu işlem geri alınabilir.`
            : ''
        }
        confirmLabel="Pasife al"
        variant="destructive"
        isLoading={deactivateMutation.isPending}
      />
    </div>
  )
}

function UserRow({
  user,
  currentUserId,
  isMock,
  showingMock,
  canManage,
  isRoot,
  onEditPermissions,
  onPromote,
  onDeactivate,
  isPending,
}: {
  user: TenantUser
  currentUserId?: number
  isMock: boolean
  showingMock: boolean
  canManage: boolean
  isRoot: boolean
  onEditPermissions: () => void
  onPromote: () => void
  onDeactivate: () => void
  isPending: boolean
}) {
  const isSelf = user.id === currentUserId
  const actionsDisabled = isMock || showingMock || isPending

  const permissionsDisplay =
    user.role === 'Root' || user.role === 'Admin'
      ? ['Tam yetki']
      : user.permissions.length > 0
        ? user.permissions.map(permissionLabel)
        : ['—']

  return (
    <tr
      className={cn(
        'border-b transition-colors hover:bg-muted/30',
        isMock && showingMock && 'bg-amber-50/30',
        !user.isActive && 'opacity-60',
      )}
    >
      <td className="px-6 py-4">
        <p className="font-medium">{user.email}</p>
        <p className="text-xs text-muted-foreground">{user.phone}</p>
      </td>
      <td className="px-6 py-4">
        <UserRoleBadge role={user.role} />
      </td>
      <td className="px-6 py-4">
        <Badge variant={user.isActive ? 'success' : 'muted'}>
          {user.isActive ? 'Aktif' : 'Pasif'}
        </Badge>
      </td>
      <td className="max-w-xs px-6 py-4">
        <div className="flex flex-wrap gap-1">
          {permissionsDisplay.map((p) => (
            <Badge key={p} variant="outline" className="font-normal">
              {p}
            </Badge>
          ))}
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="flex flex-wrap gap-1.5">
          {canManage && user.role === 'User' && user.isActive && (
            <Button variant="outline" size="sm" disabled={actionsDisabled} onClick={onEditPermissions}>
              <Shield className="h-3.5 w-3.5" />
              İzinler
            </Button>
          )}
          {isRoot && user.role === 'User' && user.isActive && (
            <Button variant="outline" size="sm" disabled={actionsDisabled} onClick={onPromote}>
              Admin yap
            </Button>
          )}
          {canManage && user.role !== 'Root' && user.isActive && !isSelf && (
            <Button
              variant="outline"
              size="sm"
              disabled={actionsDisabled}
              onClick={onDeactivate}
              className="text-destructive hover:text-destructive"
            >
              <UserMinus className="h-3.5 w-3.5" />
              Pasife al
            </Button>
          )}
          {isMock && showingMock && (
            <Badge variant="warning" className="font-normal">
              Demo
            </Badge>
          )}
        </div>
      </td>
    </tr>
  )
}

function InviteDialog({
  open,
  onClose,
  isRoot,
  isSubmitting,
  onSubmit,
}: {
  open: boolean
  onClose: () => void
  isRoot: boolean
  isSubmitting: boolean
  onSubmit: (data: InviteForm) => void
}) {
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors },
  } = useForm<InviteForm>({
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
    <Dialog
      open={open}
      onClose={onClose}
      title="Kullanıcı davet et"
      description="Davet edilen kullanıcıya geçici şifre bir kez gösterilir."
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
    </Dialog>
  )
}

function EditPermissionsDialog({
  user,
  onClose,
  isSubmitting,
  onSave,
}: {
  user: TenantUser | null
  onClose: () => void
  isSubmitting: boolean
  onSave: (permissions: string[]) => void
}) {
  const [selected, setSelected] = useState<string[]>([])

  useEffect(() => {
    if (user) setSelected([...user.permissions])
    else setSelected([])
  }, [user])

  const toggle = (code: string) => {
    setSelected((prev) =>
      prev.includes(code) ? prev.filter((p) => p !== code) : [...prev, code],
    )
  }

  return (
    <Dialog
      open={user !== null}
      onClose={onClose}
      title="İzinleri güncelle"
      description={
        user ? `${user.email} — değişiklik JWT yenilenene kadar eski oturumda görünmez.` : ''
      }
    >
      {user && (
        <div className="space-y-4">
          <div className="space-y-1 rounded-xl border bg-muted/20 p-2">
            {INVITE_PERMISSION_OPTIONS.map((p) => (
              <Checkbox
                key={p.code}
                id={`perm-${p.code}`}
                checked={selected.includes(p.code)}
                onChange={() => toggle(p.code)}
                label={p.label}
              />
            ))}
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={onClose}>
              İptal
            </Button>
            <Button disabled={isSubmitting || selected.length === 0} onClick={() => onSave(selected)}>
              {isSubmitting ? 'Kaydediliyor...' : 'Kaydet'}
            </Button>
          </div>
        </div>
      )}
    </Dialog>
  )
}
