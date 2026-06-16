import type { TenantUser } from '@/lib/api'

/** Root dışındaki örnek ekip üyeleri — sayfa boş görünmesin diye */
export const MOCK_ADDITIONAL_USERS: TenantUser[] = [
  {
    id: -1,
    email: 'admin@demo.local',
    phone: '+905551112233',
    role: 'Admin',
    isActive: true,
    permissions: [],
  },
  {
    id: -2,
    email: 'developer@demo.local',
    phone: '+905552223344',
    role: 'User',
    isActive: true,
    permissions: ['logs:read', 'logs:write'],
  },
  {
    id: -3,
    email: 'viewer@demo.local',
    phone: '+905553334455',
    role: 'User',
    isActive: true,
    permissions: ['logs:read'],
  },
  {
    id: -4,
    email: 'former@demo.local',
    phone: '+905554445566',
    role: 'User',
    isActive: false,
    permissions: ['logs:read'],
  },
]

export function isMockUser(user: TenantUser): boolean {
  return user.id < 0
}

export function mergeWithMockUsers(apiUsers: TenantUser[]): TenantUser[] {
  const emails = new Set(apiUsers.map((u) => u.email.toLowerCase()))
  const extras = MOCK_ADDITIONAL_USERS.filter((m) => !emails.has(m.email.toLowerCase()))
  return [...apiUsers, ...extras]
}
