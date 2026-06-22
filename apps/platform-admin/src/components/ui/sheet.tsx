import { useEffect, type ReactNode } from 'react'
import { createPortal } from 'react-dom'
import { X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

export type SheetSide = 'right' | 'left'

interface SheetProps {
  open: boolean
  onClose: () => void
  title: string
  description?: string
  children: ReactNode
  side?: SheetSide
  className?: string
  footer?: ReactNode
}

const sidePanelClasses: Record<SheetSide, string> = {
  right: 'inset-y-0 right-0 border-l animate-in slide-in-from-right duration-300',
  left: 'inset-y-0 left-0 border-r animate-in slide-in-from-left duration-300',
}

export function Sheet({
  open,
  onClose,
  title,
  description,
  children,
  side = 'right',
  className,
  footer,
}: SheetProps) {
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
    <div className="fixed inset-0 z-[100]" role="presentation">
      <div
        className="absolute inset-0 bg-black/50 backdrop-blur-sm motion-safe:animate-in motion-safe:fade-in-0 motion-safe:duration-300"
        onClick={onClose}
        aria-hidden="true"
      />
      <div
        className={cn(
          'absolute flex h-full w-full max-w-md flex-col bg-background shadow-2xl motion-safe:animate-in',
          sidePanelClasses[side],
          className,
        )}
        role="dialog"
        aria-modal="true"
        aria-labelledby="sheet-title"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-start justify-between gap-4 border-b bg-muted/30 px-6 py-5">
          <div className="min-w-0">
            <h2 id="sheet-title" className="text-lg font-semibold tracking-tight">
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
        <div className="flex-1 overflow-y-auto px-6 py-5">{children}</div>
        {footer && <div className="border-t bg-muted/20 px-6 py-4">{footer}</div>}
      </div>
    </div>,
    document.body,
  )
}
