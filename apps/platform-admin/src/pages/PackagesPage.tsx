import { useQuery } from '@tanstack/react-query'
import { getPlatformPackages } from '@/lib/api'
import { Badge } from '@/components/ui/badge'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

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

export function PackagesPage() {
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['platform-packages'],
    queryFn: getPlatformPackages,
  })

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Paketler yükleniyor…</p>
  }

  if (isError) {
    return (
      <p className="text-sm text-destructive">
        {error instanceof Error ? error.message : 'Paket listesi alınamadı.'}
      </p>
    )
  }

  const packages = data?.packages ?? []

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Paketler</h1>
        <p className="text-sm text-muted-foreground">
          Abonelik planları, kota limitleri ve fiyat kırılımları.
        </p>
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        {packages.map((pkg) => (
          <Card key={pkg.id}>
            <CardHeader className="flex flex-row items-start justify-between space-y-0">
              <div>
                <CardTitle className="text-base">{pkg.name}</CardTitle>
                <p className="mt-1 text-sm text-muted-foreground">{pkg.description}</p>
              </div>
              <div className="flex gap-2">
                {pkg.isDefault ? <Badge>Varsayılan</Badge> : null}
                <Badge variant={pkg.isAvailable ? 'default' : 'secondary'}>
                  {pkg.isAvailable ? 'Satışta' : 'Kapalı'}
                </Badge>
              </div>
            </CardHeader>
            <CardContent className="grid gap-2 text-sm text-muted-foreground sm:grid-cols-2">
              <p>Dakika limiti: {formatNumber(pkg.maxLogsPerMinute)}</p>
              <p>Aylık log kotası: {formatNumber(pkg.monthlyRequestLimit)}</p>
              <p>Saklama: {formatNumber(pkg.storageRetentionDays)} gün</p>
              <p>Log seviyeleri: {pkg.allowedLogLevels}</p>
              <p>Aylık: {formatPrice(pkg.priceMonthly)}</p>
              <p>Yıllık: {formatPrice(pkg.priceAnnual)}</p>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
