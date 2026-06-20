import { useEffect, useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Download, RefreshCw } from 'lucide-react'
import { exportLogs, getLogs, type LogEntry } from '@/lib/api'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Select } from '@/components/ui/select'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { LogLevelBadge } from '@/components/logs/LogLevelBadge'
import { LogDetailSheet } from '@/components/logs/LogDetailSheet'
import { LogExportScopeDialog, getQuickDateLabel } from '@/components/logs/LogExportScopeDialog'
import { LogsStatsGrid } from '@/components/logs/LogsStatsGrid'
import { LogsFilterBar, type LogsFilterState, type QuickDatePreset } from '@/components/logs/LogsFilterBar'
import {
  buildExportLogsParams,
  createDefaultLogsFilters,
  isRelativeTimePreset,
  quickDatePresetToRange,
  resolveLogsDateFilter,
  shouldUseLiveRefresh,
  type LogExportScope,
} from '@/lib/log-date-filters'
import { AccessDeniedCard, EmptyState, PageHeader } from '@/components/layout/PageShell'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'

const PAGE_SIZE_OPTIONS = [25, 50, 100] as const
const LIVE_REFRESH_MS = 30_000

function formatTimestamp(iso: string): string {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'short',
    timeStyle: 'medium',
  }).format(new Date(iso))
}

function buildGetLogsParams(
  filters: LogsFilterState,
  search: string,
  page: number,
  pageSize: number,
) {
  const dateFilter = resolveLogsDateFilter(filters)

  return {
    page,
    pageSize,
    search,
    logLevels: filters.logLevels,
    httpMethods: filters.httpMethods,
    statusCodes: filters.statusCodes.map(Number),
    applicationNames: filters.applicationNames,
    from: dateFilter.apply ? dateFilter.from : undefined,
    to: dateFilter.apply ? dateFilter.to : undefined,
  }
}

function isSameFilterState(a: LogsFilterState, b: LogsFilterState): boolean {
  return (
    a.searchInput === b.searchInput &&
    a.quickDate === b.quickDate &&
    a.fromDate === b.fromDate &&
    a.toDate === b.toDate &&
    a.logLevels.join() === b.logLevels.join() &&
    a.httpMethods.join() === b.httpMethods.join() &&
    a.statusCodes.join() === b.statusCodes.join() &&
    a.applicationNames.join() === b.applicationNames.join()
  )
}

