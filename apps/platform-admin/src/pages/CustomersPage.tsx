import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { getPlatformTenants, subscriptionStatusLabel } from '@/lib/api'
import { TenantManageSheet } from '@/components/tenants/TenantManageSheet'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

function formatNumber(value: number): string {
  return new Intl.NumberFormat('tr-TR').format(value)
}

export function CustomersPage() {
  const [selectedTenantId, setSelectedTenantId] = useState<number | null>(null)
  const [sheetOpen, setSheetOpen] = useState(false)

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['platform-tenants'],
    queryFn: getPlatformTenants,
  })

  const openTenantSheet = (tenantId: number) => {
    setSelectedTenantId(tenantId)
    setSheetOpen(true)
  }

  const closeTenantSheet = () => {
    setSheetOpen(false)
    setSelectedTenantId(null)
  }

  if (isLoading) {
    return <p className="text-sm text-muted-foreground">Müşteri listesi yükleniyor…</p>
  }

  if (isError) {
    return (
      <p className="text-sm text-destructive">
        {error instanceof Error ? error.message : 'Müşteri listesi alınamadı.'}
      </p>
    )
  }

  const tenants = data?.tenants ?? []

  return (
    <>
      <div className="space-y-6">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Müşteriler</h1>
          <p className="text-sm text-muted-foreground">
            Kayıtlı tenantlar, paket durumları ve abonelik yönetimi.
          </p>
        </div>

        {tenants.length === 0 ? (
          <p className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
            Henüz kayıtlı müşteri yok.
          </p>
        ) : (
          <div className="grid gap-4">
            {tenants.map((tenant) => (
              <Card key={tenant.id}>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <div className="space-y-1">
                    <CardTitle className="text-base font-semibold">{tenant.name}</CardTitle>
                    {tenant.currentPackageName ? (
                      <p className="text-sm text-muted-foreground">
                        {tenant.currentPackageName}
                        {tenant.currentSubscriptionStatus !== null
                          ? ` · ${subscriptionStatusLabel(tenant.currentSubscriptionStatus)}`
                          : null}
                      </p>
                    ) : (
                      <p className="text-sm text-muted-foreground">Paket atanmamış</p>
                    )}
                  </div>
                  <Badge variant={tenant.isActive ? 'default' : 'secondary'}>
                    {tenant.isActive ? 'Aktif' : 'Pasif'}
                  </Badge>
                </CardHeader>
                <CardContent className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                  <div className="grid gap-1 text-sm text-muted-foreground sm:grid-cols-3 sm:gap-4">
                    <p>Kayıt: {formatDate(tenant.createdAt)}</p>
                    <p>Kullanıcı: {formatNumber(tenant.userCount)}</p>
                    <p>Log: {formatNumber(tenant.logCount)}</p>
                  </div>
                  <Button variant="outline" size="sm" onClick={() => openTenantSheet(tenant.id)}>
                    Yönet
                  </Button>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>

      <TenantManageSheet
        tenantId={selectedTenantId}
        open={sheetOpen}
        onClose={closeTenantSheet}
      />
    </>
  )
}
