import { useEffect, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

interface DialogProps {
  open: boolean
  onClose: () => void
  title: string
  description?: string
  children: ReactNode
  className?: string
  footer?: ReactNode
}

export function Dialog({
  open,
  onClose,
  title,
  description,
  children,
  className,
  footer,
}: DialogProps) {
  useEffect(() => {
    if (!open) return

    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose()
    }
    window.addEventListener('keydown', onKeyDown)

    return () => {
      document.body.style.overflow = previousOverflow
      window.removeEventListener('keydown', onKeyDown)
    }
  }, [open, onClose])

  if (!open) return null

  return createPortal(
    <div
      className="fixed inset-0 z-[100] flex min-h-[100dvh] w-full items-center justify-center bg-black/60 p-4 backdrop-blur-sm animate-in fade-in-0"
      onClick={onClose}
      role="presentation"
    >
      <div
        className={cn(
          'max-h-[90vh] w-full max-w-lg overflow-hidden rounded-2xl border bg-background shadow-2xl animate-in zoom-in-95',
          className,
        )}
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
        aria-labelledby="dialog-title"
      >
        <div className="flex items-start justify-between gap-4 border-b bg-muted/30 px-6 py-5">
          <div className="min-w-0">
            <h2 id="dialog-title" className="text-lg font-semibold tracking-tight">
              {title}
            </h2>
            {description && (
              <p className="mt-1.5 text-sm text-muted-foreground">{description}</p>
            )}
          </div>
          <Button
            variant="ghost"
            size="sm"
            className="shrink-0 rounded-full"
            onClick={onClose}
            aria-label="Kapat"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>
        <div className="overflow-y-auto px-6 py-5">{children}</div>
        {footer && <div className="border-t bg-muted/20 px-6 py-4">{footer}</div>}
      </div>
    </div>,
    document.body,
  )
}

interface ConfirmDialogProps {
  open: boolean
  onClose: () => void
  onConfirm: () => void
  title: string
  description: string
  confirmLabel?: string
  cancelLabel?: string
  variant?: 'default' | 'destructive'
  isLoading?: boolean
}

export function ConfirmDialog({
  open,
  onClose,
  onConfirm,
  title,
  description,
  confirmLabel = 'Onayla',
  cancelLabel = 'İptal',
  variant = 'default',
  isLoading = false,
}: ConfirmDialogProps) {
  return (
    <Dialog open={open} onClose={onClose} title={title} description={description}>
      <div className="flex justify-end gap-2">
        <Button variant="outline" onClick={onClose} disabled={isLoading}>
          {cancelLabel}
        </Button>
        <Button
          variant={variant === 'destructive' ? 'destructive' : 'default'}
          onClick={onConfirm}
          disabled={isLoading}
        >
          {isLoading ? 'İşleniyor...' : confirmLabel}
        </Button>
      </div>
    </Dialog>
  )
}

// Backward-compatible alias
export { Dialog as SimpleDialog }
