import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight, Download, Search, X } from 'lucide-react'
import { toast } from 'sonner'
import {
  exportPlatformTenants,
  getPlatformPackages,
  getPlatformTenants,
  type PlatformTenantListFilters,
  type PlatformTenantSortField,
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

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100] as const

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
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(25)
  const [sortBy, setSortBy] = useState<PlatformTenantSortField>('createdAt')
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('desc')
  const [exporting, setExporting] = useState(false)

  const queryOptions = useMemo(
    () => ({
      name: appliedFilters.name || undefined,
      rootEmail: appliedFilters.rootEmail || undefined,
      rootPhone: appliedFilters.rootPhone || undefined,
      packageCode: appliedFilters.packageCode || undefined,
      isActive: appliedFilters.isActive,
      subscriptionStartFrom: appliedFilters.subscriptionStartFrom || undefined,
      subscriptionStartTo: appliedFilters.subscriptionStartTo || undefined,
      page,
      pageSize,
      sortBy,
      sortDir,
    }),
    [appliedFilters, page, pageSize, sortBy, sortDir],
  )

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['platform-tenants', queryOptions],
    queryFn: () => getPlatformTenants(queryOptions),
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
  const totalCount = data?.totalCount ?? 0
  const packages = packagesQuery.data?.packages ?? []
  const activeFilterCount = countActiveFilters(appliedFilters)
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))
  const rangeStart = totalCount === 0 ? 0 : (page - 1) * pageSize + 1
  const rangeEnd = Math.min(page * pageSize, totalCount)

  const applyFilters = () => {
    setAppliedFilters({ ...draftFilters })
    setPage(1)
  }

  const clearFilters = () => {
    setDraftFilters(emptyFilters)
    setAppliedFilters(emptyFilters)
    setPage(1)
  }

  const handleExport = async () => {
    setExporting(true)
    try {
      const blob = await exportPlatformTenants({
        name: appliedFilters.name || undefined,
        rootEmail: appliedFilters.rootEmail || undefined,
        rootPhone: appliedFilters.rootPhone || undefined,
        packageCode: appliedFilters.packageCode || undefined,
        isActive: appliedFilters.isActive,
        subscriptionStartFrom: appliedFilters.subscriptionStartFrom || undefined,
        subscriptionStartTo: appliedFilters.subscriptionStartTo || undefined,
        sortBy,
        sortDir,
      })
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = 'musteriler.csv'
      link.click()
      URL.revokeObjectURL(url)
    } catch (err) {
      toast.error(err instanceof Error ? err.message : 'CSV dışa aktarımı başarısız.')
    } finally {
      setExporting(false)
    }
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
          <div className="flex flex-wrap items-center gap-2">
            {!isLoading && !isError ? (
              <p className="text-sm text-muted-foreground">
                {totalCount > 0
                  ? `${formatNumber(rangeStart)}–${formatNumber(rangeEnd)} / ${formatNumber(totalCount)}`
                  : '0 sonuç'}
                {activeFilterCount > 0 ? ` · ${activeFilterCount} filtre` : ''}
              </p>
            ) : null}
            <Button
              type="button"
              variant="outline"
              size="sm"
              className="h-9"
              disabled={exporting || isLoading}
              onClick={handleExport}
            >
              <Download className="mr-2 h-4 w-4" />
              {exporting ? 'Dışa aktarılıyor…' : 'CSV'}
            </Button>
          </div>
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

            <div className="w-[9.5rem]">
              <Select
                className="h-9"
                value={sortBy}
                onChange={(event) => {
                  setSortBy(event.target.value as PlatformTenantSortField)
                  setPage(1)
                }}
              >
                <option value="createdAt">Kayıt tarihi</option>
                <option value="name">Firma adı</option>
                <option value="isActive">Durum</option>
                <option value="userCount">Kullanıcı sayısı</option>
              </Select>
            </div>

            <div className="w-[7rem]">
              <Select
                className="h-9"
                value={sortDir}
                onChange={(event) => {
                  setSortDir(event.target.value as 'asc' | 'desc')
                  setPage(1)
                }}
              >
                <option value="desc">Azalan</option>
                <option value="asc">Artan</option>
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

        {!isLoading && !isError && totalCount > 0 ? (
          <div className="flex flex-col gap-3 border-t border-border/60 pt-4 sm:flex-row sm:items-center sm:justify-between">
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <span>Sayfa başına</span>
              <Select
                className="h-8 w-[4.5rem]"
                value={String(pageSize)}
                onChange={(event) => {
                  setPageSize(Number(event.target.value))
                  setPage(1)
                }}
              >
                {PAGE_SIZE_OPTIONS.map((size) => (
                  <option key={size} value={size}>
                    {size}
                  </option>
                ))}
              </Select>
            </div>

            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-8"
                disabled={page <= 1}
                onClick={() => setPage((current) => Math.max(1, current - 1))}
              >
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <span className="min-w-[5rem] text-center text-sm text-muted-foreground">
                {page} / {totalPages}
              </span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="h-8"
                disabled={page >= totalPages}
                onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
              >
                <ChevronRight className="h-4 w-4" />
              </Button>
            </div>
          </div>
        ) : null}
      </div>

      <TenantManageSheet
        tenantId={selectedTenantId}
        open={sheetOpen}
        onClose={closeTenantSheet}
      />
    </>
  )
}
