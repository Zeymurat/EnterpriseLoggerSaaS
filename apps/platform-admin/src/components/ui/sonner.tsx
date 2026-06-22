import { Toaster as Sonner, toast } from 'sonner'

export function Toaster() {
  return (
    <Sonner
      position="top-right"
      closeButton
      expand
      gap={10}
      toastOptions={{
        duration: 4000,
        classNames: {
          toast:
            'group rounded-xl border border-border bg-card text-foreground shadow-lg font-sans',
          title: 'text-sm font-semibold',
          description: 'text-sm text-muted-foreground',
          success: 'border-status-success/30 bg-status-success-muted text-foreground',
          error: 'border-status-error/30 bg-status-error-muted text-foreground',
          warning: 'border-status-warning/30 bg-status-warning-muted text-foreground',
          info: 'border-status-info/30 bg-status-info-muted text-foreground',
          closeButton: 'border-border bg-background hover:bg-muted',
        },
      }}
    />
  )
}

export { toast }
