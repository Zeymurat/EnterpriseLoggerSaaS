import { useEffect, useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { RefreshCw, Search, Shield, UserCheck, UserMinus, UserPlus, Users } from 'lucide-react'
import {
  deactivateUser,
  getUsers,
  inviteUser,
  updateUserPermissions,
  updateUserRole,
  type InviteUserRequest,
  type TenantUser,
} from '@/lib/api'
import { INVITE_PERMISSION_OPTIONS, permissionLabel } from '@/lib/permissions'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Checkbox } from '@/components/ui/checkbox'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Dialog, ConfirmDialog } from '@/components/ui/dialog'
import { InviteSheet } from '@/components/users/InviteSheet'
import { UserRoleBadge } from '@/components/users/UserRoleBadge'
import {
  AccessDeniedCard,
  EmptyState,
  PageHeader,
  StatCard,
} from '@/components/layout/PageShell'
import { ApiErrorCard } from '@/components/layout/ApiErrorCard'
import { Skeleton } from '@/components/ui/skeleton'
import { toast } from '@/components/ui/sonner'
import { cn } from '@/lib/utils'

type RoleFilter = 'All' | 'Root' | 'Admin' | 'User'

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
    enabled: can('users:read'),
  })

  const users = usersQuery.data ?? []

  const filteredUsers = useMemo(
    () => filterUsers(users, roleFilter, search),
    [users, roleFilter, search],
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
  const isPending =
    roleMutation.isPending || deactivateMutation.isPending || permissionsMutation.isPending

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
              disabled={usersQuery.isFetching}
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

      {usersQuery.isError && (
        <ApiErrorCard
          title="Kullanıcı listesi yüklenemedi"
          error={usersQuery.error}
          onRetry={() => usersQuery.refetch()}
          isRetrying={usersQuery.isFetching}
        />
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {usersQuery.isLoading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-28 rounded-xl" />
          ))
        ) : (
          <>
            <StatCard label="Toplam" value={users.length} icon={Users} />
            <StatCard
              label="Aktif"
              value={users.filter((u) => u.isActive).length}
              tone="success"
              icon={UserCheck}
            />
            <StatCard
              label="Admin"
              value={users.filter((u) => u.role === 'Admin').length}
              tone="info"
            />
            <StatCard
              label="User"
              value={users.filter((u) => u.role === 'User').length}
              tone="violet"
            />
          </>
        )}
      </div>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <CardTitle className="text-lg">Ekip listesi</CardTitle>
              <CardDescription>
                {usersQuery.isLoading
                  ? 'Yükleniyor...'
                  : `${filteredUsers.length} / ${users.length} kayıt`}
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
                  disabled={usersQuery.isLoading}
                />
              </div>
              <Select
                className="min-w-[140px]"
                value={roleFilter}
                onChange={(e) => setRoleFilter(e.target.value as RoleFilter)}
                disabled={usersQuery.isLoading}
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
                {usersQuery.isLoading ? (
                  Array.from({ length: 5 }).map((_, i) => (
                    <tr key={i} className="border-b">
                      <td colSpan={5} className="px-6 py-3">
                        <Skeleton className="h-12 w-full" />
                      </td>
                    </tr>
                  ))
                ) : users.length === 0 ? (
                  <tr>
                    <td colSpan={5}>
                      <EmptyState
                        title="Henüz kullanıcı yok"
                        description="Root hesabı dışında ekip üyesi eklemek için Davet et butonunu kullanın."
                      />
                    </td>
                  </tr>
                ) : filteredUsers.length === 0 ? (
                  <tr>
                    <td colSpan={5}>
                      <EmptyState
                        title="Filtreye uygun kullanıcı bulunamadı"
                        description="Arama veya rol filtresini değiştirmeyi deneyin."
                      />
                    </td>
                  </tr>
                ) : (
                  filteredUsers.map((u) => (
                    <UserRow
                      key={u.id}
                      user={u}
                      currentUserId={currentUser?.id}
                      canManage={canManage}
                      isRoot={isRoot}
                      isPending={isPending}
                      onEditPermissions={() => setPermissionsUser(u)}
                      onPromote={() => roleMutation.mutate({ id: u.id, role: 'Admin' })}
                      onDeactivate={() => setDeactivateTarget(u)}
                    />
                  ))
                )}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      <InviteSheet
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
  canManage,
  isRoot,
  isPending,
  onEditPermissions,
  onPromote,
  onDeactivate,
}: {
  user: TenantUser
  currentUserId?: number
  canManage: boolean
  isRoot: boolean
  isPending: boolean
  onEditPermissions: () => void
  onPromote: () => void
  onDeactivate: () => void
}) {
  const isSelf = user.id === currentUserId

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
            <Button variant="outline" size="sm" disabled={isPending} onClick={onEditPermissions}>
              <Shield className="h-3.5 w-3.5" />
              İzinler
            </Button>
          )}
          {isRoot && user.role === 'User' && user.isActive && (
            <Button variant="outline" size="sm" disabled={isPending} onClick={onPromote}>
              Admin yap
            </Button>
          )}
          {canManage && user.role !== 'Root' && user.isActive && !isSelf && (
            <Button
              variant="outline"
              size="sm"
              disabled={isPending}
              onClick={onDeactivate}
              className="text-destructive hover:text-destructive"
            >
              <UserMinus className="h-3.5 w-3.5" />
              Pasife al
            </Button>
          )}
        </div>
      </td>
    </tr>
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
