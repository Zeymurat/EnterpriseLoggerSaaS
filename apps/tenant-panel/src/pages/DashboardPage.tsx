import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { ArrowRight, RefreshCw, ScrollText, Users, UserCheck, Activity } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { getLogs, getUsers, type LogEntry } from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ApiErrorCard } from '@/components/layout/ApiErrorCard'
import { EmptyState, PageHeader } from '@/components/layout/PageShell'
import { LogLevelBadge } from '@/components/logs/LogLevelBadge'
import { LogCorrelationTraceButton } from '@/components/logs/LogCorrelationTraceButton'
import { LogDetailSheet } from '@/components/logs/LogDetailSheet'
import { LogsOnboardingCard } from '@/components/logs/LogsOnboardingCard'
import { LogsStatsGrid } from '@/components/logs/LogsStatsGrid'
import { LogsTimeRangeSelect } from '@/components/logs/LogsTimeRangeSelect'
import { UserRoleBadge } from '@/components/users/UserRoleBadge'
import { ApiKeyManagementCard } from '@/components/settings/ApiKeyManagementCard'
import { Skeleton } from '@/components/ui/skeleton'
import { permissionLabel } from '@/lib/permissions'
import { isTenantWithoutLogs } from '@/lib/log-tenant-state'
import { buildLogsTracePath } from '@/lib/logs-trace'
import {
  createDefaultLogsFilters,
  isRelativeTimePreset,
  quickDatePresetToRange,
  resolveLogsDateFilter,
  shouldUseLiveRefresh,
  type QuickDatePreset,
} from '@/lib/log-date-filters'
import { cn } from '@/lib/utils'

const LIVE_REFRESH_MS = 30_000

function formatTimestamp(iso: string): string {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(iso))
}

function buildDashboardFilters(quickDate: QuickDatePreset) {
  const base = createDefaultLogsFilters()

  if (quickDate === 'all') {
    return { ...base, quickDate: 'all' as const, fromDate: '', toDate: '' }
  }

  if (isRelativeTimePreset(quickDate)) {
    return { ...base, quickDate, fromDate: '', toDate: '' }
  }

  const range = quickDatePresetToRange(quickDate)
  return {
    ...base,
    quickDate,
    fromDate: range?.from ?? '',
    toDate: range?.to ?? '',
  }
}

