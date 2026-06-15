import { useAuth } from '@/contexts/AuthContext'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { getApiUrl } from '@/lib/api'

export function DashboardPage() {
  const { user } = useAuth()

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Dashboard</h1>
        <p className="text-muted-foreground">Tenant yönetim paneline hoş geldiniz.</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Oturum</CardTitle>
            <CardDescription>Aktif kullanıcı ve tenant bilgisi</CardDescription>
          </CardHeader>
          <CardContent className="space-y-2 text-sm">
            <p><span className="font-medium">E-posta:</span> {user?.email}</p>
            <p><span className="font-medium">Rol:</span> {user?.role}</p>
            <p><span className="font-medium">Tenant:</span> {user?.tenantName}</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>İzinler</CardTitle>
            <CardDescription>JWT içindeki permission claim&apos;leri</CardDescription>
          </CardHeader>
          <CardContent>
            <ul className="list-inside list-disc text-sm text-muted-foreground">
              {(user?.permissions.length ? user.permissions : ['Tam yetki (Root/Admin)']).map((p) => (
                <li key={p}>{p}</li>
              ))}
            </ul>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Sonraki adımlar</CardTitle>
          <CardDescription>Bu PR iskelet kurar; sayfalar sonraki PR&apos;larda gelir</CardDescription>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          <p>API: <code className="rounded bg-muted px-1 py-0.5">{getApiUrl()}</code></p>
          <p className="mt-2">Logs ve Users sayfaları bir sonraki PR&apos;da eklenecek.</p>
        </CardContent>
      </Card>
    </div>
  )
}
