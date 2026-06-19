import { Copy } from 'lucide-react'
import type { ReactNode } from 'react'
import type { LogEntry } from '@/types/log-entry'
import { LogLevelBadge } from '@/components/logs/LogLevelBadge'
import { Button } from '@/components/ui/button'
import { Sheet } from '@/components/ui/sheet'
import { toast } from '@/components/ui/sonner'

function formatTimestamp(iso: string): string {
  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'medium',
    timeStyle: 'medium',
  }).format(new Date(iso))
}

function hasValue(value: string | number | null | undefined): boolean {
  return value !== null && value !== undefined && value !== ''
}

interface LogDetailSheetProps {
  log: LogEntry | null
  onClose: () => void
}

function DetailField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="space-y-1.5">
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-sm">{children}</dd>
    </div>
  )
}

export function LogDetailSheet({ log, onClose }: LogDetailSheetProps) {
  const copyMessage = async () => {
    if (!log) return
    await navigator.clipboard.writeText(log.message)
    toast.success('Mesaj kopyalandı')
  }

  return (
    <Sheet
      open={log !== null}
      onClose={onClose}
      title="Log detayı"
      description={log ? `${log.applicationName} · ${formatTimestamp(log.timestamp)}` : undefined}
      side="right"
      className="max-w-lg"
      footer={
        log ? (
          <Button type="button" variant="outline" size="sm" onClick={copyMessage}>
            <Copy className="h-4 w-4" />
            Mesajı kopyala
          </Button>
        ) : undefined
      }
    >
      {log && (
        <dl className="space-y-5">
          <DetailField label="Seviye">
            <LogLevelBadge level={log.logLevel} />
          </DetailField>

          <DetailField label="Uygulama">
            <span className="font-medium">{log.applicationName}</span>
          </DetailField>

          {hasValue(log.httpMethod) && (
            <DetailField label="İstek tipi">
              <span className="font-mono text-xs font-medium">{log.httpMethod}</span>
            </DetailField>
          )}

          {hasValue(log.requestPath) && (
            <DetailField label="İstek URL">
              <span className="font-mono text-xs break-all">{log.requestPath}</span>
            </DetailField>
          )}

          {hasValue(log.statusCode) && (
            <DetailField label="İstek sonucu">
              <span className="font-mono text-xs">{log.statusCode}</span>
            </DetailField>
          )}

          {hasValue(log.actorIdentifier) && (
            <DetailField label="Tetikleyen">
              <span>{log.actorIdentifier}</span>
            </DetailField>
          )}

          {hasValue(log.correlationId) && (
            <DetailField label="İzleme kimliği">
              <span className="font-mono text-xs">{log.correlationId}</span>
            </DetailField>
          )}

          {hasValue(log.exceptionType) && (
            <DetailField label="Hata tipi">
              <span className="font-mono text-xs text-destructive">{log.exceptionType}</span>
            </DetailField>
          )}

          <DetailField label="Mesaj">
            <p className="whitespace-pre-wrap break-words rounded-xl border bg-muted/20 p-3 font-mono text-xs leading-relaxed">
              {log.message}
            </p>
          </DetailField>
        </dl>
      )}
    </Sheet>
  )
}
