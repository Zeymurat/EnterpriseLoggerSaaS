import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Pencil, Plus, Trash2 } from 'lucide-react'
import {
  createPlatformPackage,
  deletePlatformPackage,
  formatPackageLogLevels,
  getPlatformPackages,
  updatePlatformPackage,
  type PlatformPackage,
  type PlatformPackageFormData,
} from '@/lib/api'
import { PackageFormDialog } from '@/components/packages/PackageFormDialog'
import { ConfirmDialog } from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { toast } from 'sonner'
import { cn } from '@/lib/utils'

function formatNumber(value: number): string {
  return new Intl.NumberFormat('tr-TR').format(value)
}

function formatPrice(value: number): string {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: 'TRY',
    maximumFractionDigits: 0,
  }).format(value)
}

function toUpdatePayload(form: PlatformPackageFormData) {
  const { code: _code, ...payload } = form
  return payload
}

function PackageStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg border border-border/50 bg-muted/20 px-3 py-2">
      <p className="text-[11px] font-medium uppercase tracking-wide text-muted-foreground">{label}</p>
      <p className="mt-0.5 text-sm font-medium text-foreground">{value}</p>
    </div>
  )
}

function PackagePrice({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-md border border-border/50 bg-muted/15 px-5 py-1.5 text-center">
      <p className="text-[10px] leading-tight text-muted-foreground">{label}</p>
      <p className="text-xs font-medium tabular-nums text-foreground">{value}</p>
    </div>
  )
}

