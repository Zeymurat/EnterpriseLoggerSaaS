import { useEffect, useRef, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  assignTenantSubscription,
  BillingCycle,
  billingCycleLabel,
  cancelTenantSubscription,
  deletePlatformTenant,
  getPlatformPackages,
  getPlatformTenantDetail,
  getTenantAvailablePayments,
  getTenantPayments,
  impersonateTenant,
  linkSubscriptionPayment,
  removeTenantSubscription,
  resetTenantRootPassword,
  setTenantStatus,
  PAYMENT_GRACE_DAY_OPTIONS,
  paymentStatusLabel,
  SubscriptionStatus,
  subscriptionStatusLabel,
  type BillingCycleValue,
} from '@/lib/api'
import {
  buildTenantPanelImpersonateUrl,
  getTenantPanelUrl,
  navigateTenantPanelTab,
  openPendingTenantPanelTab,
  tryOpenImpersonateInNewTab,
} from '@/lib/impersonate'
import { Sheet } from '@/components/ui/sheet'
import { ConfirmDialog, Dialog } from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Button, buttonVariants } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { toast } from 'sonner'
import { ExternalLink, Trash2 } from 'lucide-react'
import { cn } from '@/lib/utils'

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

function formatMoney(value: number, currency: string): string {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value)
}

function buildGracePeriodEndDate(
  isPaid: boolean,
  paymentGraceDays: string,
  customGraceDays: string,
): string | null {
  if (isPaid) return null

  const days =
    paymentGraceDays === 'custom' ? Number(customGraceDays) : Number(paymentGraceDays)

  if (!Number.isFinite(days) || days < 1) {
    return null
  }

  const end = new Date()
  end.setUTCDate(end.getUTCDate() + days)
  return end.toISOString()
}

