import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  assignTenantSubscription,
  BillingCycle,
  billingCycleLabel,
  getPlatformPackages,
  getPlatformTenantDetail,
  impersonateTenant,
  openTenantPanelWithSession,
  subscriptionStatusLabel,
  type BillingCycleValue,
} from '@/lib/api'
import { Sheet } from '@/components/ui/sheet'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { toast } from 'sonner'
import { ExternalLink } from 'lucide-react'

interface TenantManageSheetProps {
  tenantId: number | null
  open: boolean
  onClose: () => void
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat('tr-TR').format(value)
}

export function TenantManageSheet({ tenantId, open, onClose }: TenantManageSheetProps) {
  const queryClient = useQueryClient()
  const [packageId, setPackageId] = useState('')
  const [billingCycle, setBillingCycle] = useState(String(BillingCycle.Monthly))
  const [autoRenew, setAutoRenew] = useState(true)
  const [isPaid, setIsPaid] = useState(true)

  const detailQuery = useQuery({
    queryKey: ['platform-tenant', tenantId],
    queryFn: () => getPlatformTenantDetail(tenantId!),
    enabled: open && tenantId !== null && tenantId > 0,
  })

  const packagesQuery = useQuery({
    queryKey: ['platform-packages'],
    queryFn: getPlatformPackages,
    enabled: open,
  })

  const tenant = detailQuery.data
  const current = tenant?.currentSubscription

  useEffect(() => {
    if (!open || !current) return
    setPackageId(String(current.packageId))
    setBillingCycle(String(current.billingCycle))
    setAutoRenew(current.autoRenew)
    setIsPaid(current.isPaid)
  }, [open, current?.id])

  const assignMutation = useMutation({
    mutationFn: () =>
      assignTenantSubscription(tenantId!, {
        packageId: Number(packageId),
        billingCycle: Number(billingCycle) as BillingCycleValue,
        autoRenew,
        isPaid,
      }),
    onSuccess: async () => {
      toast.success('Abonelik güncellendi.')
      await queryClient.invalidateQueries({ queryKey: ['platform-tenant', tenantId] })
      await queryClient.invalidateQueries({ queryKey: ['platform-tenants'] })
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : 'Abonelik atanamadı.')
    },
  })

  const impersonateMutation = useMutation({
    mutationFn: () => impersonateTenant(tenantId!),
    onSuccess: (session) => {
      openTenantPanelWithSession(session)
      toast.success('Tenant paneli yeni sekmede açıldı.')
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : 'Login-as başarısız.')
    },
  })

  const packages = packagesQuery.data?.packages.filter((pkg) => pkg.isAvailable) ?? []

  return (
    <Sheet
      open={open}
      onClose={onClose}
      title={tenant?.name ?? 'Müşteri'}
      description="Abonelik yönetimi ve tenant paneline giriş"
      className="w-full max-w-xl sm:max-w-2xl"
      footer={
        tenant ? (
          <Button
            variant="outline"
            className="w-full sm:w-auto"
            disabled={impersonateMutation.isPending || !tenant.isActive}
            onClick={() => impersonateMutation.mutate()}
          >
            <ExternalLink className="mr-2 h-4 w-4" />
            {impersonateMutation.isPending ? 'Açılıyor…' : 'Tenant paneline giriş (Root)'}
          </Button>
        ) : null
      }
    >
      {detailQuery.isLoading ? (
        <p className="text-sm text-muted-foreground">Yükleniyor…</p>
      ) : detailQuery.isError || !tenant ? (
        <p className="text-sm text-destructive">Müşteri bilgisi alınamadı.</p>
      ) : (
        <div className="space-y-6">
          <div className="flex flex-wrap items-center gap-2">
            <Badge variant={tenant.isActive ? 'default' : 'secondary'}>
              {tenant.isActive ? 'Tenant aktif' : 'Tenant pasif'}
            </Badge>
            <span className="text-sm text-muted-foreground">
              {formatNumber(tenant.userCount)} kullanıcı · {formatNumber(tenant.logCount)} log
            </span>
          </div>

          <section className="space-y-2 rounded-xl border p-4">
            <h3 className="text-sm font-semibold">Mevcut abonelik</h3>
            {current ? (
              <div className="grid gap-1 text-sm text-muted-foreground sm:grid-cols-2">
                <p>
                  <span className="text-foreground">{current.packageName}</span> ·{' '}
                  {subscriptionStatusLabel(current.status)}
                </p>
                <p>{billingCycleLabel(current.billingCycle)}</p>
                <p>
                  {formatDate(current.startDate)} → {formatDate(current.endDate)}
                </p>
                <p>
                  Otomatik yenileme: {current.autoRenew ? 'Açık' : 'Kapalı'} · Ödeme:{' '}
                  {current.isPaid ? 'Alındı' : 'Bekliyor'}
                </p>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">Aktif abonelik yok.</p>
            )}
          </section>

          <section className="space-y-4 rounded-xl border p-4">
            <h3 className="text-sm font-semibold">Paket ata / değiştir</h3>
            <div className="grid gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="sheet-package">Paket</Label>
                <Select
                  id="sheet-package"
                  value={packageId}
                  onChange={(event) => setPackageId(event.target.value)}
                >
                  <option value="">Paket seçin</option>
                  {packages.map((pkg) => (
                    <option key={pkg.id} value={pkg.id}>
                      {pkg.name}
                    </option>
                  ))}
                </Select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="sheet-cycle">Dönem</Label>
                <Select
                  id="sheet-cycle"
                  value={billingCycle}
                  onChange={(event) => setBillingCycle(event.target.value)}
                >
                  {Object.entries(BillingCycle).map(([label, value]) => (
                    <option key={label} value={value}>
                      {billingCycleLabel(value)}
                    </option>
                  ))}
                </Select>
              </div>
            </div>
            <div className="flex flex-wrap gap-4">
              <Checkbox
                label="Otomatik yenileme"
                checked={autoRenew}
                onChange={(event) => setAutoRenew(event.target.checked)}
              />
              <Checkbox
                label="Ödeme alındı"
                checked={isPaid}
                onChange={(event) => setIsPaid(event.target.checked)}
              />
            </div>
            <Button
              disabled={!packageId || assignMutation.isPending}
              onClick={() => assignMutation.mutate()}
            >
              {assignMutation.isPending ? 'Kaydediliyor…' : 'Aboneliği uygula'}
            </Button>
            <p className="text-xs text-muted-foreground">
              Otomatik yenileme şu an yalnızca kayıt altına alınır; süre bitince uzatma işi Faz 2D
              worker ile gelecek.
            </p>
          </section>

          <section className="space-y-3">
            <h3 className="text-sm font-semibold">Abonelik geçmişi</h3>
            {tenant.subscriptionHistory.length === 0 ? (
              <p className="text-sm text-muted-foreground">Kayıt yok.</p>
            ) : (
              tenant.subscriptionHistory.map((item) => (
                <div key={item.id} className="rounded-lg border px-3 py-2 text-sm">
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-medium">{item.packageName}</span>
                    <Badge variant="secondary">{subscriptionStatusLabel(item.status)}</Badge>
                  </div>
                  <p className="text-muted-foreground">
                    {formatDate(item.startDate)} → {formatDate(item.endDate)}
                  </p>
                </div>
              ))
            )}
          </section>
        </div>
      )}
    </Sheet>
  )
}
