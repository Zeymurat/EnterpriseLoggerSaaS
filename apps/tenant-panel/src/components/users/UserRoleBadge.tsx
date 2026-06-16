import { cn } from '@/lib/utils'

const roleStyles: Record<string, string> = {
  Root: 'bg-violet-100 text-violet-800 border-violet-200',
  Admin: 'bg-sky-100 text-sky-800 border-sky-200',
  User: 'bg-slate-100 text-slate-700 border-slate-200',
}

export function UserRoleBadge({ role }: { role: string }) {
  const style = roleStyles[role] ?? 'bg-muted text-muted-foreground border-border'

  return (
    <span
      className={cn(
        'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold',
        style,
      )}
    >
      {role}
    </span>
  )
}
