export const PERMISSION_OPTIONS = [
  { code: 'logs:read', label: 'Log okuma' },
  { code: 'logs:write', label: 'Log yazma' },
  { code: 'users:read', label: 'Kullanıcı listeleme' },
  { code: 'users:invite', label: 'Kullanıcı davet' },
  { code: 'users:manage', label: 'Kullanıcı yönetimi' },
  { code: 'tenant:settings:read', label: 'Tenant ayarları (okuma)' },
  { code: 'tenant:settings:write', label: 'Tenant ayarları (yazma)' },
  { code: 'apikeys:rotate', label: 'API key yenileme' },
] as const

export function permissionLabel(code: string): string {
  return PERMISSION_OPTIONS.find((p) => p.code === code)?.label ?? code
}

/** User rolü davet formunda seçilebilir izinler (yaygın subset) */
export const INVITE_PERMISSION_OPTIONS = PERMISSION_OPTIONS.filter((p) =>
  ['logs:read', 'logs:write', 'users:read'].includes(p.code),
)
