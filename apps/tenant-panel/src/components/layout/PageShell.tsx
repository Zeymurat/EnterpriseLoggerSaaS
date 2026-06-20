import type { LucideIcon } from 'lucide-react'
import { Inbox } from 'lucide-react'
import type { SupportedLogLevelKey } from '@/lib/log-levels'
import { getLogLevelStyle } from '@/lib/log-levels'
import { cn } from '@/lib/utils'

interface PageHeaderProps {
  title: string
  description?: string
  actions?: React.ReactNode
}

export function PageHeader({ title, description, actions }: PageHeaderProps) {
  return (
    <div className="flex flex-col gap-5 sm:flex-row sm:items-end sm:justify-between">
      <div className="space-y-2">
        <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-primary/80">
          Tenant panel
        </p>
        <h1 className="text-3xl font-semibold tracking-tight text-foreground sm:text-[2rem]">
          {title}
        </h1>
        {description && (
          <p className="max-w-2xl text-sm leading-relaxed text-muted-foreground">{description}</p>
        )}
      </div>
      {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
    </div>
  )
}

type StatTone = 'default' | 'info' | 'warning' | 'error' | 'success' | 'violet'

interface StatCardProps {
  label: string
  value: number | string
  tone?: StatTone
  logLevel?: SupportedLogLevelKey
  icon?: LucideIcon
  hint?: string
}

const toneValueStyles: Record<StatTone, string> = {
  default: 'text-foreground',
  info: 'text-status-info',
  warning: 'text-status-warning',
  error: 'text-status-error',
  success: 'text-status-success',
  violet: 'text-status-accent',
}

const toneAccentStyles: Record<StatTone, string> = {
  default: 'bg-primary',
  info: 'bg-status-info',
  warning: 'bg-status-warning',
  error: 'bg-status-error',
  success: 'bg-status-success',
  violet: 'bg-status-accent',
}

const iconStyles: Record<StatTone, string> = {
  default: 'bg-primary/10 text-primary',
  info: 'bg-status-info-muted text-status-info',
  warning: 'bg-status-warning-muted text-status-warning',
  error: 'bg-status-error-muted text-status-error',
  success: 'bg-status-success-muted text-status-success',
  violet: 'bg-status-accent-muted text-status-accent',
}

export function StatCard({
  label,
  value,
  tone = 'default',
  logLevel,
  icon: Icon,
  hint,
}: StatCardProps) {
  const levelStyle = logLevel ? getLogLevelStyle(logLevel) : null
  const resolvedTone = levelStyle?.tone ?? tone
  const accentClass = levelStyle?.accentClass ?? toneAccentStyles[resolvedTone]

  return (
    <div className="warm-card group p-5 hover:-translate-y-0.5">
      <div className={cn('absolute inset-x-0 top-0 h-1 opacity-90', accentClass)} />
      <div className="flex items-start justify-between gap-4 pt-1">
        <div className="space-y-2.5">
          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
          <p
            className={cn(
              'text-[2rem] font-semibold leading-none tabular-nums tracking-tight',
              toneValueStyles[resolvedTone],
            )}
          >
            {value}
          </p>
          {hint && <p className="text-xs text-muted-foreground/90">{hint}</p>}
        </div>
        {Icon && (
          <div
            className={cn(
              'flex h-11 w-11 items-center justify-center rounded-2xl transition-transform duration-200 group-hover:scale-105',
              iconStyles[resolvedTone],
            )}
          >
            <Icon className="h-5 w-5" />
          </div>
        )}
      </div>
    </div>
  )
}

interface AccessDeniedCardProps {
  title?: string
  permission: string
}

export function AccessDeniedCard({
  title = 'Erişim reddedildi',
  permission,
}: AccessDeniedCardProps) {
  return (
    <div className="glass-surface p-8">
      <div className="mx-auto max-w-md text-center">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-destructive/10 text-destructive">
          <span className="text-xl font-bold">!</span>
        </div>
        <h2 className="text-xl font-semibold">{title}</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          Bu sayfayı görüntülemek için{' '}
          <code className="rounded-lg bg-muted px-2 py-0.5 text-xs">{permission}</code> izni gerekir.
        </p>
      </div>
    </div>
  )
}

interface EmptyStateProps {
  title: string
  description?: string
  icon?: LucideIcon
  action?: React.ReactNode
  className?: string
}

export function EmptyState({
  title,
  description,
  icon: Icon = Inbox,
  action,
  className,
}: EmptyStateProps) {
  return (
    <div
      className={cn('flex flex-col items-center justify-center px-6 py-20 text-center', className)}
    >
      <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-2xl bg-primary/10 text-primary">
        <Icon className="h-6 w-6" />
      </div>
      <p className="font-medium text-foreground">{title}</p>
      {description && (
        <p className="mt-2 max-w-sm text-sm leading-relaxed text-muted-foreground">{description}</p>
      )}
      {action && <div className="mt-6">{action}</div>}
    </div>
  )
}
