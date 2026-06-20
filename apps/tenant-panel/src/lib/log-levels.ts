export const SUPPORTED_LOG_LEVELS = [
  {
    key: 'Info',
    label: 'Info',
    tone: 'info' as const,
    summaryKey: 'info' as const,
    accentClass: 'bg-status-info',
  },
  {
    key: 'Warning',
    label: 'Warning',
    tone: 'warning' as const,
    summaryKey: 'warning' as const,
    accentClass: 'bg-status-warning',
  },
  {
    key: 'Error',
    label: 'Error',
    tone: 'error' as const,
    summaryKey: 'error' as const,
    accentClass: 'bg-status-error',
  },
] as const

export type SupportedLogLevelKey = (typeof SUPPORTED_LOG_LEVELS)[number]['key']

export function getLogLevelStyle(key: SupportedLogLevelKey) {
  return SUPPORTED_LOG_LEVELS.find((level) => level.key === key)!
}
