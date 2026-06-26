import { useQuery } from '@tanstack/react-query'
import { AlertTriangle } from 'lucide-react'
import { getBillingNotice } from '@/lib/api'

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('tr-TR', { dateStyle: 'long' }).format(new Date(value))
}

export function BillingNoticeBanner() {
  const { data } = useQuery({
    queryKey: ['billing-notice'],
    queryFn: getBillingNotice,
    staleTime: 60_000,
  })

  if (!data?.showNotice) {
    return null
  }

  const message =
    data.message ??
    `${data.packageName ?? 'Aboneliğiniz'} yenilendi. Havale/EFT ödemenizi ${data.paymentDueBy ? formatDate(data.paymentDueBy) : 'en kısa sürede'} tamamlayın.`

  return (
    <div
      role="status"
      className="mb-6 flex items-start gap-3 rounded-xl border border-status-warning/30 bg-status-warning-muted px-4 py-3 text-sm text-foreground"
    >
      <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0 text-status-warning" />
      <div className="space-y-1">
        <p className="font-medium">Ödeme bekleniyor</p>
        <p className="text-muted-foreground">{message}</p>
        {data.periodEnd ? (
          <p className="text-xs text-muted-foreground">
            Dönem sonu: {formatDate(data.periodEnd)}
          </p>
        ) : null}
      </div>
    </div>
  )
}
