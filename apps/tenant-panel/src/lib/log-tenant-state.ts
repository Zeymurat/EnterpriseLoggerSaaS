import type { LogLevelSummary } from '@/types/log-entry'

export function resolveTenantTotalLogs(
  summary: LogLevelSummary | undefined,
  overallSummary: LogLevelSummary | null | undefined,
  isDateFiltered: boolean,
): number | null {
  if (!summary) return null

  if (isDateFiltered) return overallSummary?.total ?? 0

  return summary.total
}

export function isTenantWithoutLogs(
  summary: LogLevelSummary | undefined,
  overallSummary: LogLevelSummary | null | undefined,
  isDateFiltered: boolean,
  isReady: boolean,
): boolean {
  if (!isReady) return false

  const total = resolveTenantTotalLogs(summary, overallSummary, isDateFiltered)
  return total === 0
}
