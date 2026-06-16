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
          success: 'border-emerald-200 bg-emerald-50 text-emerald-950',
          error: 'border-red-200 bg-red-50 text-red-950',
          warning: 'border-amber-200 bg-amber-50 text-amber-950',
          info: 'border-sky-200 bg-sky-50 text-sky-950',
          closeButton: 'border-border bg-background hover:bg-muted',
        },
      }}
    />
  )
}

export { toast }
