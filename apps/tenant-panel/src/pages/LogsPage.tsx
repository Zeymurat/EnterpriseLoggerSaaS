import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { AlertTriangle, Database, RefreshCw, ScrollText, Search } from 'lucide-react'
import { createLog, getLogs, useMockLogsOnly, type LogEntry } from '@/lib/api'
import { DEMO_LOG_PAYLOADS, isMockLogEntry, MOCK_LOG_ENTRIES } from '@/lib/mock-logs'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { LogLevelBadge } from '@/components/logs/LogLevelBadge'
import { LogDetailSheet } from '@/components/logs/LogDetailSheet'
import {
  AccessDeniedCard,
  EmptyState,
  MockDataBanner,
  PageHeader,
  StatCard,
} from '@/components/layout/PageShell'
import { cn } from '@/lib/utils'

type LevelFilter = 'All' | 'Info' | 'Warning' | 'Error'

function formatTimestamp(iso: string): string {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'short',
    timeStyle: 'medium',
  }).format(new Date(iso))
}

function filterLogs(logs: LogEntry[], level: LevelFilter, search: string): LogEntry[] {
  const query = search.trim().toLowerCase()

  return logs.filter((log) => {
    if (level !== 'All' && log.logLevel !== level) return false
    if (!query) return true

    return (
      log.message.toLowerCase().includes(query) ||
      log.applicationName.toLowerCase().includes(query) ||
      log.logLevel.toLowerCase().includes(query)
    )
  })
}

function countByLevel(logs: LogEntry[], level: string): number {
  return logs.filter((l) => l.logLevel === level).length
}

