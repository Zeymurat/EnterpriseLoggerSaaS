import { Badge, type BadgeProps } from '@/components/ui/badge'

const levelVariants: Record<string, BadgeProps['variant']> = {
  Info: 'info',
  Warning: 'warning',
  Error: 'destructive',
}

export function LogLevelBadge({ level }: { level: string }) {
  return <Badge variant={levelVariants[level] ?? 'muted'}>{level}</Badge>
}
