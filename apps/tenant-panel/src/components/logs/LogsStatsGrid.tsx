import { AlertTriangle, Info, ScrollText, TriangleAlert } from 'lucide-react'
import type { LogLevelSummary } from '@/types/log-entry'
import { SUPPORTED_LOG_LEVELS, type SupportedLogLevelKey } from '@/lib/log-levels'
import { StatCard } from '@/components/layout/PageShell'
import { Skeleton } from '@/components/ui/skeleton'

const levelIcons: Record<SupportedLogLevelKey, typeof Info> = {
  Info,
  Warning: TriangleAlert,
  Error: AlertTriangle,
}

interface LogsStatsGridProps {
  summary?: LogLevelSummary
  overallSummary?: LogLevelSummary | null
  isDateFiltered?: boolean
  isLoading?: boolean
}

export function LogsStatsGrid({
  summary,
  overallSummary,
  isDateFiltered = false,
  isLoading = false,
}: LogsStatsGridProps) {
  if (isLoading && !summary) {
    return (
      <div className="space-y-3">
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {Array.from({ length: 4 }).map((_, i) => (
            <Skeleton key={i} className="h-28 rounded-xl" />
          ))}
        </div>
      </div>
    )
  }

  const filtered = summary ?? { total: 0, info: 0, warning: 0, error: 0 }
  const overall = overallSummary ?? filtered

  const totalHint = isDateFiltered ? `Tüm zamanlar: ${overall.total}` : undefined

  return (
    <div className="space-y-3">
      {isDateFiltered && (
        <div className="flex flex-wrap items-center gap-2">
          <span className="inline-flex items-center gap-2 rounded-full border border-border/60 bg-card/80 px-3.5 py-1.5 text-xs text-muted-foreground shadow-sm backdrop-blur-sm">
            Toplam kayıt
            <span className="font-semibold text-foreground">{overall.total}</span>
          </span>
          <span className="inline-flex items-center gap-2 rounded-full border border-primary/15 bg-primary/5 px-3.5 py-1.5 text-xs text-muted-foreground">
            Seçili aralık
            <span className="font-semibold text-primary">{filtered.total}</span>
          </span>
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard
          label={isDateFiltered ? 'Aralık toplamı' : 'Toplam'}
          value={filtered.total}
          icon={ScrollText}
          hint={totalHint}
        />

        {SUPPORTED_LOG_LEVELS.map((level) => {
          const filteredValue = filtered[level.summaryKey]
          const overallValue = overall[level.summaryKey]
          const Icon = levelIcons[level.key]

          return (
            <StatCard
              key={level.key}
              label={level.label}
              value={filteredValue}
              logLevel={level.key}
              icon={Icon}
              hint={
                isDateFiltered
                  ? `Tüm zamanlar: ${overallValue}`
                  : undefined
              }
            />
          )
        })}
      </div>
    </div>
  )
}
