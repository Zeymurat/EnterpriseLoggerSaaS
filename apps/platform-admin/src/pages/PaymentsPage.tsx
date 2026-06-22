import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function PaymentsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Ödemeler</h1>
        <p className="text-sm text-muted-foreground">
          Tahsilat takibi, geciken ödemeler ve fatura geçmişi.
        </p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Yakında</CardTitle>
        </CardHeader>
        <CardContent className="text-sm text-muted-foreground">
          Ödeme kayıtları, vadesi geçen abonelikler ve tahsilat onayı bu ekranda yönetilecek.
          Şimdilik abonelik durumunu müşteri panelinden (sağ sheet) &quot;Ödeme alındı&quot;
          işaretleyerek yönetebilirsin.
        </CardContent>
      </Card>
    </div>
  )
}