export function TenantManageSheet({ tenantId, open, onClose }: TenantManageSheetProps) {
  const queryClient = useQueryClient()
  const [packageId, setPackageId] = useState('')
  const [billingCycle, setBillingCycle] = useState(String(BillingCycle.Monthly))
  const [autoRenew, setAutoRenew] = useState(true)
  const [isPaid, setIsPaid] = useState(true)
  const [paymentId, setPaymentId] = useState('')
  const [linkPaymentId, setLinkPaymentId] = useState('')
  const [paymentGraceDays, setPaymentGraceDays] = useState('7')
  const [customGraceDays, setCustomGraceDays] = useState('')
  const [impersonateFallbackTicket, setImpersonateFallbackTicket] = useState<string | null>(null)
  const pendingTenantTabRef = useRef<Window | null>(null)
  const [deleteArmed, setDeleteArmed] = useState(false)
  const [cancellationReason, setCancellationReason] = useState('')
  const [removeSubscriptionId, setRemoveSubscriptionId] = useState<number | null>(null)
  const [statusConfirmOpen, setStatusConfirmOpen] = useState(false)
  const [passwordResetConfirmOpen, setPasswordResetConfirmOpen] = useState(false)
  const [temporaryPassword, setTemporaryPassword] = useState<string | null>(null)

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

  const availablePaymentsQuery = useQuery({
    queryKey: ['tenant-available-payments', tenantId],
    queryFn: () => getTenantAvailablePayments(tenantId!),
    enabled: open && tenantId !== null && tenantId > 0,
  })

  const tenantPaymentsQuery = useQuery({
    queryKey: ['tenant-payments', tenantId],
    queryFn: () => getTenantPayments(tenantId!),
    enabled: open && tenantId !== null && tenantId > 0,
  })

  const tenant = detailQuery.data
  const current = tenant?.currentSubscription
  const pendingPaymentSubscription =
    current &&
    !current.isPaid &&
    current.status === SubscriptionStatus.PendingPayment

  useEffect(() => {
    if (!open) {
      setPaymentId('')
      setLinkPaymentId('')
      setImpersonateFallbackTicket(null)
      setDeleteArmed(false)
      setCancellationReason('')
      setRemoveSubscriptionId(null)
      setStatusConfirmOpen(false)
      setPasswordResetConfirmOpen(false)
      setTemporaryPassword(null)
      return
    }
    if (!current) return
    setPackageId(String(current.packageId))
    setBillingCycle(String(current.billingCycle))
    setAutoRenew(current.autoRenew)
    setIsPaid(current.isPaid)
  }, [open, current?.id])

  const invalidateTenantQueries = async () => {
    await queryClient.invalidateQueries({ queryKey: ['platform-tenant', tenantId] })
    await queryClient.invalidateQueries({ queryKey: ['platform-tenants'] })
    await queryClient.invalidateQueries({ queryKey: ['tenant-available-payments', tenantId] })
    await queryClient.invalidateQueries({ queryKey: ['tenant-payments', tenantId] })
  }

  const assignMutation = useMutation({
    mutationFn: () => {
      const gracePeriodEndDate = buildGracePeriodEndDate(
        isPaid,
        paymentGraceDays,
        customGraceDays,
      )

      if (!isPaid && !gracePeriodEndDate) {
        throw new Error('Geçerli bir ödeme süresi girin.')
      }

      return assignTenantSubscription(tenantId!, {
        packageId: Number(packageId),
        billingCycle: Number(billingCycle) as BillingCycleValue,
        autoRenew,
        isPaid,
        paymentId: paymentId ? Number(paymentId) : null,
        gracePeriodEndDate,
      })
    },
    onSuccess: async () => {
      toast.success('Abonelik güncellendi.')
      await invalidateTenantQueries()
      setPaymentId('')
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : 'Abonelik atanamadı.')
    },
  })

  const linkPaymentMutation = useMutation({
    mutationFn: () =>
      linkSubscriptionPayment(tenantId!, Number(linkPaymentId), current?.id),
    onSuccess: async () => {
      toast.success('Ödeme mevcut aboneliğe bağlandı.')
      await invalidateTenantQueries()
      setLinkPaymentId('')
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : 'Ödeme bağlanamadı.')
    },
  })

  const impersonateMutation = useMutation({
    mutationFn: () => impersonateTenant(tenantId!),
    onSuccess: ({ ticket }) => {
      const opened =
        navigateTenantPanelTab(pendingTenantTabRef.current, ticket) ||
        tryOpenImpersonateInNewTab(ticket)

      pendingTenantTabRef.current = null

      if (opened) {
        setImpersonateFallbackTicket(null)
        toast.success('Tenant paneli yeni sekmede açıldı.')
      } else {
        setImpersonateFallbackTicket(ticket)
        toast.error('Otomatik açılamadı — alttaki bağlantıya tıklayın.', { duration: 10000 })
      }
    },
    onError: (error) => {
      pendingTenantTabRef.current?.close()
      pendingTenantTabRef.current = null
      setImpersonateFallbackTicket(null)
      toast.error(error instanceof Error ? error.message : 'Login-as başarısız.')
    },
  })

  function handleOpenTenantPanelPointerDown(event: React.PointerEvent<HTMLButtonElement>) {
    if (event.button !== 0 || impersonateMutation.isPending || !tenant?.isActive) return
    pendingTenantTabRef.current = openPendingTenantPanelTab()
  }

  function handleOpenTenantPanel() {
    setImpersonateFallbackTicket(null)
    impersonateMutation.mutate()
  }

  const cancelMutation = useMutation({
    mutationFn: () => cancelTenantSubscription(tenantId!, cancellationReason.trim()),
    onSuccess: async () => {
      toast.success('Paket iptali kaydedildi, müşteri Free pakete düşürüldü.')
      setCancellationReason('')
      await invalidateTenantQueries()
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : 'Paket iptal edilemedi.')
    },
  })

  const removeSubscriptionMutation = useMutation({
    mutationFn: (subscriptionId: number) =>
      removeTenantSubscription(tenantId!, subscriptionId),
    onSuccess: async () => {
      toast.success('Paket kaydı silindi.')
      setRemoveSubscriptionId(null)
      await invalidateTenantQueries()
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : 'Paket kaydı silinemedi.')
    },
  })

  const deleteMutation = useMutation({
    mutationFn: () => deletePlatformTenant(tenantId!),
    onSuccess: async () => {
      toast.success('Müşteri silindi.')
      await queryClient.invalidateQueries({ queryKey: ['platform-tenants'] })
      onClose()
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : 'Müşteri silinemedi.')
    },
  })

  const statusMutation = useMutation({
    mutationFn: (isActive: boolean) => setTenantStatus(tenantId!, isActive),
    onSuccess: async (_data, isActive) => {
      toast.success(isActive ? 'Tenant aktifleştirildi.' : 'Tenant pasife alındı.')
      setStatusConfirmOpen(false)
      await invalidateTenantQueries()
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : 'Tenant durumu güncellenemedi.')
    },
  })

  const passwordResetMutation = useMutation({
    mutationFn: () => resetTenantRootPassword(tenantId!),
    onSuccess: ({ temporaryPassword: password }) => {
      setPasswordResetConfirmOpen(false)
      setTemporaryPassword(password)
      toast.success('Geçici şifre oluşturuldu.')
    },
    onError: (error) => {
      toast.error(error instanceof Error ? error.message : 'Şifre sıfırlanamadı.')
    },
  })

  const packages = packagesQuery.data?.packages.filter((pkg) => pkg.isAvailable) ?? []
  const availablePayments = availablePaymentsQuery.data?.payments ?? []
  const tenantPayments = tenantPaymentsQuery.data?.payments ?? []
  const canCancelPackage = current && current.packageCode !== 'free'

  function handleRemoveSubscription(subscriptionId: number) {
    if (removeSubscriptionId === subscriptionId) {
      removeSubscriptionMutation.mutate(subscriptionId)
      return
    }
    setRemoveSubscriptionId(subscriptionId)
  }

  async function copyImpersonateLink() {
    if (!impersonateFallbackTicket) return
    try {
      await navigator.clipboard.writeText(
        buildTenantPanelImpersonateUrl(impersonateFallbackTicket),
      )
      toast.success('Bağlantı panoya kopyalandı.')
    } catch {
      toast.error('Bağlantı kopyalanamadı.')
    }
  }

  const impersonateFallbackUrl = impersonateFallbackTicket
    ? buildTenantPanelImpersonateUrl(impersonateFallbackTicket)
    : null

  async function copyTemporaryPassword() {
    if (!temporaryPassword) return
    try {
      await navigator.clipboard.writeText(temporaryPassword)
      toast.success('Şifre panoya kopyalandı.')
    } catch {
      toast.error('Şifre kopyalanamadı.')
    }
  }

  return (
    <>
    <Sheet
      open={open}
      onClose={onClose}
      title={tenant?.name ?? 'Müşteri'}
      description="Abonelik yönetimi ve tenant paneline giriş"
      className="w-full max-w-xl sm:max-w-2xl"
      footer={
        tenant ? (
          <div className="flex w-full flex-col gap-2">
            <Button
              type="button"
              variant="default"
              className="w-full"
              disabled={impersonateMutation.isPending || !tenant.isActive}
              onPointerDown={handleOpenTenantPanelPointerDown}
              onClick={handleOpenTenantPanel}
            >
              <ExternalLink className="mr-2 h-4 w-4" />
              {impersonateMutation.isPending ? 'Tenant paneli açılıyor…' : 'Tenant paneline giriş'}
            </Button>
            {impersonateFallbackUrl ? (
              <div className="flex flex-col gap-2 rounded-lg border border-amber-500/30 bg-amber-500/10 p-3">
                <p className="text-xs text-muted-foreground">
                  Safari sekmeyi engellediyse aşağıdaki bağlantıya tıklayın (Cmd+Click da
                  çalışır).
                </p>
                <a
                  href={impersonateFallbackUrl}
                  target="_blank"
                  rel="noopener"
                  className={cn(
                    buttonVariants({ variant: 'default' }),
                    'inline-flex w-full items-center justify-center gap-2',
                  )}
                >
                  <ExternalLink className="h-4 w-4" />
                  Yeni sekmede tenant paneli aç
                </a>
                <div className="flex gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    className="flex-1"
                    onClick={copyImpersonateLink}
                  >
                    Linki kopyala
                  </Button>
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    className="flex-1"
                    onClick={() => {
                      window.location.href = impersonateFallbackUrl
                    }}
                  >
                    Bu sekmede aç
                  </Button>
                </div>
              </div>
            ) : null}
            <p className="text-center text-xs text-muted-foreground">
              Tenant panel: {getTenantPanelUrl()}
            </p>
          </div>
        ) : null
      }
    >
      {detailQuery.isLoading ? (
        <p className="text-sm text-muted-foreground">Yükleniyor…</p>
      ) : detailQuery.isError || !tenant ? (
        <p className="text-sm text-destructive">Müşteri bilgisi alınamadı.</p>
      ) : (
        <div className="space-y-6">
          <div className="rounded-lg border bg-muted/20 p-4 space-y-3">
            <p className="text-sm font-medium">Root hesap</p>
            {tenant.rootUser ? (
              <div className="grid gap-1 text-sm text-muted-foreground sm:grid-cols-2">
                <p>E-posta: {tenant.rootUser.email}</p>
                <p>Telefon: {tenant.rootUser.phone || '—'}</p>
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">Root kullanıcı bulunamadı.</p>
            )}
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={!tenant.rootUser || passwordResetMutation.isPending}
                onClick={() => setPasswordResetConfirmOpen(true)}
              >
                Root şifresini sıfırla
              </Button>
              <Button
                type="button"
                variant={tenant.isActive ? 'secondary' : 'default'}
                size="sm"
                disabled={statusMutation.isPending}
                onClick={() => {
                  if (tenant.isActive) {
                    setStatusConfirmOpen(true)
                    return
                  }
                  statusMutation.mutate(true)
                }}
              >
                {tenant.isActive ? 'Tenant pasife al' : 'Tenant aktifleştir'}
              </Button>
            </div>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-2">
            <div className="flex flex-wrap items-center gap-2">
              <Badge variant={tenant.isActive ? 'default' : 'secondary'}>
                {tenant.isActive ? 'Tenant aktif' : 'Tenant pasif'}
              </Badge>
              <span className="text-sm text-muted-foreground">
                {formatNumber(tenant.userCount)} kullanıcı · {formatNumber(tenant.logCount)} log
              </span>
            </div>
            <Button
              type="button"
              variant="destructive"
              size="sm"
              disabled={deleteMutation.isPending || !tenant.canDelete}
              title={tenant.deleteBlockedReason ?? undefined}
              onClick={() => {
                if (!tenant.canDelete) return
                if (deleteArmed) {
                  deleteMutation.mutate()
                  return
                }
                setDeleteArmed(true)
              }}
            >
              <Trash2 className="mr-2 h-4 w-4" />
              {deleteMutation.isPending
                ? 'Siliniyor…'
                : deleteArmed
                  ? 'Evet, kalıcı sil'
                  : 'Müşteriyi sil'}
            </Button>
            {!tenant.canDelete && tenant.deleteBlockedReason ? (
              <p className="text-xs text-muted-foreground">{tenant.deleteBlockedReason}</p>
            ) : null}
            {deleteArmed ? (
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={deleteMutation.isPending}
                onClick={() => setDeleteArmed(false)}
              >
                Vazgeç
              </Button>
            ) : null}
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
                {current.gracePeriodEndDate ? (
                  <p className="sm:col-span-2 text-status-warning">
                    Ödeme son tarihi: {formatDate(current.gracePeriodEndDate)}
                  </p>
                ) : null}
                {current.paymentReferenceNumber ? (
                  <p className="sm:col-span-2">
                    Bağlı havale:{' '}
                    <span className="text-foreground">{current.paymentReferenceNumber}</span>
                  </p>
                ) : null}
              </div>
            ) : (
              <p className="text-sm text-muted-foreground">Aktif abonelik yok.</p>
            )}
            {canCancelPackage ? (
              <div className="mt-4 space-y-3 rounded-lg border border-dashed p-3">
                <p className="text-xs font-medium text-muted-foreground">
                  Müşteri iptali — kayıt geçmişte kalır (sebep + tarih)
                </p>
                <div className="space-y-2">
                  <Label htmlFor="cancel-reason">İptal sebebi</Label>
                  <Input
                    id="cancel-reason"
                    value={cancellationReason}
                    onChange={(event) => setCancellationReason(event.target.value)}
                    placeholder="Örn. Müşteri talebi, bütçe yetersizliği"
                  />
                </div>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={!cancellationReason.trim() || cancelMutation.isPending}
                  onClick={() => cancelMutation.mutate()}
                >
                  {cancelMutation.isPending ? 'Kaydediliyor…' : 'İptali kaydet (Free pakete düşür)'}
                </Button>
              </div>
            ) : null}
            {current && current.packageCode !== 'free' ? (
              <div className="mt-3 space-y-2 rounded-lg border border-destructive/20 bg-destructive/5 p-3">
                <p className="text-xs text-muted-foreground">
                  Yanlış atama — kayıt tamamen silinir, hiç atanmamış gibi olur.
                </p>
                <Button
                  type="button"
                  variant="destructive"
                  size="sm"
                  disabled={removeSubscriptionMutation.isPending}
                  onClick={() => handleRemoveSubscription(current.id)}
                >
                  {removeSubscriptionMutation.isPending && removeSubscriptionId === current.id
                    ? 'Siliniyor…'
                    : removeSubscriptionId === current.id
                      ? 'Evet, paket kaydını sil'
                      : 'Paket kaydını sil'}
                </Button>
                {removeSubscriptionId === current.id ? (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => setRemoveSubscriptionId(null)}
                  >
                    Vazgeç
                  </Button>
                ) : null}
              </div>
            ) : null}
          </section>

          {pendingPaymentSubscription ? (
            <section className="space-y-3 rounded-xl border border-status-warning/30 bg-status-warning-muted/20 p-4">
              <h3 className="text-sm font-semibold">Mevcut aboneliğe ödeme bağla</h3>
              <p className="text-xs text-muted-foreground">
                Havale geldiğinde yeni paket atamadan, bekleyen {current.packageName} aboneliğine
                doğrudan bağlayabilirsiniz.
              </p>
              <div className="space-y-2">
                <Label htmlFor="link-payment">Havale kaydı</Label>
                <Select
                  id="link-payment"
                  value={linkPaymentId}
                  onChange={(event) => setLinkPaymentId(event.target.value)}
                >
                  <option value="">Ödeme seçin</option>
                  {availablePayments.map((payment) => (
                    <option key={payment.id} value={payment.id}>
                      {payment.packageName} · {formatMoney(payment.amount, payment.currency)} ·{' '}
                      {payment.referenceNumber} ({paymentStatusLabel(payment.status)})
                    </option>
                  ))}
                </Select>
              </div>
              <Button
                type="button"
                size="sm"
                disabled={!linkPaymentId || linkPaymentMutation.isPending}
                onClick={() => linkPaymentMutation.mutate()}
              >
                {linkPaymentMutation.isPending ? 'Bağlanıyor…' : 'Ödemeyi mevcut aboneliğe bağla'}
              </Button>
            </section>
          ) : null}

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
            {!isPaid ? (
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="sheet-grace-days">Ödeme süresi (gün)</Label>
                  <Select
                    id="sheet-grace-days"
                    value={paymentGraceDays}
                    onChange={(event) => setPaymentGraceDays(event.target.value)}
                  >
                    {PAYMENT_GRACE_DAY_OPTIONS.map((days) => (
                      <option key={days} value={String(days)}>
                        {days} gün
                      </option>
                    ))}
                    <option value="custom">Özel süre</option>
                  </Select>
                </div>
                {paymentGraceDays === 'custom' ? (
                  <div className="space-y-2">
                    <Label htmlFor="sheet-custom-grace">Gün sayısı</Label>
                    <Input
                      id="sheet-custom-grace"
                      type="number"
                      min="1"
                      max="90"
                      value={customGraceDays}
                      onChange={(event) => setCustomGraceDays(event.target.value)}
                      placeholder="Örn. 12"
                    />
                  </div>
                ) : null}
              </div>
            ) : null}
            <div className="space-y-2">
              <Label htmlFor="sheet-payment">Yeni atama için ödeme bağla (isteğe bağlı)</Label>
              <Select
                id="sheet-payment"
                value={paymentId}
                onChange={(event) => setPaymentId(event.target.value)}
              >
                <option value="">Ödeme seçilmedi</option>
                {availablePayments.map((payment) => (
                  <option key={payment.id} value={payment.id}>
                    {payment.packageName} · {formatMoney(payment.amount, payment.currency)} ·{' '}
                    {payment.referenceNumber} ({paymentStatusLabel(payment.status)})
                  </option>
                ))}
              </Select>
              <p className="text-xs text-muted-foreground">
                Ödeme seçerseniz abonelik anında aktif olur. Seçmezseniz yukarıdaki süre kadar
                ödeme beklenir.
              </p>
            </div>
            <Button
              type="button"
              disabled={!packageId || assignMutation.isPending}
              onClick={() => assignMutation.mutate()}
            >
              {assignMutation.isPending ? 'Kaydediliyor…' : 'Aboneliği uygula'}
            </Button>
          </section>

          <section className="space-y-3">
            <h3 className="text-sm font-semibold">Ödeme geçmişi</h3>
            {tenantPaymentsQuery.isLoading ? (
              <p className="text-sm text-muted-foreground">Ödemeler yükleniyor…</p>
            ) : tenantPayments.length === 0 ? (
              <p className="text-sm text-muted-foreground">Bu müşteri için ödeme kaydı yok.</p>
            ) : (
              tenantPayments.map((payment) => (
                <div key={payment.id} className="rounded-lg border px-3 py-2 text-sm">
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-medium">
                      {payment.packageName} · {formatMoney(payment.amount, payment.currency)}
                    </span>
                    <Badge variant="secondary">{paymentStatusLabel(payment.status)}</Badge>
                  </div>
                  <p className="text-muted-foreground">{payment.referenceNumber}</p>
                  <p className="text-xs text-muted-foreground">
                    {payment.linkedSubscriptionId
                      ? `Abonelik #${payment.linkedSubscriptionId} ile bağlı`
                      : 'Aboneliğe bağlanmadı'}
                  </p>
                </div>
              ))
            )}
          </section>

          <section className="space-y-3">
            <h3 className="text-sm font-semibold">Abonelik geçmişi</h3>
            {tenant.subscriptionHistory.filter((item) => item.id !== current?.id).length === 0 ? (
              <p className="text-sm text-muted-foreground">Geçmiş kayıt yok.</p>
            ) : (
              tenant.subscriptionHistory
                .filter((item) => item.id !== current?.id)
                .map((item) => (
                  <div key={item.id} className="rounded-lg border px-3 py-2 text-sm">
                  <div className="flex items-center justify-between gap-2">
                    <span className="font-medium">{item.packageName}</span>
                    <Badge variant="secondary">{subscriptionStatusLabel(item.status)}</Badge>
                  </div>
                  <p className="text-muted-foreground">
                    {formatDate(item.startDate)} → {formatDate(item.endDate)}
                  </p>
                  {item.cancelledAt ? (
                    <p className="text-xs text-muted-foreground">
                      İptal: {formatDate(item.cancelledAt)}
                      {item.cancellationReason ? ` · ${item.cancellationReason}` : null}
                    </p>
                  ) : null}
                  <div className="mt-2 flex flex-wrap gap-2">
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="h-8 px-2 text-destructive hover:text-destructive"
                      disabled={removeSubscriptionMutation.isPending}
                      onClick={() => handleRemoveSubscription(item.id)}
                    >
                      {removeSubscriptionId === item.id
                        ? 'Evet, sil'
                        : 'Kaydı sil'}
                    </Button>
                    {removeSubscriptionId === item.id ? (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        className="h-8 px-2"
                        onClick={() => setRemoveSubscriptionId(null)}
                      >
                        Vazgeç
                      </Button>
                    ) : null}
                  </div>
                </div>
              ))
            )}
          </section>
        </div>
      )}
    </Sheet>

    <ConfirmDialog
      open={statusConfirmOpen}
      onClose={() => setStatusConfirmOpen(false)}
      onConfirm={() => statusMutation.mutate(false)}
      title="Tenant pasife alınsın mı?"
      description="Pasif tenant kullanıcıları giriş yapamaz ve API erişimi engellenir. Veriler silinmez; tekrar aktifleştirebilirsiniz."
      confirmLabel="Pasife al"
      variant="destructive"
      isLoading={statusMutation.isPending}
    />

    <ConfirmDialog
      open={passwordResetConfirmOpen}
      onClose={() => setPasswordResetConfirmOpen(false)}
      onConfirm={() => passwordResetMutation.mutate()}
      title="Root şifresi sıfırlansın mı?"
      description="Yeni geçici şifre yalnızca bir kez gösterilir. Root kullanıcı kendi şifresini değiştiremez; şifreyi siz iletmelisiniz."
      confirmLabel="Sıfırla"
      variant="destructive"
      isLoading={passwordResetMutation.isPending}
    />

    <Dialog
      open={temporaryPassword !== null}
      onClose={() => setTemporaryPassword(null)}
      title="Geçici root şifresi"
      description="Bu şifreyi güvenli bir kanalla müşteriye iletin. Pencere kapatıldığında tekrar gösterilmez."
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={() => setTemporaryPassword(null)}>
            Kapat
          </Button>
          <Button onClick={copyTemporaryPassword}>Panoya kopyala</Button>
        </div>
      }
    >
      <code className="block rounded-lg border bg-muted/40 px-4 py-3 text-sm font-mono break-all">
        {temporaryPassword}
      </code>
    </Dialog>
    </>
  )
}