export function LogsPage() {
  const { can } = useAuth()
  const defaultFilters = useMemo(() => createDefaultLogsFilters(), [])
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState<(typeof PAGE_SIZE_OPTIONS)[number]>(25)
  const [filters, setFilters] = useState<LogsFilterState>(defaultFilters)
  const [search, setSearch] = useState('')
  const [selectedLog, setSelectedLog] = useState<LogEntry | null>(null)
  const [isExporting, setIsExporting] = useState(false)
  const [exportNotice, setExportNotice] = useState<string | null>(null)
  const [exportDialogOpen, setExportDialogOpen] = useState(false)

  useEffect(() => {
    const timer = window.setTimeout(() => setSearch(filters.searchInput), 300)
    return () => window.clearTimeout(timer)
  }, [filters.searchInput])

  useEffect(() => {
    setPage(1)
  }, [
    search,
    filters.logLevels,
    filters.httpMethods,
    filters.statusCodes,
    filters.applicationNames,
    filters.fromDate,
    filters.toDate,
    filters.quickDate,
    pageSize,
  ])

  const logsQuery = useQuery({
    queryKey: [
      'logs',
      page,
      pageSize,
      search,
      filters.logLevels,
      filters.httpMethods,
      filters.statusCodes,
      filters.applicationNames,
      filters.quickDate,
      filters.fromDate,
      filters.toDate,
    ],
    queryFn: () => getLogs(buildGetLogsParams(filters, search, page, pageSize)),
    enabled: can('logs:read'),
    placeholderData: (previous) => previous,
    refetchInterval: shouldUseLiveRefresh(filters.quickDate) ? LIVE_REFRESH_MS : false,
  })

  const logs = logsQuery.data?.items ?? []
  const filterOptions = logsQuery.data?.availableFilters
  const totalCount = logsQuery.data?.totalCount ?? 0
  const summary = logsQuery.data?.summary
  const overallSummary = logsQuery.data?.overallSummary
  const isDateFiltered = logsQuery.data?.isDateFiltered ?? false
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  const pageNumbers = useMemo(() => {
    const maxButtons = 5
    let start = Math.max(1, page - Math.floor(maxButtons / 2))
    const end = Math.min(totalPages, start + maxButtons - 1)
    start = Math.max(1, end - maxButtons + 1)
    return Array.from({ length: end - start + 1 }, (_, i) => start + i)
  }, [page, totalPages])

  const hasActiveFilters = !isSameFilterState(filters, defaultFilters) || search.length > 0

  const handleQuickDateChange = (preset: QuickDatePreset) => {
    if (preset === 'all') {
      setFilters((current) => ({
        ...current,
        quickDate: 'all',
        fromDate: '',
        toDate: '',
      }))
      return
    }

    if (preset === '') {
      setFilters((current) => ({ ...current, quickDate: '' }))
      return
    }

    if (isRelativeTimePreset(preset)) {
      setFilters((current) => ({
        ...current,
        quickDate: preset,
        fromDate: '',
        toDate: '',
      }))
      return
    }

    const range = quickDatePresetToRange(preset)
    if (!range) return

    setFilters((current) => ({
      ...current,
      quickDate: preset,
      fromDate: range.from,
      toDate: range.to,
    }))
  }

  const handleFromDateChange = (value: string) => {
    setFilters((current) => ({ ...current, fromDate: value, quickDate: '' }))
  }

  const handleToDateChange = (value: string) => {
    setFilters((current) => ({ ...current, toDate: value, quickDate: '' }))
  }

  const clearFilters = () => {
    setFilters(createDefaultLogsFilters())
    setSearch('')
    setExportNotice(null)
  }

  const handleExport = async (scope: LogExportScope = 'screen') => {
    setIsExporting(true)
    setExportNotice(null)

    try {
      const { blob, fileName, exportedCount, totalMatching, truncated } = await exportLogs(
        buildExportLogsParams(filters, search, scope),
      )

      const url = URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = fileName
      anchor.click()
      URL.revokeObjectURL(url)

      const scopeLabel = scope === 'all' ? 'Tüm zamanlar' : getQuickDateLabel(filters.quickDate)

      setExportNotice(
        truncated
          ? `${scopeLabel}: ${exportedCount} kayıt indirildi (${totalMatching} eşleşen kayıttan ilk ${exportedCount}).`
          : `${scopeLabel}: ${exportedCount} kayıt CSV olarak indirildi.`,
      )
      setExportDialogOpen(false)
    } catch (error) {
      setExportNotice((error as Error).message)
    } finally {
      setIsExporting(false)
    }
  }

  const handleExportClick = () => {
    if (hasActiveFilters) {
      void handleExport('screen')
      return
    }

    setExportDialogOpen(true)
  }

  const screenExportCount = totalCount
  const allExportCount = isDateFiltered ? (overallSummary?.total ?? totalCount) : totalCount

  if (!can('logs:read')) {
    return <AccessDeniedCard permission="logs:read" />
  }

  return (
    <div className="space-y-8">
      <PageHeader
        title="Loglar"
        description="Tenant loglarını izleyin, filtreleyin ve analiz edin."
        actions={
          <div className="flex flex-wrap items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={handleExportClick}
              disabled={isExporting || logsQuery.isFetching}
            >
              <Download className={cn('h-4 w-4', isExporting && 'animate-pulse')} />
              CSV indir
            </Button>
            <Button
              variant="outline"
              size="sm"
              onClick={() => logsQuery.refetch()}
              disabled={logsQuery.isFetching}
            >
              <RefreshCw className={cn('h-4 w-4', logsQuery.isFetching && 'animate-spin')} />
              Yenile
            </Button>
          </div>
        }
      />

      {exportNotice && (
        <Alert>
          <AlertTitle>Dışa aktarma</AlertTitle>
          <AlertDescription>{exportNotice}</AlertDescription>
        </Alert>
      )}

      {logsQuery.isError && (
        <Alert variant="destructive">
          <AlertTitle>API hatası</AlertTitle>
          <AlertDescription>{(logsQuery.error as Error).message}</AlertDescription>
        </Alert>
      )}

      <LogsStatsGrid
        summary={summary}
        overallSummary={overallSummary}
        isDateFiltered={isDateFiltered}
        isLoading={logsQuery.isLoading && !logsQuery.data}
      />

      {shouldUseLiveRefresh(filters.quickDate) && (
        <p className="text-xs text-muted-foreground">
          Canlı aralık seçili — liste 30 saniyede bir otomatik yenilenir.
        </p>
      )}

      <LogsFilterBar
        filters={filters}
        filterOptions={filterOptions}
        onSearchInputChange={(value) =>
          setFilters((current) => ({ ...current, searchInput: value }))
        }
        onLogLevelsChange={(values) =>
          setFilters((current) => ({ ...current, logLevels: values }))
        }
        onHttpMethodsChange={(values) =>
          setFilters((current) => ({ ...current, httpMethods: values }))
        }
        onStatusCodesChange={(values) =>
          setFilters((current) => ({ ...current, statusCodes: values }))
        }
        onApplicationNamesChange={(values) =>
          setFilters((current) => ({ ...current, applicationNames: values }))
        }
        onQuickDateChange={handleQuickDateChange}
        onFromDateChange={handleFromDateChange}
        onToDateChange={handleToDateChange}
        onClear={clearFilters}
        hasActiveFilters={hasActiveFilters}
      />

      <Card className="overflow-hidden">
        <CardHeader className="border-b border-border/50 bg-muted/20 pb-4">
          <CardTitle className="text-base font-semibold">Log kayıtları</CardTitle>
          <CardDescription>
            {logsQuery.isLoading && !logsQuery.data
              ? 'Yükleniyor...'
              : totalCount === 0
                ? 'Kayıt bulunamadı'
                : `${(page - 1) * pageSize + 1}–${Math.min(page * pageSize, totalCount)} / ${totalCount} kayıt`}
          </CardDescription>
        </CardHeader>
        <CardContent className="p-0">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="data-table-head">
                  <th className="px-6 py-3.5 font-medium">Zaman</th>
                  <th className="px-6 py-3.5 font-medium">Seviye</th>
                  <th className="px-6 py-3.5 font-medium">Uygulama</th>
                  <th className="px-6 py-3.5 font-medium">Mesaj</th>
                </tr>
              </thead>
              <tbody>
                {logsQuery.isLoading && !logsQuery.data ? (
                  Array.from({ length: 8 }).map((_, i) => (
                    <tr key={i} className="border-b">
                      <td colSpan={4} className="px-6 py-3">
                        <Skeleton className="h-5 w-full" />
                      </td>
                    </tr>
                  ))
                ) : totalCount === 0 ? (
                  <tr>
                    <td colSpan={4}>
                      <EmptyState
                        title="Kayıt bulunamadı"
                        description={
                          hasActiveFilters
                            ? 'Filtreleri değiştirmeyi veya temizlemeyi deneyin.'
                            : 'Müşteri uygulamanız API anahtarı ile POST /api/logs üzerinden log gönderebilir.'
                        }
                      />
                    </td>
                  </tr>
                ) : (
                  logs.map((log) => (
                    <tr
                      key={log.id}
                      onClick={() => setSelectedLog(log)}
                      className="data-table-row cursor-pointer"
                    >
                      <td className="whitespace-nowrap px-6 py-4 font-mono text-[11px] text-muted-foreground">
                        {formatTimestamp(log.timestamp)}
                      </td>
                      <td className="px-6 py-4">
                        <LogLevelBadge level={log.logLevel} />
                      </td>
                      <td className="px-6 py-4 text-sm font-medium">{log.applicationName}</td>
                      <td className="max-w-md px-6 py-4 text-sm text-muted-foreground">{log.message}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {totalCount > 0 && (
            <div className="flex flex-col gap-3 border-t px-6 py-4 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <span>Sayfa başına</span>
                <Select
                  className="h-9 w-20"
                  value={String(pageSize)}
                  onChange={(e) =>
                    setPageSize(Number(e.target.value) as (typeof PAGE_SIZE_OPTIONS)[number])
                  }
                >
                  {PAGE_SIZE_OPTIONS.map((size) => (
                    <option key={size} value={size}>
                      {size}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="flex flex-wrap items-center gap-1">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page <= 1 || logsQuery.isFetching}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                  <ChevronLeft className="h-4 w-4" />
                  Önceki
                </Button>
                {pageNumbers.map((pageNumber) => (
                  <Button
                    key={pageNumber}
                    variant={pageNumber === page ? 'default' : 'outline'}
                    size="sm"
                    className="min-w-9"
                    disabled={logsQuery.isFetching}
                    onClick={() => setPage(pageNumber)}
                  >
                    {pageNumber}
                  </Button>
                ))}
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= totalPages || logsQuery.isFetching}
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                >
                  Sonraki
                  <ChevronRight className="h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      <LogDetailSheet log={selectedLog} onClose={() => setSelectedLog(null)} />

      <LogExportScopeDialog
        open={exportDialogOpen}
        onClose={() => setExportDialogOpen(false)}
        onConfirm={handleExport}
        screenLabel={getQuickDateLabel(filters.quickDate)}
        screenCount={screenExportCount}
        allCount={allExportCount}
        isLoading={isExporting}
      />
    </div>
  )
}
