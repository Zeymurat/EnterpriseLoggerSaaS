import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, CreditCard } from 'lucide-react'
import {
  getTenantBillingOverview,
  paymentStatusLabel,
  type PaymentStatusValue,
} from '@/lib/api'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('tr-TR', { dateStyle: 'long' }).format(new Date(value))
}

function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat('tr-TR', {
    style: 'currency',
    currency: currency || 'TRY',
  }).format(amount)
}

function subscriptionStatusLabel(status: number | null): string {
  switch (status) {
    case 0:
      return 'Aktif'
    case 1:
      return 'Ödeme bekliyor'
    case 2:
      return 'Gecikmiş'
    case 3:
      return 'İptal'
    case 4:
      return 'Kapatıldı'
    default:
      return '—'
  }
}

export function BillingPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['tenant-billing-overview'],
    queryFn: getTenantBillingOverview,
  })

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Abonelik bilgisi yükleniyor…</p>
  }

  if (isError || !data) {
    return <p className="text-sm text-destructive">Abonelik bilgisi alınamadı.</p>
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Abonelik & Ödeme</h1>
        <p className="text-sm text-muted-foreground">Paket durumu, dönemler ve ödeme geçmişi.</p>
      </div>

      {data.showPaymentNotice ? (
        <div className="flex items-start gap-3 rounded-xl border border-status-warning/30 bg-status-warning-muted px-4 py-3 text-sm">
          <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-status-warning" />
          <p>{data.paymentNoticeMessage}</p>
        </div>
      ) : null}

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Aktif abonelik</CardTitle>
        </CardHeader>
        <CardContent className="grid gap-2 text-sm sm:grid-cols-2">
          {data.hasActiveSubscription ? (
            <>
              <p>
                <span className="text-muted-foreground">Paket:</span> {data.packageName}
              </p>
              <p>
                <span className="text-muted-foreground">Durum:</span>{' '}
                {subscriptionStatusLabel(data.status)}
              </p>
              <p>
                <span className="text-muted-foreground">Dönem:</span>{' '}
                {data.startDate && data.endDate
                  ? `${formatDate(data.startDate)} – ${formatDate(data.endDate)}`
                  : '—'}
              </p>
              <p>
                <span className="text-muted-foreground">Grace:</span>{' '}
                {data.gracePeriodEndDate ? formatDate(data.gracePeriodEndDate) : '—'}
              </p>
              <p>
                <span className="text-muted-foreground">Log saklama:</span>{' '}
                {data.storageRetentionDays} gün
              </p>
              <p>
                <span className="text-muted-foreground">Aylık kota:</span>{' '}
                {new Intl.NumberFormat('tr-TR').format(data.monthlyRequestLimit)} log
              </p>
            </>
          ) : (
            <p className="text-muted-foreground">Aktif abonelik bulunamadı.</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader className="flex flex-row items-center gap-2 space-y-0">
          <CreditCard className="h-4 w-4 text-muted-foreground" />
          <CardTitle className="text-base">Ödeme geçmişi</CardTitle>
        </CardHeader>
        <CardContent>
          {data.recentPayments.length === 0 ? (
            <p className="text-sm text-muted-foreground">Henüz ödeme kaydı yok.</p>
          ) : (
            <div className="space-y-3">
              {data.recentPayments.map((payment) => (
                <div
                  key={payment.id}
                  className="flex flex-col gap-2 border-b border-border/50 pb-3 last:border-0 last:pb-0 sm:flex-row sm:items-center sm:justify-between"
                >
                  <div>
                    <p className="font-medium">{formatMoney(payment.amount, payment.currency)}</p>
                    <p className="text-sm text-muted-foreground">
                      Ref: {payment.referenceNumber || '—'}
                    </p>
                  </div>
                  <div className="flex items-center gap-3 text-sm text-muted-foreground">
                    <span>
                      {formatDate(payment.periodStart)} – {formatDate(payment.periodEnd)}
                    </span>
                    <Badge variant="secondary">
                      {paymentStatusLabel(payment.status as PaymentStatusValue)}
                    </Badge>
                  </div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
