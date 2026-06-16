import { Badge, type BadgeProps } from '@/components/ui/badge'

const roleVariants: Record<string, BadgeProps['variant']> = {
  Root: 'violet',
  Admin: 'info',
  User: 'muted',
}

export function UserRoleBadge({ role }: { role: string }) {
  return <Badge variant={roleVariants[role] ?? 'muted'}>{role}</Badge>
}