export function LogsPage() {
  const { can } = useAuth()
  const queryClient = useQueryClient()
  const [levelFilter, setLevelFilter] = useState<LevelFilter>('All')
  const [search, setSearch] = useState('')
  const [selectedLog, setSelectedLog] = useState<LogEntry | null>(null)

  const logsQuery = useQuery({
    queryKey: ['logs'],
    queryFn: getLogs,
    enabled: can('logs:read') && !useMockLogsOnly(),
  })

  const seedMutation = useMutation({
    mutationFn: async () => {
      for (const payload of DEMO_LOG_PAYLOADS) {
        await createLog(payload)
      }
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['logs'] }),
  })

  const { displayLogs, showingMockFallback, dataSource } = useMemo(() => {
    if (useMockLogsOnly()) {
      return {
        displayLogs: MOCK_LOG_ENTRIES,
        showingMockFallback: true,
        dataSource: 'mock' as const,
      }
    }

    if (logsQuery.isLoading || logsQuery.isError) {
      return {
        displayLogs: MOCK_LOG_ENTRIES,
        showingMockFallback: true,
        dataSource: 'mock' as const,
      }
    }

    const apiLogs = logsQuery.data ?? []
    if (apiLogs.length === 0) {
      return {
        displayLogs: MOCK_LOG_ENTRIES,
        showingMockFallback: true,
        dataSource: 'mock-fallback' as const,
      }
    }

    return {
      displayLogs: apiLogs,
      showingMockFallback: false,
      dataSource: 'api' as const,
    }
  }, [logsQuery.data, logsQuery.isLoading, logsQuery.isError])

  const filteredLogs = useMemo(
    () => filterLogs(displayLogs, levelFilter, search),
    [displayLogs, levelFilter, search],
  )

  if (!can('logs:read')) {
    return <AccessDeniedCard permission="logs:read" />
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Loglar"
        description="Tenant loglarını izleyin, filtreleyin ve analiz edin."
        actions={
          <>
            <Button
              variant="outline"
              size="sm"
              onClick={() => logsQuery.refetch()}
              disabled={useMockLogsOnly() || logsQuery.isFetching}
            >
              <RefreshCw className={cn('h-4 w-4', logsQuery.isFetching && 'animate-spin')} />
              Yenile
            </Button>
            {can('logs:write') && dataSource !== 'mock' && (
              <Button
                size="sm"
                onClick={() => seedMutation.mutate()}
                disabled={seedMutation.isPending}
              >
                <Database className="h-4 w-4" />
                {seedMutation.isPending ? 'Yükleniyor...' : 'Demo yükle'}
              </Button>
            )}
          </>
        }
      />

      {showingMockFallback && (
        <MockDataBanner
          title="Demo verisi gösteriliyor"
          description={
            dataSource === 'mock-fallback'
              ? "API'de henüz log yok — örnek kayıtlar listeleniyor. Demo yükle ile gerçek veriye dönüştürebilirsiniz."
              : 'VITE_USE_MOCK_LOGS=true — yalnızca mock veri modu aktif.'
          }
        />
      )}

      {logsQuery.isError && !useMockLogsOnly() && (
        <Alert variant="destructive">
          <AlertTitle>API hatası</AlertTitle>
          <AlertDescription>
            {(logsQuery.error as Error).message} — demo veri gösteriliyor.
          </AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Toplam" value={displayLogs.length} icon={ScrollText} />
        <StatCard label="Info" value={countByLevel(displayLogs, 'Info')} tone="info" />
        <StatCard label="Warning" value={countByLevel(displayLogs, 'Warning')} tone="warning" />
        <StatCard
          label="Error"
          value={countByLevel(displayLogs, 'Error')}
          tone="error"
          icon={AlertTriangle}
        />
      </div>

      <Card>
        <CardHeader className="pb-4">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <CardTitle className="text-lg">Log kayıtları</CardTitle>
              <CardDescription>
                {filteredLogs.length} / {displayLogs.length} kayıt gösteriliyor
              </CardDescription>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
              <div className="relative min-w-[220px]">
                <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
                <Input
                  className="pl-9"
                  placeholder="Mesaj veya uygulama ara..."
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                />
              </div>
              <Select
                className="min-w-[160px]"
                value={levelFilter}
                onChange={(e) => setLevelFilter(e.target.value as LevelFilter)}
              >
                <option value="All">Tüm seviyeler</option>
                <option value="Info">Info</option>
                <option value="Warning">Warning</option>
                <option value="Error">Error</option>
              </Select>
            </div>
          </div>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/30 text-left text-xs uppercase tracking-wide text-muted-foreground">
                  <th className="px-6 py-3 font-medium">Zaman</th>
                  <th className="px-6 py-3 font-medium">Seviye</th>
                  <th className="px-6 py-3 font-medium">Uygulama</th>
                  <th className="px-6 py-3 font-medium">Mesaj</th>
                </tr>
              </thead>
              <tbody>
                {filteredLogs.length === 0 ? (
                  <tr>
                    <td colSpan={4}>
                      <EmptyState
                        title="Filtreye uygun log bulunamadı"
                        description="Arama veya seviye filtresini değiştirmeyi deneyin."
                      />
                    </td>
                  </tr>
                ) : (
                  filteredLogs.map((log) => (
                    <tr
                      key={log.id}
                      onClick={() => setSelectedLog(log)}
                      className={cn(
                        'cursor-pointer border-b transition-colors hover:bg-muted/30',
                        isMockLogEntry(log) && showingMockFallback && 'bg-amber-50/30',
                      )}
                    >
                      <td className="whitespace-nowrap px-6 py-3.5 font-mono text-xs text-muted-foreground">
                        {formatTimestamp(log.timestamp)}
                      </td>
                      <td className="px-6 py-3.5">
                        <LogLevelBadge level={log.logLevel} />
                      </td>
                      <td className="px-6 py-3.5 font-medium">{log.applicationName}</td>
                      <td className="max-w-md px-6 py-3.5 text-muted-foreground">{log.message}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>

      <LogDetailSheet log={selectedLog} onClose={() => setSelectedLog(null)} />
    </div>
  )
}
