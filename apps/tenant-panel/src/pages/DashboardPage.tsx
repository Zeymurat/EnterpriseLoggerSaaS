import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import {
  AlertTriangle,
  ArrowRight,
  ScrollText,
  Users,
  UserCheck,
  Activity,
} from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { getLogs, getUsers, useMockLogsOnly, useMockUsersOnly } from '@/lib/api'
import { MOCK_LOG_ENTRIES } from '@/lib/mock-logs'
import { mergeWithMockUsers } from '@/lib/mock-users'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { PageHeader, StatCard } from '@/components/layout/PageShell'
import { LogLevelBadge } from '@/components/logs/LogLevelBadge'
import { UserRoleBadge } from '@/components/users/UserRoleBadge'
import { ApiKeyManagementCard } from '@/components/settings/ApiKeyManagementCard'
import { Skeleton } from '@/components/ui/skeleton'
import { permissionLabel } from '@/lib/permissions'

function formatTimestamp(iso: string): string {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(iso))
}

export function DashboardPage() {
  const { user, can } = useAuth()

  const logsQuery = useQuery({
    queryKey: ['logs'],
    queryFn: getLogs,
    enabled: can('logs:read') && !useMockLogsOnly(),
  })

  const usersQuery = useQuery({
    queryKey: ['users'],
    queryFn: getUsers,
    enabled: can('users:read') && !useMockUsersOnly(),
  })

  const logs = useMockLogsOnly()
    ? MOCK_LOG_ENTRIES
    : logsQuery.data?.length
      ? logsQuery.data
      : logsQuery.isLoading
        ? []
        : MOCK_LOG_ENTRIES

  const users = useMockUsersOnly()
    ? mergeWithMockUsers([
        {
          id: user?.id ?? 1,
          email: user?.email ?? '',
          phone: user?.phone ?? '',
          role: user?.role ?? 'Root',
          isActive: true,
          permissions: user?.permissions ?? [],
        },
      ])
    : usersQuery.data?.length
      ? usersQuery.data
      : usersQuery.isLoading
        ? []
        : mergeWithMockUsers([
            {
              id: user?.id ?? 1,
              email: user?.email ?? '',
              phone: user?.phone ?? '',
              role: user?.role ?? 'Root',
              isActive: true,
              permissions: user?.permissions ?? [],
            },
          ])

  const errorCount = logs.filter((l) => l.logLevel === 'Error').length
  const warningCount = logs.filter((l) => l.logLevel === 'Warning').length
  const activeUsers = users.filter((u) => u.isActive).length
  const recentLogs = [...logs]
    .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime())
    .slice(0, 5)

  const isLoading = (can('logs:read') && logsQuery.isLoading) || (can('users:read') && usersQuery.isLoading)

  return (
    <div className="space-y-8">
      <PageHeader
        title={`Merhaba, ${user?.email.split('@')[0]}`}
        description={`${user?.tenantName} tenant'ına hoş geldiniz. İşte güncel özet.`}
      />

      <ApiKeyManagementCard />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {isLoading ? (
          Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-28 rounded-xl" />
          ))
        ) : (
          <>
            {can('logs:read') && (
              <>
                <StatCard label="Toplam log" value={logs.length} icon={ScrollText} />
                <StatCard
                  label="Hata"
                  value={errorCount}
                  tone="error"
                  icon={AlertTriangle}
                  hint={warningCount > 0 ? `${warningCount} uyarı` : undefined}
                />
              </>
            )}
            {can('users:read') && (
              <>
                <StatCard label="Kullanıcı" value={users.length} icon={Users} tone="violet" />
                <StatCard
                  label="Aktif kullanıcı"
                  value={activeUsers}
                  tone="success"
                  icon={UserCheck}
                />
              </>
            )}
            {!can('logs:read') && !can('users:read') && (
              <StatCard label="Oturum" value="Aktif" icon={Activity} tone="success" />
            )}
          </>
        )}
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        <Card className="lg:col-span-2">
          <CardHeader className="flex flex-row items-center justify-between space-y-0">
            <div>
              <CardTitle className="text-lg">Son loglar</CardTitle>
              <CardDescription>En güncel 5 kayıt</CardDescription>
            </div>
            {can('logs:read') && (
              <Button variant="ghost" size="sm" asChild>
                <Link to="/logs">
                  Tümünü gör
                  <ArrowRight className="h-4 w-4" />
                </Link>
              </Button>
            )}
          </CardHeader>
          <CardContent>
            {!can('logs:read') ? (
              <p className="py-8 text-center text-sm text-muted-foreground">
                Log özeti için <code className="text-xs">logs:read</code> izni gerekir.
              </p>
            ) : isLoading ? (
              <div className="space-y-3">
                {Array.from({ length: 5 }).map((_, i) => (
                  <Skeleton key={i} className="h-12 w-full" />
                ))}
              </div>
            ) : recentLogs.length === 0 ? (
              <p className="py-8 text-center text-sm text-muted-foreground">Henüz log kaydı yok.</p>
            ) : (
              <div className="space-y-2">
                {recentLogs.map((log) => (
                  <div
                    key={log.id}
                    className="flex items-start gap-3 rounded-lg border bg-muted/20 px-4 py-3 transition-colors hover:bg-muted/40"
                  >
                    <LogLevelBadge level={log.logLevel} />
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium">{log.message}</p>
                      <p className="text-xs text-muted-foreground">
                        {log.applicationName} · {formatTimestamp(log.timestamp)}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Hesabınız</CardTitle>
              <CardDescription>Oturum bilgileri</CardDescription>
            </CardHeader>
            <CardContent className="space-y-3 text-sm">
              <div className="flex items-center justify-between gap-2">
                <span className="text-muted-foreground">E-posta</span>
                <span className="truncate font-medium">{user?.email}</span>
              </div>
              <div className="flex items-center justify-between gap-2">
                <span className="text-muted-foreground">Şirket</span>
                <span className="font-medium">{user?.tenantName}</span>
              </div>
              <div className="flex items-center justify-between gap-2">
                <span className="text-muted-foreground">Rol</span>
                <UserRoleBadge role={user?.role ?? 'User'} />
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-lg">Hızlı erişim</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2">
              {can('logs:read') && (
                <Button variant="outline" className="w-full justify-between" asChild>
                  <Link to="/logs">
                    Logları incele
                    <ScrollText className="h-4 w-4" />
                  </Link>
                </Button>
              )}
              {can('users:read') && (
                <Button variant="outline" className="w-full justify-between" asChild>
                  <Link to="/users">
                    Kullanıcıları yönet
                    <Users className="h-4 w-4" />
                  </Link>
                </Button>
              )}
            </CardContent>
          </Card>

          {user && user.permissions.length > 0 && user.role === 'User' && (
            <Card>
              <CardHeader>
                <CardTitle className="text-lg">İzinleriniz</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="flex flex-wrap gap-1.5">
                  {user.permissions.map((p) => (
                    <span
                      key={p}
                      className="rounded-md bg-muted px-2 py-1 text-xs text-muted-foreground"
                    >
                      {permissionLabel(p)}
                    </span>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
    </div>
  )
}
