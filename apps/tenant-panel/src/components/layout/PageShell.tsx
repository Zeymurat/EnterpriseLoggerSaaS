import type { LucideIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

interface PageHeaderProps {
  title: string
  description?: string
  actions?: React.ReactNode
}

export function PageHeader({ title, description, actions }: PageHeaderProps) {
  return (
    <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
      <div className="space-y-1">
        <h1 className="text-3xl font-bold tracking-tight">{title}</h1>
        {description && <p className="text-muted-foreground">{description}</p>}
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
  icon?: LucideIcon
  hint?: string
}

const toneStyles: Record<StatTone, string> = {
  default: 'text-foreground',
  info: 'text-sky-600',
  warning: 'text-amber-600',
  error: 'text-red-600',
  success: 'text-emerald-600',
  violet: 'text-violet-600',
}

const iconStyles: Record<StatTone, string> = {
  default: 'bg-primary/10 text-primary',
  info: 'bg-sky-100 text-sky-600',
  warning: 'bg-amber-100 text-amber-600',
  error: 'bg-red-100 text-red-600',
  success: 'bg-emerald-100 text-emerald-600',
  violet: 'bg-violet-100 text-violet-600',
}

export function StatCard({ label, value, tone = 'default', icon: Icon, hint }: StatCardProps) {
  return (
    <div className="rounded-xl border bg-card p-5 shadow-sm transition-shadow hover:shadow-md">
      <div className="flex items-start justify-between gap-3">
        <div className="space-y-2">
          <p className="text-sm font-medium text-muted-foreground">{label}</p>
          <p className={cn('text-3xl font-bold tabular-nums tracking-tight', toneStyles[tone])}>
            {value}
          </p>
          {hint && <p className="text-xs text-muted-foreground">{hint}</p>}
        </div>
        {Icon && (
          <div className={cn('flex h-10 w-10 items-center justify-center rounded-xl', iconStyles[tone])}>
            <Icon className="h-5 w-5" />
          </div>
        )}
      </div>
    </div>
  )
}

interface MockDataBannerProps {
  title: string
  description: string
}

export function MockDataBanner({ title, description }: MockDataBannerProps) {
  return (
    <div className="flex items-start gap-3 rounded-xl border border-amber-200/80 bg-amber-50/80 px-4 py-3 text-sm text-amber-950">
      <span className="mt-0.5 flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-amber-100 text-xs font-bold text-amber-700">
        ✦
      </span>
      <div>
        <p className="font-medium">{title}</p>
        <p className="mt-0.5 opacity-90">{description}</p>
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
    <div className="rounded-xl border bg-card p-8 shadow-sm">
      <div className="mx-auto max-w-md text-center">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10 text-destructive">
          <span className="text-xl font-bold">!</span>
        </div>
        <h2 className="text-xl font-semibold">{title}</h2>
        <p className="mt-2 text-sm text-muted-foreground">
          Bu sayfayı görüntülemek için{' '}
          <code className="rounded bg-muted px-1.5 py-0.5 text-xs">{permission}</code> izni gerekir.
        </p>
      </div>
    </div>
  )
}

interface EmptyStateProps {
  title: string
  description?: string
}

export function EmptyState({ title, description }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center justify-center px-6 py-16 text-center">
      <p className="font-medium text-foreground">{title}</p>
      {description && <p className="mt-1 max-w-sm text-sm text-muted-foreground">{description}</p>}
    </div>
  )
}