export function DashboardPage() {
  const { user, can } = useAuth()
  const navigate = useNavigate()
  const [quickDate, setQuickDate] = useState<QuickDatePreset>('today')
  const [selectedLog, setSelectedLog] = useState<LogEntry | null>(null)
  const filters = useMemo(() => buildDashboardFilters(quickDate), [quickDate])

  const logsQuery = useQuery({
    queryKey: ['logs', 'dashboard', quickDate, filters.fromDate, filters.toDate],
    queryFn: () => {
      const resolved = resolveLogsDateFilter(filters)
      return getLogs({
        page: 1,
        pageSize: 5,
        from: resolved.apply ? resolved.from : undefined,
        to: resolved.apply ? resolved.to : undefined,
      })
    },
    enabled: can('logs:read'),
    refetchInterval: shouldUseLiveRefresh(quickDate) ? LIVE_REFRESH_MS : false,
  })

  const usersQuery = useQuery({
    queryKey: ['users'],
    queryFn: getUsers,
    enabled: can('users:read'),
  })

  const logs = logsQuery.data?.items ?? []
  const summary = logsQuery.data?.summary
  const overallSummary = logsQuery.data?.overallSummary
  const isDateFiltered = logsQuery.data?.isDateFiltered ?? false
  const users = usersQuery.data ?? []
  const activeUsers = users.filter((u) => u.isActive).length

  const isLoading =
    (can('logs:read') && logsQuery.isLoading) || (can('users:read') && usersQuery.isLoading)

  const logsDataReady = !!logsQuery.data && !logsQuery.isLoading
  const tenantHasNoLogs = isTenantWithoutLogs(
    summary,
    overallSummary,
    isDateFiltered,
    logsDataReady,
  )

  return (
    <div className="space-y-8">
      <PageHeader
        title={`Merhaba, ${user?.email.split('@')[0]}`}
        description={`${user?.tenantName} tenant'ına hoş geldiniz. İşte güncel özet.`}
        actions={
          can('logs:read') ? (
            <Button
              variant="outline"
              size="sm"
              onClick={() => logsQuery.refetch()}
              disabled={logsQuery.isFetching}
            >
              <RefreshCw className={cn('h-4 w-4', logsQuery.isFetching && 'animate-spin')} />
              Yenile
            </Button>
          ) : undefined
        }
      />

      <ApiKeyManagementCard />

      {can('logs:read') && (
        <div className="space-y-4">
          {logsQuery.isError && (
            <ApiErrorCard
              title="Log özeti yüklenemedi"
              error={logsQuery.error}
              onRetry={() => logsQuery.refetch()}
              isRetrying={logsQuery.isFetching}
            />
          )}

          {!logsQuery.isError && tenantHasNoLogs && <LogsOnboardingCard />}

          {!logsQuery.isError && !tenantHasNoLogs && (
            <>
              <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
                <div className="min-w-0 space-y-1.5 sm:max-w-xs">
                  <p className="text-xs font-medium text-muted-foreground">Zaman aralığı</p>
                  <LogsTimeRangeSelect value={quickDate} onChange={setQuickDate} />
                </div>
                {shouldUseLiveRefresh(quickDate) && (
                  <p className="text-xs text-muted-foreground">Canlı aralık — 30 saniyede bir yenilenir.</p>
                )}
              </div>

              <LogsStatsGrid
                summary={summary}
                overallSummary={overallSummary}
                isDateFiltered={isDateFiltered}
                isLoading={logsQuery.isLoading && !logsQuery.data}
              />
            </>
          )}
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {isLoading ? (
          Array.from({ length: 2 }).map((_, i) => (
            <Skeleton key={i} className="h-28 rounded-xl" />
          ))
        ) : (
          <>
            {can('users:read') && (
              <>
                <Card className="p-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm text-muted-foreground">Kullanıcı</p>
                      <p className="text-2xl font-semibold">{users.length}</p>
                    </div>
                    <Users className="h-8 w-8 text-status-accent" />
                  </div>
                </Card>
                <Card className="p-4">
                  <div className="flex items-center justify-between gap-3">
                    <div>
                      <p className="text-sm text-muted-foreground">Aktif kullanıcı</p>
                      <p className="text-2xl font-semibold">{activeUsers}</p>
                    </div>
                    <UserCheck className="h-8 w-8 text-status-success" />
                  </div>
                </Card>
              </>
            )}
            {!can('logs:read') && !can('users:read') && (
              <Card className="p-4">
                <div className="flex items-center justify-between gap-3">
                  <div>
                    <p className="text-sm text-muted-foreground">Oturum</p>
                    <p className="text-2xl font-semibold">Aktif</p>
                  </div>
                  <Activity className="h-8 w-8 text-status-success" />
                </div>
              </Card>
            )}
          </>
        )}
      </div>

      <div
        className={cn(
          'grid gap-6',
          can('logs:read') && !tenantHasNoLogs ? 'lg:grid-cols-3' : 'sm:grid-cols-2 lg:grid-cols-3',
        )}
      >
        {can('logs:read') && !tenantHasNoLogs && (
          <Card className="lg:col-span-2">
            <CardHeader className="flex flex-row items-center justify-between space-y-0">
              <div>
                <CardTitle className="text-lg">Son loglar</CardTitle>
                <CardDescription>Seçili aralıktaki en güncel 5 kayıt</CardDescription>
              </div>
              <Button variant="ghost" size="sm" asChild>
                <Link to="/logs">
                  Tümünü gör
                  <ArrowRight className="h-4 w-4" />
                </Link>
              </Button>
            </CardHeader>
            <CardContent>
              {logsQuery.isLoading && !logsQuery.data ? (
                <div className="space-y-3">
                  {Array.from({ length: 5 }).map((_, i) => (
                    <Skeleton key={i} className="h-12 w-full" />
                  ))}
                </div>
              ) : logs.length === 0 ? (
                <EmptyState
                  title="Seçili zaman aralığında log yok"
                  description="Farklı bir zaman aralığı seçmeyi deneyin veya tüm logları görmek için Loglar sayfasına gidin."
                  action={
                    <Button variant="outline" size="sm" asChild>
                      <Link to="/logs">
                        Logları aç
                        <ArrowRight className="h-4 w-4" />
                      </Link>
                    </Button>
                  }
                />
              ) : (
                <div className="space-y-2">
                  {logs.map((log) => (
                    <div
                      key={log.id}
                      className="flex items-start gap-2 rounded-xl border border-border/50 bg-card/60 px-4 py-3 transition-colors hover:border-primary/20 hover:bg-primary/[0.04]"
                    >
                      <button
                        type="button"
                        onClick={() => setSelectedLog(log)}
                        className="flex min-w-0 flex-1 items-start gap-3 text-left"
                      >
                        <LogLevelBadge level={log.logLevel} />
                        <div className="min-w-0 flex-1">
                          <p className="truncate text-sm font-medium">{log.message}</p>
                          <p className="text-xs text-muted-foreground">
                            {log.applicationName} · {formatTimestamp(log.timestamp)}
                          </p>
                        </div>
                      </button>
                      <LogCorrelationTraceButton
                        correlationId={log.correlationId ?? ''}
                        correlationLogCount={log.correlationLogCount}
                        onTrace={(correlationId) => navigate(buildLogsTracePath(correlationId))}
                        className="mt-0.5"
                      />
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        )}

        <div
          className={cn(
            'space-y-6',
            can('logs:read') && tenantHasNoLogs && 'sm:col-span-2 lg:col-span-3',
          )}
        >
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

      <LogDetailSheet log={selectedLog} onClose={() => setSelectedLog(null)} />
    </div>
  )
}
