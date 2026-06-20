import type { LogsFilterState } from '@/lib/log-date-filters'
import { createDefaultLogsFilters } from '@/lib/log-date-filters'

export function buildLogsTracePath(correlationId: string): string {
  return `/logs?correlationId=${encodeURIComponent(correlationId.trim())}`
}

export function applyCorrelationTraceFilters(
  correlationId: string,
  base: LogsFilterState = createDefaultLogsFilters(),
): LogsFilterState {
  return {
    ...base,
    searchInput: '',
    logLevels: [],
    httpMethods: [],
    statusCodes: [],
    applicationNames: [],
    quickDate: 'all',
    fromDate: '',
    toDate: '',
    correlationId: correlationId.trim(),
  }
}
