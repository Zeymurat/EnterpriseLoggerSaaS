import { GitBranch, X } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface LogsTraceBannerProps {
  correlationId: string
  onClear: () => void
}

export function LogsTraceBanner({ correlationId, onClear }: LogsTraceBannerProps) {
  return (
    <div className="flex flex-col gap-3 rounded-2xl border border-primary/20 bg-primary/[0.06] px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex min-w-0 items-start gap-3">
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
          <GitBranch className="h-5 w-5" />
        </div>
        <div className="min-w-0 space-y-1">
          <p className="text-sm font-medium text-foreground">İstek zinciri izleniyor</p>
          <p className="text-xs text-muted-foreground">
            İzleme kimliği{' '}
            <code className="rounded-md bg-background/80 px-1.5 py-0.5 font-mono text-[11px] text-foreground">
              {correlationId}
            </code>{' '}
            ile eşleşen tüm loglar listeleniyor (tüm zamanlar).
          </p>
        </div>
      </div>
      <Button type="button" variant="outline" size="sm" className="shrink-0" onClick={onClear}>
        <X className="h-4 w-4" />
        İzlemeyi kapat
      </Button>
    </div>
  )
}
