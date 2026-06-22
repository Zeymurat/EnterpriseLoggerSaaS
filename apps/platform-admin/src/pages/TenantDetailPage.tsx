import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  assignTenantSubscription,
  BillingCycle,
  billingCycleLabel,
  getPlatformPackages,
  getPlatformTenantDetail,
  subscriptionStatusLabel,
  type BillingCycleValue,
} from '@/lib/api'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Checkbox } from '@/components/ui/checkbox'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { toast } from 'sonner'

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat('tr-TR').format(value)
}

export function TenantDetailPage() {
  const { id } = useParams()
  const tenantId = Number(id)
  const queryClient = useQueryClient()

  const [packageId, setPackageId] = useState<string>('')
  const [billingCycle, setBillingCycle] = useState<string>(String(BillingCycle.Monthly))
  const [autoRenew, setAutoRenew] = useState(true)
  const [isPaid, setIsPaid] = useState(true)

  const detailQuery = useQuery({
    queryKey: ['platform-tenant', tenantId],
    queryFn: () => getPlatformTenantDetail(tenantId),
    enabled: Number.isFinite(tenantId) && tenantId > 0,
  })

  const packagesQuery = useQuery({
    queryKey: ['platform-packages'],
    queryFn: getPlatformPackages,
  })

  const assignMutation = useMutation({
    mutationFn: () =>
      assignTenantSubscription(tenantId, {
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

  if (!Number.isFinite(tenantId) || tenantId <= 0) {
    return <p className="text-sm text-destructive">Geçersiz tenant kimliği.</p>
  }

  if (detailQuery.isLoading) {
    return <p className="text-sm text-muted-foreground">Tenant detayı yükleniyor…</p>
  }

  if (detailQuery.isError || !detailQuery.data) {
    return (
      <p className="text-sm text-destructive">
        {detailQuery.error instanceof Error ? detailQuery.error.message : 'Tenant bulunamadı.'}
      </p>
    )
  }

  const tenant = detailQuery.data
  const packages = packagesQuery.data?.packages.filter((pkg) => pkg.isAvailable) ?? []
  const current = tenant.currentSubscription

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <Button asChild variant="ghost" size="sm" className="mb-2 -ml-2 w-fit">
            <Link to="/">← Tenant listesi</Link>
          </Button>
          <h1 className="text-2xl font-semibold tracking-tight">{tenant.name}</h1>
          <p className="text-sm text-muted-foreground">
            Kullanıcı: {formatNumber(tenant.userCount)} · Log: {formatNumber(tenant.logCount)}
          </p>
        </div>
        <Badge variant={tenant.isActive ? 'default' : 'secondary'}>
          {tenant.isActive ? 'Tenant aktif' : 'Tenant pasif'}
        </Badge>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Mevcut abonelik</CardTitle>
        </CardHeader>
        <CardContent>
          {current ? (
            <div className="grid gap-2 text-sm sm:grid-cols-2">
              <p>
                <span className="text-muted-foreground">Paket:</span> {current.packageName}
              </p>
              <p>
                <span className="text-muted-foreground">Durum:</span>{' '}
                {subscriptionStatusLabel(current.status)}
              </p>
              <p>
                <span className="text-muted-foreground">Dönem:</span>{' '}
                {billingCycleLabel(current.billingCycle)}
              </p>
              <p>
                <span className="text-muted-foreground">Ödeme:</span>{' '}
                {current.isPaid ? 'Ödendi' : 'Bekliyor'}
              </p>
              <p>
                <span className="text-muted-foreground">Başlangıç:</span>{' '}
                {formatDate(current.startDate)}
              </p>
              <p>
                <span className="text-muted-foreground">Bitiş:</span> {formatDate(current.endDate)}
              </p>
              <p>
                <span className="text-muted-foreground">Dakika limiti:</span>{' '}
                {formatNumber(current.maxLogsPerMinute)}
              </p>
              <p>
                <span className="text-muted-foreground">Aylık log kotası:</span>{' '}
                {formatNumber(current.monthlyRequestLimit)}
              </p>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">Aktif abonelik yok.</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Paket ata / değiştir</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="package-select">Paket</Label>
              <Select
                id="package-select"
                value={packageId}
                onChange={(event) => setPackageId(event.target.value)}
              >
                <option value="">Paket seçin</option>
                {packages.map((pkg) => (
                  <option key={pkg.id} value={pkg.id}>
                    {pkg.name} ({formatNumber(pkg.maxLogsPerMinute)}/dk)
                  </option>
                ))}
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="billing-cycle-select">Faturalama dönemi</Label>
              <Select
                id="billing-cycle-select"
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

          <div className="flex flex-wrap gap-6 text-sm">
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
          {!isPaid ? (
            <p className="text-xs text-muted-foreground">
              Ödeme alınmadıysa abonelik &quot;Ödeme bekliyor&quot; durumunda açılır (önden paket
              tanımlama).
            </p>
          ) : null}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Abonelik geçmişi</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {tenant.subscriptionHistory.length === 0 ? (
            <p className="text-sm text-muted-foreground">Geçmiş kayıt yok.</p>
          ) : (
            tenant.subscriptionHistory.map((item) => (
              <div
                key={item.id}
                className="rounded-lg border px-4 py-3 text-sm"
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <p className="font-medium">{item.packageName}</p>
                  <Badge variant="secondary">{subscriptionStatusLabel(item.status)}</Badge>
                </div>
                <p className="mt-1 text-muted-foreground">
                  {billingCycleLabel(item.billingCycle)} · {formatDate(item.startDate)} →{' '}
                  {formatDate(item.endDate)}
                </p>
              </div>
            ))
          )}
        </CardContent>
      </Card>
    </div>
  )
}
