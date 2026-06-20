import { AlertCircle, RefreshCw } from 'lucide-react'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { ApiError } from '@/lib/api'
import { cn } from '@/lib/utils'

interface ApiErrorCardProps {
  title?: string
  error: unknown
  onRetry?: () => void
  isRetrying?: boolean
  className?: string
}

function resolveErrorMessage(error: unknown): string {
  if (error instanceof ApiError) return error.message
  if (error instanceof Error) return error.message
  return 'Beklenmeyen bir hata oluştu.'
}

export function ApiErrorCard({
  title = 'Sunucuyla bağlantı kurulamadı',
  error,
  onRetry,
  isRetrying = false,
  className,
}: ApiErrorCardProps) {
  const message = resolveErrorMessage(error)
  const isNetworkError = error instanceof ApiError && error.status === 0

  return (
    <Alert variant="destructive" className={cn('border-destructive/30 bg-destructive/5', className)}>
      <AlertCircle className="h-4 w-4" />
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="space-y-1">
          <AlertTitle>{isNetworkError ? 'Bağlantı kesildi' : title}</AlertTitle>
          <AlertDescription>{message}</AlertDescription>
        </div>
        {onRetry && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="shrink-0 border-destructive/30 bg-background/80 hover:bg-background"
            onClick={onRetry}
            disabled={isRetrying}
          >
            <RefreshCw className={cn('h-4 w-4', isRetrying && 'animate-spin')} />
            Tekrar dene
          </Button>
        )}
      </div>
    </Alert>
  )
}