export function PackagesPage() {
  const queryClient = useQueryClient()
  const [formOpen, setFormOpen] = useState(false)
  const [formMode, setFormMode] = useState<'create' | 'edit'>('create')
  const [editingPackage, setEditingPackage] = useState<PlatformPackage | null>(null)
  const [deleteTarget, setDeleteTarget] = useState<PlatformPackage | null>(null)

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['platform-packages'],
    queryFn: getPlatformPackages,
  })

  const invalidatePackages = async () => {
    await queryClient.invalidateQueries({ queryKey: ['platform-packages'] })
  }

  const saveMutation = useMutation({
    mutationFn: (form: PlatformPackageFormData) =>
      formMode === 'create'
        ? createPlatformPackage(form)
        : updatePlatformPackage(editingPackage!.id, toUpdatePayload(form)),
    onSuccess: async () => {
      toast.success(formMode === 'create' ? 'Paket oluşturuldu.' : 'Paket güncellendi.')
      setFormOpen(false)
      setEditingPackage(null)
      await invalidatePackages()
    },
    onError: (mutationError) => {
      toast.error(mutationError instanceof Error ? mutationError.message : 'Paket kaydedilemedi.')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: (packageId: number) => deletePlatformPackage(packageId),
    onSuccess: async () => {
      toast.success('Paket silindi.')
      setDeleteTarget(null)
      await invalidatePackages()
    },
    onError: (mutationError) => {
      toast.error(mutationError instanceof Error ? mutationError.message : 'Paket silinemedi.')
    },
  })

  const openCreate = () => {
    setFormMode('create')
    setEditingPackage(null)
    setFormOpen(true)
  }

  const openEdit = (pkg: PlatformPackage) => {
    setFormMode('edit')
    setEditingPackage(pkg)
    setFormOpen(true)
  }

  const packages = data?.packages ?? []

  return (
    <>
      <div className="space-y-5">
        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h1 className="text-2xl font-semibold tracking-tight">Paketler</h1>
            <p className="text-sm text-muted-foreground">
              Abonelik planları, kota limitleri ve fiyat kırılımları.
            </p>
          </div>
          <Button onClick={openCreate}>
            <Plus className="mr-2 h-4 w-4" />
            Yeni paket
          </Button>
        </div>

        {isLoading ? (
          <p className="text-sm text-muted-foreground">Paketler yükleniyor…</p>
        ) : isError ? (
          <p className="text-sm text-destructive">
            {error instanceof Error ? error.message : 'Paket listesi alınamadı.'}
          </p>
        ) : (
          <div className="grid gap-3 lg:grid-cols-2">
            {packages.map((pkg) => {
              const canDelete = !pkg.isDefault && pkg.activeTenantCount === 0
              const notifications = [
                pkg.isMailEnabled ? 'E-posta' : null,
                pkg.isSmsEnabled ? 'SMS' : null,
              ].filter(Boolean)

              return (
                <article
                  key={pkg.id}
                  className="rounded-xl border border-border/60 bg-card/80 p-4 shadow-sm transition-colors hover:border-primary/30"
                >
                  <div className="flex items-start justify-between gap-3">
                    <div className="min-w-0 space-y-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <h2 className="text-base font-semibold tracking-tight">{pkg.name}</h2>
                        <code className="rounded-md bg-muted px-1.5 py-0.5 text-[11px] text-muted-foreground">
                          {pkg.code}
                        </code>
                      </div>
                      {pkg.description ? (
                        <p className="text-sm text-muted-foreground line-clamp-2">{pkg.description}</p>
                      ) : null}
                    </div>

                    <div className="flex shrink-0 items-center gap-1">
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-8 w-8 p-0"
                        onClick={() => openEdit(pkg)}
                        aria-label="Düzenle"
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        size="sm"
                        className={cn(
                          'h-8 w-8 p-0',
                          canDelete
                            ? 'text-destructive hover:text-destructive'
                            : 'text-muted-foreground/40',
                        )}
                        disabled={!canDelete}
                        title={
                          pkg.isDefault
                            ? 'Varsayılan paket silinemez'
                            : pkg.activeTenantCount > 0
                              ? 'Aktif müşterisi olan paket silinemez'
                              : 'Sil'
                        }
                        onClick={() => setDeleteTarget(pkg)}
                        aria-label="Sil"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>

                  <div className="mt-3 flex flex-wrap gap-2">
                    {pkg.isDefault ? <Badge variant="secondary">Varsayılan</Badge> : null}
                    <Badge variant={pkg.isAvailable ? 'default' : 'outline'}>
                      {pkg.isAvailable ? 'Satışta' : 'Kapalı'}
                    </Badge>
                    <Badge variant="outline">
                      {formatNumber(pkg.activeTenantCount)} aktif müşteri
                    </Badge>
                  </div>

                  <div className="mt-3 grid grid-cols-2 gap-2 sm:grid-cols-4">
                    <PackageStat
                      label="Dakikalık kota"
                      value={formatNumber(pkg.maxLogsPerMinute)}
                    />
                    <PackageStat
                      label="Aylık kota"
                      value={formatNumber(pkg.monthlyRequestLimit)}
                    />
                    <PackageStat
                      label="Saklama"
                      value={`${formatNumber(pkg.storageRetentionDays)} gün`}
                    />
                    <PackageStat
                      label="Seviyeler"
                      value={formatPackageLogLevels(pkg.allowedLogLevels)}
                    />
                  </div>

                  <div className="mt-3 flex flex-wrap items-center justify-between gap-x-3 gap-y-2 border-t border-border/40 pt-3">
                    <p className="text-xs text-muted-foreground">
                      {notifications.length > 0 ? notifications.join(' · ') : 'Bildirim yok'}
                    </p>
                    <div className="flex flex-wrap gap-6">
                      <PackagePrice label="Ay" value={formatPrice(pkg.priceMonthly)} />
                      <PackagePrice label="3 ay" value={formatPrice(pkg.priceQuarterly)} />
                      <PackagePrice label="6 ay" value={formatPrice(pkg.priceSemiAnnual)} />
                      <PackagePrice label="Yıl" value={formatPrice(pkg.priceAnnual)} />
                    </div>
                  </div>
                </article>
              )
            })}
          </div>
        )}
      </div>

      <PackageFormDialog
        open={formOpen}
        mode={formMode}
        pkg={editingPackage}
        isSaving={saveMutation.isPending}
        onClose={() => {
          setFormOpen(false)
          setEditingPackage(null)
        }}
        onSubmit={(form) => saveMutation.mutate(form)}
      />

      <ConfirmDialog
        open={deleteTarget !== null}
        onClose={() => setDeleteTarget(null)}
        onConfirm={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}
        title="Paket silinsin mi?"
        description={
          deleteTarget
            ? `${deleteTarget.name} kalıcı olarak silinecek. Abonelik veya ödeme kaydı varsa işlem reddedilir.`
            : ''
        }
        confirmLabel="Sil"
        variant="destructive"
        isLoading={deleteMutation.isPending}
      />
    </>
  )
}
