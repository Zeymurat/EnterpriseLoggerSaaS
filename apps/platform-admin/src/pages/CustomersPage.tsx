import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Search, X } from 'lucide-react'
import {
  getPlatformPackages,
  getPlatformTenants,
  type PlatformTenantListFilters,
  subscriptionStatusLabel,
} from '@/lib/api'
import { TenantManageSheet } from '@/components/tenants/TenantManageSheet'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { cn } from '@/lib/utils'

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat('tr-TR').format(value)
}

const emptyFilters: PlatformTenantListFilters = {
  name: '',
  rootEmail: '',
  rootPhone: '',
  packageCode: '',
  isActive: undefined,
  subscriptionStartFrom: '',
  subscriptionStartTo: '',
}

function countActiveFilters(filters: PlatformTenantListFilters): number {
  let count = 0
  if (filters.name?.trim()) count++
  if (filters.rootEmail?.trim()) count++
  if (filters.rootPhone?.trim()) count++
  if (filters.packageCode?.trim()) count++
  if (filters.isActive !== undefined) count++
  if (filters.subscriptionStartFrom?.trim()) count++
  if (filters.subscriptionStartTo?.trim()) count++
  return count
}

export function CustomersPage() {
  const [selectedTenantId, setSelectedTenantId] = useState<number | null>(null)
  const [sheetOpen, setSheetOpen] = useState(false)
  const [draftFilters, setDraftFilters] = useState<PlatformTenantListFilters>(emptyFilters)
  const [appliedFilters, setAppliedFilters] = useState<PlatformTenantListFilters>(emptyFilters)

  const queryFilters = useMemo(
    () => ({
      name: appliedFilters.name || undefined,
      rootEmail: appliedFilters.rootEmail || undefined,
      rootPhone: appliedFilters.rootPhone || undefined,
      packageCode: appliedFilters.packageCode || undefined,
      isActive: appliedFilters.isActive,
      subscriptionStartFrom: appliedFilters.subscriptionStartFrom || undefined,
      subscriptionStartTo: appliedFilters.subscriptionStartTo || undefined,
    }),
    [appliedFilters],
  )

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['platform-tenants', queryFilters],
    queryFn: () => getPlatformTenants(queryFilters),
  })

  const packagesQuery = useQuery({
    queryKey: ['platform-packages'],
    queryFn: getPlatformPackages,
  })

  const openTenantSheet = (tenantId: number) => {
    setSelectedTenantId(tenantId)
    setSheetOpen(true)
  }

  const closeTenantSheet = () => {
    setSheetOpen(false)
    setSelectedTenantId(null)
  }

  const tenants = data?.tenants ?? []
  const packages = packagesQuery.data?.packages ?? []
  const activeFilterCount = countActiveFilters(appliedFilters)

  const applyFilters = () => setAppliedFilters({ ...draftFilters })

  const clearFilters = () => {
    setDraftFilters(emptyFilters)
    setAppliedFilters(emptyFilters)
  }

  return (
    <>
      <div className="space-y-5">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">Müşteriler</h1>
            <p className="text-sm text-muted-foreground">
              Kayıtlı tenantlar, paket durumları ve abonelik yönetimi.
            </p>
          </div>
          {!isLoading && !isError ? (
            <p className="text-sm text-muted-foreground">
              {tenants.length} sonuç
              {activeFilterCount > 0 ? ` · ${activeFilterCount} filtre` : ''}
            </p>
          ) : null}
        </div>

        <div className="rounded-xl border border-border/60 bg-muted/15 p-2.5 sm:p-3">
          <div className="flex flex-wrap items-center gap-2">
            <div className="relative w-full max-w-[76rem] flex-1">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                className="h-9 pl-9"
                placeholder="Firma adı"
                value={draftFilters.name ?? ''}
                onChange={(event) =>
                  setDraftFilters((current) => ({ ...current, name: event.target.value }))
                }
                onKeyDown={(event) => {
                  if (event.key === 'Enter') applyFilters()
                }}
              />
            </div>

            <div className="w-[8.5rem]">
              <Select
                className="h-9"
                value={draftFilters.packageCode ?? ''}
                onChange={(event) =>
                  setDraftFilters((current) => ({ ...current, packageCode: event.target.value }))
                }
              >
                <option value="">Paket</option>
                {packages.map((pkg) => (
                  <option key={pkg.id} value={pkg.code}>
                    {pkg.name}
                  </option>
                ))}
              </Select>
            </div>

            <div className="w-[7.5rem]">
              <Select
                className="h-9"
                value={
                  draftFilters.isActive === undefined
                    ? ''
                    : draftFilters.isActive
                      ? 'active'
                      : 'inactive'
                }
                onChange={(event) => {
                  const value = event.target.value
                  setDraftFilters((current) => ({
                    ...current,
                    isActive: value === '' ? undefined : value === 'active',
                  }))
                }}
              >
                <option value="">Durum</option>
                <option value="active">Aktif</option>
                <option value="inactive">Pasif</option>
              </Select>
            </div>

            <div className="ml-auto flex items-center gap-2">
            <Button type="button" size="sm" className="h-9" onClick={applyFilters}>
              Uygula
            </Button>

            {activeFilterCount > 0 ? (
              <Button
                type="button"
                variant="ghost"
                size="sm"
                className="h-9 px-2 text-muted-foreground"
                onClick={clearFilters}
                aria-label="Filtreleri temizle"
              >
                <X className="h-4 w-4" />
              </Button>
            ) : null}
            </div>
          </div>

          <div className="mt-2.5 grid gap-2 border-t border-border/40 pt-2.5 sm:grid-cols-2 lg:grid-cols-4">
              <Input
                className="h-9"
                placeholder="Root e-posta"
                value={draftFilters.rootEmail ?? ''}
                onChange={(event) =>
                  setDraftFilters((current) => ({ ...current, rootEmail: event.target.value }))
                }
                onKeyDown={(event) => {
                  if (event.key === 'Enter') applyFilters()
                }}
              />
              <Input
                className="h-9"
                placeholder="Root telefon"
                value={draftFilters.rootPhone ?? ''}
                onChange={(event) =>
                  setDraftFilters((current) => ({ ...current, rootPhone: event.target.value }))
                }
                onKeyDown={(event) => {
                  if (event.key === 'Enter') applyFilters()
                }}
              />
              <Input
                className="h-9"
                type="date"
                title="Paket başlangıç (min)"
                value={draftFilters.subscriptionStartFrom ?? ''}
                onChange={(event) =>
                  setDraftFilters((current) => ({
                    ...current,
                    subscriptionStartFrom: event.target.value,
                  }))
                }
              />
              <Input
                className="h-9"
                type="date"
                title="Paket başlangıç (max)"
                value={draftFilters.subscriptionStartTo ?? ''}
                onChange={(event) =>
                  setDraftFilters((current) => ({
                    ...current,
                    subscriptionStartTo: event.target.value,
                  }))
                }
              />
          </div>
        </div>

        {isLoading ? (
          <p className="text-sm text-muted-foreground">Müşteri listesi yükleniyor…</p>
        ) : isError ? (
          <p className="text-sm text-destructive">
            {error instanceof Error ? error.message : 'Müşteri listesi alınamadı.'}
          </p>
        ) : tenants.length === 0 ? (
          <p className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
            Filtrelere uygun müşteri bulunamadı.
          </p>
        ) : (
          <div className="grid gap-4">
            {tenants.map((tenant) => (
              <Card
                key={tenant.id}
                role="button"
                tabIndex={0}
                className={cn(
                  'cursor-pointer transition-colors hover:border-primary/40 hover:bg-muted/30',
                  selectedTenantId === tenant.id && sheetOpen && 'border-primary/50',
                )}
                onClick={() => openTenantSheet(tenant.id)}
                onKeyDown={(event) => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault()
                    openTenantSheet(tenant.id)
                  }
                }}
              >
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <div className="space-y-1">
                    <CardTitle className="text-base font-semibold">{tenant.name}</CardTitle>
                    {tenant.currentPackageName ? (
                      <p className="text-sm text-muted-foreground">
                        {tenant.currentPackageName}
                        {tenant.currentSubscriptionStatus !== null
                          ? ` · ${subscriptionStatusLabel(tenant.currentSubscriptionStatus)}`
                          : null}
                        {tenant.currentSubscriptionStartDate
                          ? ` · ${formatDate(tenant.currentSubscriptionStartDate)}`
                          : null}
                      </p>
                    ) : (
                      <p className="text-sm text-muted-foreground">Paket atanmamış</p>
                    )}
                  </div>
                  <Badge variant={tenant.isActive ? 'default' : 'secondary'}>
                    {tenant.isActive ? 'Aktif' : 'Pasif'}
                  </Badge>
                </CardHeader>
                <CardContent className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                  <div className="grid gap-1 text-sm text-muted-foreground sm:grid-cols-2 lg:grid-cols-4 sm:gap-4">
                    <p>Root: {tenant.rootEmail ?? '—'}</p>
                    <p>Tel: {tenant.rootPhone ?? '—'}</p>
                    <p>Kayıt: {formatDate(tenant.createdAt)}</p>
                    <p>
                      {formatNumber(tenant.userCount)} kullanıcı · {formatNumber(tenant.logCount)} log
                    </p>
                  </div>
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={(event) => {
                      event.stopPropagation()
                      openTenantSheet(tenant.id)
                    }}
                  >
                    Yönet
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>

      <TenantManageSheet
        tenantId={selectedTenantId}
        open={sheetOpen}
        onClose={closeTenantSheet}
      />
    </>
  )
}
