import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  BillingCycle,
  billingCycleLabel,
  confirmPlatformPayment,
  deletePlatformPayment,
  getPlatformPackages,
  getPlatformPayments,
  getPlatformTenants,
  PaymentStatus,
  paymentStatusLabel,
  packagePriceForCycle,
  recordPlatformPayment,
  rejectPlatformPayment,
  updatePlatformPayment,
  type BillingCycleValue,
  type PaymentStatusValue,
  type PlatformPayment,
} from '@/lib/api'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select } from '@/components/ui/select'
import { toast } from 'sonner'

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium' }).format(new Date(value))
}

function formatMoney(value: number, currency: string): string {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency,
    maximumFractionDigits: 0,
  }).format(value)
}

const tabs: { key: 'pending' | 'history' | 'rejected'; label: string; status?: PaymentStatusValue }[] = [
  { key: 'pending', label: 'Bekleyen Havaleler', status: PaymentStatus.Pending },
  { key: 'history', label: 'Onaylı Geçmiş', status: PaymentStatus.Confirmed },
  { key: 'rejected', label: 'Reddedilen', status: PaymentStatus.Rejected },
]

export function PaymentsPage() {
  const [tab, setTab] = useState<'pending' | 'history' | 'rejected'>('pending')
  const [showRecordForm, setShowRecordForm] = useState(false)
  const [tenantId, setTenantId] = useState('')
  const [packageId, setPackageId] = useState('')
  const [billingCycle, setBillingCycle] = useState(String(BillingCycle.Monthly))
  const [amount, setAmount] = useState('')
  const [referenceNumber, setReferenceNumber] = useState('')
  const [periodStart, setPeriodStart] = useState(() => new Date().toISOString().slice(0, 10))
  const [notes, setNotes] = useState('')
  const [editingPayment, setEditingPayment] = useState<PlatformPayment | null>(null)
  const [editPackageId, setEditPackageId] = useState('')
  const [editBillingCycle, setEditBillingCycle] = useState(String(BillingCycle.Monthly))
  const [editAmount, setEditAmount] = useState('')
  const [editReferenceNumber, setEditReferenceNumber] = useState('')
  const [editPeriodStart, setEditPeriodStart] = useState('')
  const [editNotes, setEditNotes] = useState('')
  const [deletePaymentId, setDeletePaymentId] = useState<number | null>(null)
  const queryClient = useQueryClient()
  const activeTab = tabs.find((item) => item.key === tab)!

  const tenantsQuery = useQuery({
    queryKey: ['platform-tenants'],
    queryFn: () => getPlatformTenants({ pageSize: 100 }),
  })

  const packagesQuery = useQuery({
    queryKey: ['platform-packages'],
    queryFn: getPlatformPackages,
  })

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['platform-payments', activeTab.status],
    queryFn: () => getPlatformPayments(activeTab.status),
  })

  const confirmMutation = useMutation({
    mutationFn: confirmPlatformPayment,
    onSuccess: async () => {
      toast.success('Ödeme onaylandı.')
      await queryClient.invalidateQueries({ queryKey: ['platform-payments'] })
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Onay başarısız.'),
  })

  const rejectMutation = useMutation({
    mutationFn: rejectPlatformPayment,
    onSuccess: async () => {
      toast.success('Ödeme reddedildi.')
      await queryClient.invalidateQueries({ queryKey: ['platform-payments'] })
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Red başarısız.'),
  })

  const recordMutation = useMutation({
    mutationFn: recordPlatformPayment,
    onSuccess: async () => {
      toast.success('Havale kaydı oluşturuldu.')
      setShowRecordForm(false)
      setReferenceNumber('')
      setAmount('')
      setNotes('')
      await queryClient.invalidateQueries({ queryKey: ['platform-payments'] })
      await queryClient.invalidateQueries({ queryKey: ['tenant-available-payments'] })
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Kayıt başarısız.'),
  })

  const updateMutation = useMutation({
    mutationFn: () =>
      updatePlatformPayment(editingPayment!.id, {
        packageId: Number(editPackageId),
        billingCycle: Number(editBillingCycle) as BillingCycleValue,
        amount: Number(editAmount),
        referenceNumber: editReferenceNumber.trim(),
        periodStart: new Date(editPeriodStart).toISOString(),
        notes: editNotes.trim() || undefined,
      }),
    onSuccess: async () => {
      toast.success('Ödeme güncellendi.')
      setEditingPayment(null)
      await queryClient.invalidateQueries({ queryKey: ['platform-payments'] })
      await queryClient.invalidateQueries({ queryKey: ['tenant-available-payments'] })
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Güncelleme başarısız.'),
  })

  const deletePaymentMutation = useMutation({
    mutationFn: deletePlatformPayment,
    onSuccess: async () => {
      toast.success('Ödeme kaydı silindi.')
      setDeletePaymentId(null)
      await queryClient.invalidateQueries({ queryKey: ['platform-payments'] })
      await queryClient.invalidateQueries({ queryKey: ['tenant-available-payments'] })
    },
    onError: (err) => toast.error(err instanceof Error ? err.message : 'Silme başarısız.'),
  })

  const payments = data?.payments ?? []
  const tenants = tenantsQuery.data?.tenants ?? []
  const packages = packagesQuery.data?.packages.filter((pkg) => pkg.isAvailable) ?? []

  const selectedPackage = packages.find((pkg) => String(pkg.id) === packageId)

  function applyPackagePrice(pkg: (typeof packages)[number], cycle: BillingCycleValue) {
    const price = packagePriceForCycle(pkg, cycle)
    if (price > 0) setAmount(String(price))
  }

  function handleRecordSubmit() {
    if (!tenantId || !packageId || !amount || !referenceNumber.trim()) {
      toast.error('Müşteri, paket, tutar ve referans zorunludur.')
      return
    }

    recordMutation.mutate({
      tenantId: Number(tenantId),
      packageId: Number(packageId),
      billingCycle: Number(billingCycle) as BillingCycleValue,
      amount: Number(amount),
      referenceNumber: referenceNumber.trim(),
      periodStart: new Date(periodStart).toISOString(),
      notes: notes.trim() || undefined,
    })
  }

  function startEditPayment(payment: PlatformPayment) {
    setEditingPayment(payment)
    setEditPackageId(String(payment.packageId))
    setEditBillingCycle(String(payment.billingCycle))
    setEditAmount(String(payment.amount))
    setEditReferenceNumber(payment.referenceNumber)
    setEditPeriodStart(payment.periodStart.slice(0, 10))
    setEditNotes(payment.notes)
  }

  function handleDeletePayment(paymentId: number) {
    if (deletePaymentId === paymentId) {
      deletePaymentMutation.mutate(paymentId)
      return
    }
    setDeletePaymentId(paymentId)
  }

  const editSelectedPackage = packages.find((pkg) => String(pkg.id) === editPackageId)

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Ödemeler</h1>
        <p className="text-sm text-muted-foreground">
          Havale/EFT kayıtları, onay ve abonelik eşleştirme merkezi.
        </p>
      </div>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0">
          <div>
            <CardTitle className="text-base">Havale girişi</CardTitle>
            <p className="text-sm text-muted-foreground">
              Bankadan gelen EFT/havaleyi bekleyen ödeme olarak kaydedin.
            </p>
          </div>
          <Button variant="outline" size="sm" onClick={() => setShowRecordForm((value) => !value)}>
            {showRecordForm ? 'Kapat' : 'Yeni kayıt'}
          </Button>
        </CardHeader>
        {showRecordForm ? (
          <CardContent className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="payment-tenant">Müşteri</Label>
              <Select
                id="payment-tenant"
                value={tenantId}
                onChange={(event) => setTenantId(event.target.value)}
              >
                <option value="">Müşteri seçin</option>
                {tenants.map((tenant) => (
                  <option key={tenant.id} value={tenant.id}>
                    {tenant.name}
                  </option>
                ))}
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="payment-package">Paket</Label>
              <Select
                id="payment-package"
                value={packageId}
                onChange={(event) => {
                  const nextPackageId = event.target.value
                  setPackageId(nextPackageId)
                  const pkg = packages.find((item) => String(item.id) === nextPackageId)
                  if (pkg) {
                    applyPackagePrice(pkg, Number(billingCycle) as BillingCycleValue)
                  }
                }}
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
              <Label htmlFor="payment-cycle">Dönem</Label>
              <Select
                id="payment-cycle"
                value={billingCycle}
                onChange={(event) => {
                  const nextCycle = Number(event.target.value) as BillingCycleValue
                  setBillingCycle(event.target.value)
                  if (selectedPackage) {
                    applyPackagePrice(selectedPackage, nextCycle)
                  }
                }}
              >
                {Object.entries(BillingCycle).map(([label, value]) => (
                  <option key={label} value={value}>
                    {billingCycleLabel(value)}
                  </option>
                ))}
              </Select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="payment-amount">Tutar (TRY)</Label>
              <Input
                id="payment-amount"
                type="number"
                min="1"
                value={amount}
                onChange={(event) => setAmount(event.target.value)}
                placeholder={selectedPackage ? 'Paket fiyatı otomatik dolar' : 'Tutar'}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="payment-reference">Dekont / açıklama kodu</Label>
              <Input
                id="payment-reference"
                value={referenceNumber}
                onChange={(event) => setReferenceNumber(event.target.value)}
                placeholder="Havale açıklaması"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="payment-period">Dönem başlangıcı</Label>
              <Input
                id="payment-period"
                type="date"
                value={periodStart}
                onChange={(event) => setPeriodStart(event.target.value)}
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="payment-notes">Not (isteğe bağlı)</Label>
              <Input
                id="payment-notes"
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
              />
            </div>
            <div className="sm:col-span-2">
              <Button disabled={recordMutation.isPending} onClick={handleRecordSubmit}>
                {recordMutation.isPending ? 'Kaydediliyor…' : 'Havale kaydını oluştur'}
              </Button>
            </div>
          </CardContent>
        ) : null}
      </Card>

      <div className="flex gap-2">
        {tabs.map((item) => (
          <Button
            key={item.key}
            variant={tab === item.key ? 'default' : 'outline'}
            size="sm"
            onClick={() => setTab(item.key)}
          >
            {item.label}
          </Button>
        ))}
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Ödemeler yükleniyor…</p>
      ) : isError ? (
        <p className="text-sm text-destructive">
          {error instanceof Error ? error.message : 'Ödemeler alınamadı.'}
        </p>
      ) : payments.length === 0 ? (
        <p className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
          Bu sekmede kayıt yok.
        </p>
      ) : (
        <div className="grid gap-4">
          {payments.map((payment) => (
            <Card key={payment.id}>
              <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2">
                <div>
                  <CardTitle className="text-base">{payment.tenantName}</CardTitle>
                  <p className="text-sm text-muted-foreground">
                    {payment.packageName} · {payment.referenceNumber}
                  </p>
                </div>
                <Badge>{paymentStatusLabel(payment.status)}</Badge>
              </CardHeader>
              <CardContent className="space-y-3 text-sm">
                {editingPayment?.id === payment.id ? (
                  <div className="grid gap-3 sm:grid-cols-2">
                    <div className="space-y-2 sm:col-span-2">
                      <Label>Paket</Label>
                      <Select
                        value={editPackageId}
                        onChange={(event) => {
                          const nextId = event.target.value
                          setEditPackageId(nextId)
                          const pkg = packages.find((item) => String(item.id) === nextId)
                          if (pkg) {
                            const price = packagePriceForCycle(
                              pkg,
                              Number(editBillingCycle) as BillingCycleValue,
                            )
                            if (price > 0) setEditAmount(String(price))
                          }
                        }}
                      >
                        {packages.map((pkg) => (
                          <option key={pkg.id} value={pkg.id}>
                            {pkg.name}
                          </option>
                        ))}
                      </Select>
                    </div>
                    <div className="space-y-2">
                      <Label>Dönem</Label>
                      <Select
                        value={editBillingCycle}
                        onChange={(event) => {
                          const cycle = Number(event.target.value) as BillingCycleValue
                          setEditBillingCycle(event.target.value)
                          if (editSelectedPackage) {
                            const price = packagePriceForCycle(editSelectedPackage, cycle)
                            if (price > 0) setEditAmount(String(price))
                          }
                        }}
                      >
                        {Object.entries(BillingCycle).map(([label, value]) => (
                          <option key={label} value={value}>
                            {billingCycleLabel(value)}
                          </option>
                        ))}
                      </Select>
                    </div>
                    <div className="space-y-2">
                      <Label>Tutar</Label>
                      <Input
                        type="number"
                        min="1"
                        value={editAmount}
                        onChange={(event) => setEditAmount(event.target.value)}
                      />
                    </div>
                    <div className="space-y-2 sm:col-span-2">
                      <Label>Referans</Label>
                      <Input
                        value={editReferenceNumber}
                        onChange={(event) => setEditReferenceNumber(event.target.value)}
                      />
                    </div>
                    <div className="space-y-2">
                      <Label>Dönem başlangıcı</Label>
                      <Input
                        type="date"
                        value={editPeriodStart}
                        onChange={(event) => setEditPeriodStart(event.target.value)}
                      />
                    </div>
                    <div className="space-y-2">
                      <Label>Not</Label>
                      <Input
                        value={editNotes}
                        onChange={(event) => setEditNotes(event.target.value)}
                      />
                    </div>
                    <div className="flex flex-wrap gap-2 sm:col-span-2">
                      <Button
                        size="sm"
                        disabled={updateMutation.isPending}
                        onClick={() => updateMutation.mutate()}
                      >
                        {updateMutation.isPending ? 'Kaydediliyor…' : 'Kaydet'}
                      </Button>
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => setEditingPayment(null)}
                      >
                        Vazgeç
                      </Button>
                    </div>
                  </div>
                ) : (
                  <>
                    <p className="font-medium">{formatMoney(payment.amount, payment.currency)}</p>
                    <p className="text-muted-foreground">
                      Dönem: {formatDate(payment.periodStart)} → {formatDate(payment.periodEnd)}
                    </p>
                    {payment.notes ? <p className="text-muted-foreground">{payment.notes}</p> : null}
                    {payment.status === PaymentStatus.Pending ? (
                      <div className="flex flex-wrap gap-2 pt-1">
                        <Button
                          size="sm"
                          disabled={confirmMutation.isPending}
                          onClick={() => confirmMutation.mutate(payment.id)}
                        >
                          Onayla
                        </Button>
                        <Button
                          size="sm"
                          variant="outline"
                          disabled={rejectMutation.isPending}
                          onClick={() => rejectMutation.mutate(payment.id)}
                        >
                          Reddet
                        </Button>
                        {!payment.linkedSubscriptionId ? (
                          <>
                            <Button
                              size="sm"
                              variant="secondary"
                              onClick={() => startEditPayment(payment)}
                            >
                              Düzenle
                            </Button>
                            <Button
                              size="sm"
                              variant="destructive"
                              disabled={deletePaymentMutation.isPending}
                              onClick={() => handleDeletePayment(payment.id)}
                            >
                              {deletePaymentId === payment.id ? 'Evet, sil' : 'Sil'}
                            </Button>
                          </>
                        ) : null}
                      </div>
                    ) : payment.status === PaymentStatus.Rejected ? (
                      <div className="flex flex-wrap gap-2 pt-1">
                        {!payment.linkedSubscriptionId ? (
                          <Button
                            size="sm"
                            variant="destructive"
                            disabled={deletePaymentMutation.isPending}
                            onClick={() => handleDeletePayment(payment.id)}
                          >
                            {deletePaymentId === payment.id ? 'Evet, sil' : 'Sil'}
                          </Button>
                        ) : null}
                        {deletePaymentId === payment.id ? (
                          <Button size="sm" variant="ghost" onClick={() => setDeletePaymentId(null)}>
                            Vazgeç
                          </Button>
                        ) : null}
                      </div>
                    ) : (
                      <p className="text-xs text-muted-foreground">
                        {payment.linkedSubscriptionId
                          ? `Abonelik #${payment.linkedSubscriptionId} ile bağlı. Onaylı ödemeler silinemez veya düzenlenemez.`
                          : 'Henüz aboneliğe bağlanmadı. Onaylı ödemeler silinemez veya düzenlenemez.'}
                      </p>
                    )}
                  </>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
