import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  AlertCircle,
  RefreshCw,
  Search,
  Sparkles,
  UserMinus,
  UserPlus,
  Shield,
} from 'lucide-react'
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
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { UserRoleBadge } from '@/components/users/UserRoleBadge'
import { SimpleDialog } from '@/components/users/SimpleDialog'
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
    onError: (err: Error) => toast.error(err.message),
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
      queryClient.invalidateQueries({ queryKey: ['users'] })
      toast.success('Kullanıcı pasife alındı')
    },
    onError: (err: Error) => toast.error(err.message),
  })

  const canInvite = can('users:invite')
  const canManage = can('users:manage')
  const isRoot = currentUser?.role === 'Root'

  if (!can('users:read')) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <AlertCircle className="h-5 w-5 text-destructive" />
            Erişim reddedildi
          </CardTitle>
          <CardDescription>
            Kullanıcı listesi için <code className="text-xs">users:read</code> izni gerekir.
          </CardDescription>
        </CardHeader>
      </Card>
    )
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Users</h1>
          <p className="text-muted-foreground">Ekip üyelerini davet edin ve yönetin.</p>
        </div>
        <div className="flex flex-wrap gap-2">
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
              Kullanıcı davet et
            </Button>
          )}
        </div>
      </div>

      {showingMockFallback && (
        <div className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
          <Sparkles className="mt-0.5 h-4 w-4 shrink-0" />
          <div>
            <p className="font-medium">Demo kullanıcılar gösteriliyor</p>
            <p className="text-amber-800/90">
              {dataSource === 'mock-fallback'
                ? 'Tenant\'ta yalnızca Root var — örnek Admin/User satırları eklendi. Davet ile gerçek kullanıcı ekleyebilirsiniz.'
                : 'Mock mod veya API yüklenemedi — demo satırlar salt okunur.'}
            </p>
          </div>
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Toplam" value={displayUsers.length} />
        <StatCard label="Aktif" value={displayUsers.filter((u) => u.isActive).length} />
        <StatCard label="Admin" value={displayUsers.filter((u) => u.role === 'Admin').length} />
        <StatCard label="User" value={displayUsers.filter((u) => u.role === 'User').length} />
      </div>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <CardTitle className="text-lg">Kullanıcılar</CardTitle>
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
              <select
                className="h-10 rounded-md border border-input bg-background px-3 text-sm"
                value={roleFilter}
                onChange={(e) => setRoleFilter(e.target.value as RoleFilter)}
              >
                <option value="All">Tüm roller</option>
                <option value="Root">Root</option>
                <option value="Admin">Admin</option>
                <option value="User">User</option>
              </select>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/40 text-left text-muted-foreground">
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
                    onDeactivate={() => {
                      if (confirm(`${u.email} pasife alınsın mı?`)) {
                        deactivateMutation.mutate(u.id)
                      }
                    }}
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
        onSubmit={(data) => inviteMutation.mutate({
          email: data.email,
          phone: data.phone,
          role: data.role,
          permissions: data.role === 'User' ? data.permissions : undefined,
        })}
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
        'border-b transition-colors hover:bg-muted/20',
        isMock && showingMock && 'bg-amber-50/40',
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
        <span
          className={cn(
            'inline-flex rounded-full px-2 py-0.5 text-xs font-medium',
            user.isActive ? 'bg-emerald-100 text-emerald-800' : 'bg-muted text-muted-foreground',
          )}
        >
          {user.isActive ? 'Aktif' : 'Pasif'}
        </span>
      </td>
      <td className="max-w-xs px-6 py-4">
        <div className="flex flex-wrap gap-1">
          {permissionsDisplay.map((p) => (
            <span
              key={p}
              className="rounded-md bg-muted px-1.5 py-0.5 text-xs text-muted-foreground"
            >
              {p}
            </span>
          ))}
        </div>
      </td>
      <td className="px-6 py-4">
        <div className="flex flex-wrap gap-1">
          {canManage && user.role === 'User' && user.isActive && (
            <Button
              variant="outline"
              size="sm"
              disabled={actionsDisabled}
              onClick={onEditPermissions}
            >
              <Shield className="h-3.5 w-3.5" />
              İzinler
            </Button>
          )}
          {isRoot && user.role === 'User' && user.isActive && (
            <Button
              variant="outline"
              size="sm"
              disabled={actionsDisabled}
              onClick={onPromote}
            >
              Admin yap
            </Button>
          )}
          {canManage &&
            user.role !== 'Root' &&
            user.isActive &&
            !isSelf && (
              <Button
                variant="outline"
                size="sm"
                disabled={actionsDisabled}
                onClick={onDeactivate}
              >
                <UserMinus className="h-3.5 w-3.5" />
                Pasife al
              </Button>
            )}
          {isMock && showingMock && (
            <span className="text-xs text-muted-foreground">Demo</span>
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
    if (open) {
      reset(INVITE_FORM_DEFAULTS)
    }
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
    <SimpleDialog
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
          <select
            id="invite-role"
            className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
            {...register('role')}
          >
            <option value="User">User</option>
            {isRoot && <option value="Admin">Admin</option>}
          </select>
        </div>

        {role === 'User' && (
          <div className="space-y-2">
            <Label>İzinler</Label>
            <div className="space-y-2 rounded-md border p-3">
              {INVITE_PERMISSION_OPTIONS.map((p) => (
                <label key={p.code} className="flex cursor-pointer items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={selectedPermissions.includes(p.code)}
                    onChange={() => togglePermission(p.code)}
                  />
                  {p.label}
                  <span className="text-xs text-muted-foreground">({p.code})</span>
                </label>
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
    </SimpleDialog>
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
    <SimpleDialog
      open={user !== null}
      onClose={onClose}
      title="İzinleri güncelle"
      description={
        user
          ? `${user.email} — değişiklik JWT yenilenene kadar eski oturumda görünmez.`
          : ''
      }
    >
      {user && (
        <div className="space-y-4">
          <div className="space-y-2 rounded-md border p-3">
            {INVITE_PERMISSION_OPTIONS.map((p) => (
              <label key={p.code} className="flex cursor-pointer items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={selected.includes(p.code)}
                  onChange={() => toggle(p.code)}
                />
                {p.label}
              </label>
            ))}
          </div>
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={onClose}>
              İptal
            </Button>
            <Button
              disabled={isSubmitting || selected.length === 0}
              onClick={() => onSave(selected)}
            >
              {isSubmitting ? 'Kaydediliyor...' : 'Kaydet'}
            </Button>
          </div>
        </div>
      )}
    </SimpleDialog>
  )
}

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <Card>
      <CardContent className="pt-6">
        <p className="text-sm text-muted-foreground">{label}</p>
        <p className="text-3xl font-bold tabular-nums">{value}</p>
      </CardContent>
    </Card>
  )
}
