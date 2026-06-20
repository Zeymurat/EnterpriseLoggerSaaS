import { GitBranch } from 'lucide-react'
import { cn } from '@/lib/utils'

interface LogCorrelationTraceButtonProps {
  correlationId: string
  correlationLogCount?: number | null
  onTrace: (correlationId: string) => void
  className?: string
}

export function LogCorrelationTraceButton({
  correlationId,
  correlationLogCount,
  onTrace,
  className,
}: LogCorrelationTraceButtonProps) {
  const trimmed = correlationId.trim()
  const chainSize = correlationLogCount ?? 0

  if (!trimmed || chainSize < 2) return null

  return (
    <button
      type="button"
      title={`İstek zinciri (${chainSize} log) — filtrele`}
      aria-label={`${chainSize} logluk istek zincirini filtrele`}
      onClick={(event) => {
        event.stopPropagation()
        onTrace(trimmed)
      }}
      className={cn(
        'inline-flex shrink-0 items-center gap-1 rounded-lg border border-primary/25 bg-primary/10 px-2 py-1 text-primary transition-colors hover:bg-primary/15 hover:text-primary',
        className,
      )}
    >
      <GitBranch className="h-3.5 w-3.5" />
      <span className="text-[10px] font-semibold tabular-nums leading-none">{chainSize}</span>
    </button>
  )
}
