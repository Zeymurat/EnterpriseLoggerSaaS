export type QuickDatePreset =
  | 'all'
  | '10m'
  | '30m'
  | '1h'
  | '3h'
  | '12h'
  | 'today'
  | '7d'
  | '30d'
  | 'month'
  | 'year'
  | ''

export const RELATIVE_TIME_PRESETS = ['10m', '30m', '1h', '3h', '12h'] as const

export type RelativeTimePreset = (typeof RELATIVE_TIME_PRESETS)[number]

export interface LogsFilterState {
  searchInput: string
  logLevels: string[]
  httpMethods: string[]
  statusCodes: string[]
  applicationNames: string[]
  quickDate: QuickDatePreset
  fromDate: string
  toDate: string
}

export function formatLocalDate(date: Date): string {
  const year = date.getFullYear()
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function toUtcEndOfDay(date: string): string {
  return new Date(`${date}T23:59:59.999`).toISOString()
}

function toUtcStartOfDay(date: string): string {
  return new Date(`${date}T00:00:00.000`).toISOString()
}

const RELATIVE_MINUTES: Record<RelativeTimePreset, number> = {
  '10m': 10,
  '30m': 30,
  '1h': 60,
  '3h': 180,
  '12h': 720,
}

export function isRelativeTimePreset(
  preset: QuickDatePreset,
): preset is RelativeTimePreset {
  return RELATIVE_TIME_PRESETS.includes(preset as RelativeTimePreset)
}

export function quickDatePresetToRange(
  preset: QuickDatePreset,
): { from: string; to: string } | null {
  if (!preset || preset === 'all') return null

  if (isRelativeTimePreset(preset)) {
    const now = new Date()
    const from = new Date(now.getTime() - RELATIVE_MINUTES[preset] * 60 * 1000)
    return { from: from.toISOString(), to: now.toISOString() }
  }

  const today = new Date()
  const end = formatLocalDate(today)

  switch (preset) {
    case 'today':
      return { from: end, to: end }
    case '7d': {
      const start = new Date(today)
      start.setDate(start.getDate() - 6)
      return { from: formatLocalDate(start), to: end }
    }
    case '30d': {
      const start = new Date(today)
      start.setDate(start.getDate() - 29)
      return { from: formatLocalDate(start), to: end }
    }
    case 'month': {
      const start = new Date(today.getFullYear(), today.getMonth(), 1)
      return { from: formatLocalDate(start), to: end }
    }
    case 'year': {
      const start = new Date(today.getFullYear(), 0, 1)
      return { from: formatLocalDate(start), to: end }
    }
    default:
      return null
  }
}

export function createDefaultLogsFilters(): LogsFilterState {
  const today = formatLocalDate(new Date())
  return {
    searchInput: '',
    logLevels: [],
    httpMethods: [],
    statusCodes: [],
    applicationNames: [],
    quickDate: 'today',
    fromDate: today,
    toDate: today,
  }
}

export interface ResolvedLogsDateFilter {
  apply: boolean
  from?: string
  to?: string
  isRelative: boolean
}

export function resolveLogsDateFilter(filters: LogsFilterState): ResolvedLogsDateFilter {
  if (filters.quickDate === 'all') {
    return { apply: false, isRelative: false }
  }

  if (isRelativeTimePreset(filters.quickDate)) {
    const range = quickDatePresetToRange(filters.quickDate)
    if (!range) return { apply: false, isRelative: false }
    return {
      apply: true,
      from: range.from,
      to: range.to,
      isRelative: true,
    }
  }

  if (filters.quickDate) {
    const range = quickDatePresetToRange(filters.quickDate)
    if (range) {
      return {
        apply: true,
        from: toUtcStartOfDay(range.from),
        to: toUtcEndOfDay(range.to),
        isRelative: false,
      }
    }
  }

  if (filters.fromDate || filters.toDate) {
    return {
      apply: true,
      from: filters.fromDate ? toUtcStartOfDay(filters.fromDate) : undefined,
      to: filters.toDate ? toUtcEndOfDay(filters.toDate) : undefined,
      isRelative: false,
    }
  }

  return { apply: false, isRelative: false }
}

export function shouldUseLiveRefresh(preset: QuickDatePreset): boolean {
  return isRelativeTimePreset(preset)
}

export type LogExportScope = 'screen' | 'all'

export function buildExportLogsParams(
  filters: LogsFilterState,
  search: string,
  scope: LogExportScope = 'screen',
) {
  const effectiveFilters =
    scope === 'all'
      ? { ...filters, quickDate: 'all' as const, fromDate: '', toDate: '' }
      : filters

  const dateFilter = resolveLogsDateFilter(effectiveFilters)

  return {
    search,
    logLevels: effectiveFilters.logLevels,
    httpMethods: effectiveFilters.httpMethods,
    statusCodes: effectiveFilters.statusCodes.map(Number),
    applicationNames: effectiveFilters.applicationNames,
    from: dateFilter.apply ? dateFilter.from : undefined,
    to: dateFilter.apply ? dateFilter.to : undefined,
  }
}
