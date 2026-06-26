const TENANT_PANEL_URL = import.meta.env.VITE_TENANT_PANEL_URL ?? 'http://localhost:5173'

export function getTenantPanelUrl(): string {
  return TENANT_PANEL_URL.replace(/\/$/, '')
}

export function buildTenantPanelImpersonateUrl(ticket: string): string {
  return `${getTenantPanelUrl()}/impersonate?ticket=${encodeURIComponent(ticket)}`
}

/** Call on pointerdown — must run in the same user gesture, before async API. */
export function openPendingTenantPanelTab(): Window | null {
  return window.open('about:blank', '_blank')
}

export function navigateTenantPanelTab(tab: Window | null | undefined, ticket: string): boolean {
  if (!tab || tab.closed) return false

  tab.location.replace(buildTenantPanelImpersonateUrl(ticket))
  tab.focus()
  return true
}

export function tryOpenImpersonateInNewTab(ticket: string): boolean {
  return window.open(buildTenantPanelImpersonateUrl(ticket), '_blank') !== null
}
