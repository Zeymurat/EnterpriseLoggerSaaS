import { useQuery } from '@tanstack/react-query'
import { Activity } from 'lucide-react'
import { getTenantUsage } from '@/lib/api'
import { cn } from '@/lib/utils'

function formatNumber(value: number): string {
  return new Intl.NumberFormat('tr-TR').format(value)
}

function usagePercent(used: number, limit: number): number {
  if (limit <= 0) return 0
  return Math.min(100, Math.round((used / limit) * 100))
}

function usageTone(percent: number): string {
  if (percent >= 90) return 'bg-destructive'
  if (percent >= 70) return 'bg-status-warning'
  return 'bg-primary'
}

function UsageBar({
  label,
  used,
  limit,
}: {
  label: string
  used: number
  limit: number
}) {
  const percent = usagePercent(used, limit)

  return (
    <div className="space-y-1.5">
      <div className="flex items-center justify-between gap-3 text-xs">
        <span className="text-muted-foreground">{label}</span>
        <span className="tabular-nums text-foreground">
          {formatNumber(used)} / {formatNumber(limit)}
          <span className="ml-1.5 text-muted-foreground">(%{percent})</span>
        </span>
      </div>
      <div className="h-1.5 overflow-hidden rounded-full bg-muted">
        <div
          className={cn('h-full rounded-full transition-all', usageTone(percent))}
          style={{ width: `${percent}%` }}
        />
      </div>
    </div>
  )
}

export function QuotaUsageBanner() {
  const { data } = useQuery({
    queryKey: ['tenant-usage'],
    queryFn: getTenantUsage,
    staleTime: 30_000,
    refetchInterval: 60_000,
  })

  if (!data) {
    return null
  }

  return (
    <div className="mb-6 rounded-xl border border-border/60 bg-muted/20 px-4 py-3">
      <div className="mb-3 flex items-center gap-2 text-sm font-medium">
        <Activity className="h-4 w-4 text-muted-foreground" />
        <span>Kota kullanımı</span>
        <span className="text-muted-foreground">· {data.packageName}</span>
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        <UsageBar
          label="Aylık log"
          used={data.monthlyLogCount}
          limit={data.monthlyRequestLimit}
        />
        <UsageBar
          label="Son 1 dakika"
          used={data.logsLastMinute}
          limit={data.maxLogsPerMinute}
        />
      </div>
    </div>
  )
}
