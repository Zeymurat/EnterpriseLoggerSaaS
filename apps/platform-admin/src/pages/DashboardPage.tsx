import { Link } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { AlertTriangle, Building2, CreditCard, RefreshCw, TrendingUp } from 'lucide-react'
import { getPlatformDashboard } from '@/lib/api'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

function formatNumber(value: number): string {
  return new Intl.NumberFormat('tr-TR').format(value)
}

function StatCard({
  title,
  value,
  icon: Icon,
  tone,
}: {
  title: string
  value: number
  icon: typeof Building2
  tone?: 'warning' | 'danger'
}) {
  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon
          className={
            tone === 'danger'
              ? 'h-4 w-4 text-destructive'
              : tone === 'warning'
                ? 'h-4 w-4 text-status-warning'
                : 'h-4 w-4 text-muted-foreground'
          }
        />
      </CardHeader>
      <CardContent>
        <p className="text-2xl font-semibold tabular-nums">{formatNumber(value)}</p>
      </CardContent>
    </Card>
  )
}

export function DashboardPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['platform-dashboard'],
    queryFn: getPlatformDashboard,
  })

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Özet yükleniyor…</p>
  }

  if (isError || !data) {
    return <p className="text-sm text-destructive">Özet alınamadı.</p>
  }

  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Kontrol Paneli</h1>
          <p className="text-sm text-muted-foreground">Platform operasyon özeti.</p>
        </div>
        <div className="flex gap-2">
          <Button asChild variant="outline" size="sm">
            <Link to="/renewals">Yenilemeler</Link>
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link to="/audit-logs">Denetim kayıtları</Link>
          </Button>
        </div>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard title="Toplam müşteri" value={data.totalTenants} icon={Building2} />
        <StatCard title="Aktif müşteri" value={data.activeTenants} icon={Building2} />
        <StatCard title="Bekleyen ödeme" value={data.pendingPayments} icon={CreditCard} tone="warning" />
        <StatCard title="Ödeme bekleyen abonelik" value={data.pendingRenewals} icon={RefreshCw} tone="warning" />
      </div>

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard title="7 gün içinde yenilenecek" value={data.upcomingRenewals} icon={RefreshCw} />
        <StatCard title="Grace süresi bitiyor" value={data.graceExpiringSoon} icon={AlertTriangle} tone="danger" />
        <StatCard title="Yüksek kota kullanımı" value={data.highQuotaTenants} icon={TrendingUp} tone="warning" />
      </div>

      {data.topQuotaTenants.length > 0 ? (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">En yüksek kota kullanımı</CardTitle>
          </CardHeader>
          <CardContent className="space-y-3">
            {data.topQuotaTenants.map((item) => (
              <div
                key={item.tenantId}
                className="flex flex-col gap-2 border-b border-border/50 pb-3 last:border-0 last:pb-0 sm:flex-row sm:items-center sm:justify-between"
              >
                <div>
                  <p className="font-medium">{item.tenantName}</p>
                  <p className="text-sm text-muted-foreground">{item.packageName}</p>
                </div>
                <div className="flex items-center gap-3">
                  <span className="text-sm tabular-nums text-muted-foreground">
                    {formatNumber(item.monthlyLogCount)} / {formatNumber(item.monthlyLimit)}
                  </span>
                  <Badge variant={item.usagePercent >= 90 ? 'destructive' : 'secondary'}>
                    %{item.usagePercent}
                  </Badge>
                </div>
              </div>
            ))}
          </CardContent>
        </Card>
      ) : null}
    </div>
  )
}
