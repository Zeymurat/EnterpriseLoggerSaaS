import { useQuery } from '@tanstack/react-query'
import {
  getPlatformRenewals,
  renewalCategoryLabel,
  subscriptionStatusLabel,
} from '@/lib/api'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('tr-TR', { dateStyle: 'medium' }).format(new Date(value))
}

function categoryVariant(category: string): 'default' | 'secondary' | 'destructive' {
  if (category === 'grace_expired') return 'destructive'
  if (category === 'payment_pending') return 'secondary'
  return 'default'
}

export function RenewalsPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['platform-renewals'],
    queryFn: getPlatformRenewals,
  })

  const items = data?.items ?? []

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Yenilemeler</h1>
        <p className="text-sm text-muted-foreground">
          Ödeme bekleyen, yaklaşan ve grace süresi dolan abonelikler.
        </p>
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Liste yükleniyor…</p>
      ) : isError ? (
        <p className="text-sm text-destructive">Liste alınamadı.</p>
      ) : items.length === 0 ? (
        <p className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
          Görüntülenecek yenileme kaydı yok.
        </p>
      ) : (
        <div className="grid gap-4">
          {items.map((item) => (
            <Card key={`${item.tenantId}-${item.startDate}`}>
              <CardHeader className="flex flex-row items-start justify-between space-y-0 pb-2">
                <div className="space-y-1">
                  <CardTitle className="text-base">{item.tenantName}</CardTitle>
                  <p className="text-sm text-muted-foreground">
                    {item.packageName} · {subscriptionStatusLabel(item.status)}
                  </p>
                </div>
                <Badge variant={categoryVariant(item.category)}>
                  {renewalCategoryLabel(item.category)}
                </Badge>
              </CardHeader>
              <CardContent className="grid gap-1 text-sm text-muted-foreground sm:grid-cols-3">
                <p>Başlangıç: {formatDate(item.startDate)}</p>
                <p>Bitiş: {formatDate(item.endDate)}</p>
                <p>
                  Grace: {item.gracePeriodEndDate ? formatDate(item.gracePeriodEndDate) : '—'}
                </p>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
