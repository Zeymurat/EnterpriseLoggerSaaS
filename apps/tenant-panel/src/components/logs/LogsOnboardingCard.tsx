import { BookOpen, ScrollText } from 'lucide-react'
import { getApiUrl } from '@/lib/api'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const INTEGRATION_DOCS_URL =
  'https://github.com/Zeymurat/EnterpriseLoggerSaaS#log-ingestion--query-dual-channel-auth'

interface LogsOnboardingCardProps {
  className?: string
}

export function LogsOnboardingCard({ className }: LogsOnboardingCardProps) {
  const apiUrl = getApiUrl()

  return (
    <div className={cn('warm-card p-8 sm:p-10', className)}>
      <div className="absolute inset-x-0 top-0 h-1 bg-primary opacity-90" />
      <div className="mx-auto max-w-2xl text-center">
        <div className="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-2xl bg-primary/10 text-primary">
          <ScrollText className="h-7 w-7" />
        </div>
        <h2 className="text-xl font-semibold tracking-tight text-foreground sm:text-2xl">
          Henüz analitik veri bulunmuyor
        </h2>
        <p className="mt-3 text-sm leading-relaxed text-muted-foreground">
          Sisteminizi entegre etmek için tenant API anahtarınızı kullanın. Uygulamanız{' '}
          <code className="rounded-md bg-muted px-1.5 py-0.5 text-xs">POST /api/logs</code> ile log
          gönderdiğinde özet kartları ve listeler burada görünür.
        </p>

        <div className="mt-6 rounded-xl border border-border/60 bg-muted/30 px-4 py-3 text-left text-xs text-muted-foreground">
          <p className="font-medium text-foreground">Örnek istek</p>
          <pre className="mt-2 overflow-x-auto whitespace-pre-wrap break-all font-mono text-[11px] leading-relaxed">
            {`POST ${apiUrl}/api/logs
X-Api-Key: <tenant-api-anahtariniz>

{
  "applicationName": "BillingService",
  "logLevel": "Info",
  "message": "İlk log kaydı"
}`}
          </pre>
        </div>

        <div className="mt-6 flex flex-col items-center justify-center gap-3 sm:flex-row">
          <Button variant="default" asChild>
            <a href="#api-key-management">
              API anahtarına git
            </a>
          </Button>
          <Button variant="outline" asChild>
            <a href={INTEGRATION_DOCS_URL} target="_blank" rel="noreferrer">
              <BookOpen className="h-4 w-4" />
              Entegrasyon dokümantasyonu
            </a>
          </Button>
        </div>
      </div>
    </div>
  )
}
