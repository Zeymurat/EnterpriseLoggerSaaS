import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { auditActionLabel, getPlatformAuditLogs } from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function AuditLogsPage() {
  const [page, setPage] = useState(1)
  const [actionFilter, setActionFilter] = useState('')
  const [appliedAction, setAppliedAction] = useState('')

  const { data, isLoading, isError } = useQuery({
    queryKey: ['platform-audit-logs', page, appliedAction],
    queryFn: () =>
      getPlatformAuditLogs({
        page,
        pageSize: 25,
        action: appliedAction || undefined,
      }),
  })

  const items = data?.items ?? []
  const totalPages = Math.max(1, Math.ceil((data?.totalCount ?? 0) / (data?.pageSize ?? 25)))

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Denetim kayıtları</h1>
        <p className="text-sm text-muted-foreground">Platform admin işlem geçmişi.</p>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        <Input
          className="h-9 max-w-sm"
          placeholder="Aksiyon filtrele (ör. payment.confirmed)"
          value={actionFilter}
          onChange={(event) => setActionFilter(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === 'Enter') {
              setAppliedAction(actionFilter)
              setPage(1)
            }
          }}
        />
        <Button
          type="button"
          size="sm"
          className="h-9"
          onClick={() => {
            setAppliedAction(actionFilter)
            setPage(1)
          }}
        >
          Uygula
        </Button>
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Kayıtlar yükleniyor…</p>
      ) : isError ? (
        <p className="text-sm text-destructive">Kayıtlar alınamadı.</p>
      ) : items.length === 0 ? (
        <p className="rounded-lg border border-dashed p-8 text-center text-sm text-muted-foreground">
          Kayıt bulunamadı.
        </p>
      ) : (
        <div className="grid gap-3">
          {items.map((item) => (
            <Card key={item.id}>
              <CardHeader className="pb-2">
                <div className="flex flex-col gap-1 sm:flex-row sm:items-center sm:justify-between">
                  <CardTitle className="text-sm font-medium">
                    {auditActionLabel(item.action)}
                  </CardTitle>
                  <span className="text-xs text-muted-foreground">{formatDate(item.createdAt)}</span>
                </div>
              </CardHeader>
              <CardContent className="space-y-1 text-sm text-muted-foreground">
                <p>Aktör: {item.actorEmail}</p>
                {item.tenantName ? <p>Müşteri: {item.tenantName}</p> : null}
                {item.details ? <p>Detay: {item.details}</p> : null}
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      {!isLoading && !isError && (data?.totalCount ?? 0) > 0 ? (
        <div className="flex items-center justify-end gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage((current) => Math.max(1, current - 1))}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <span className="text-sm text-muted-foreground">
            {page} / {totalPages}
          </span>
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
        </div>
      ) : null}
    </div>
  )
}
