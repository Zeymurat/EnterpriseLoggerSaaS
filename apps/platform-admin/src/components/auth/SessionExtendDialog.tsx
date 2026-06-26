import { Clock3 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { formatSessionDuration } from '@/lib/session-config'

interface SessionExtendDialogProps {
  open: boolean
  remainingSeconds: number
  isExtending: boolean
  onExtend: () => void
  onLogout: () => void
}

export function SessionExtendDialog({
  open,
  remainingSeconds,
  isExtending,
  onExtend,
  onLogout,
}: SessionExtendDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onLogout}
      title="Oturumunuz sonlanıyor"
      description="Güvenlik nedeniyle kısa süreli hareketsizlik algılandı."
      className="max-w-md"
      footer={
        <div className="flex w-full flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <Button
            type="button"
            variant="outline"
            onMouseDown={(event) => event.stopPropagation()}
            onClick={onLogout}
            disabled={isExtending}
          >
            Çıkış yap
          </Button>
          <Button
            type="button"
            onMouseDown={(event) => event.stopPropagation()}
            onClick={onExtend}
            disabled={isExtending}
          >
            {isExtending ? 'Uzatılıyor…' : 'Oturumu uzat'}
          </Button>
        </div>
      }
    >
      <div className="flex items-start gap-3 rounded-xl border border-primary/20 bg-primary/[0.06] p-4">
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
          <Clock3 className="h-5 w-5" />
        </div>
        <div className="space-y-1 text-sm">
          <p className="font-medium text-foreground">
            Kalan süre: {formatSessionDuration(Math.max(remainingSeconds, 0))}
          </p>
          <p className="leading-relaxed text-muted-foreground">
            Platform panelinde aktif kalmak için oturumu uzatabilirsiniz. Sayfa geçişleri ve API
            istekleri aktivite sayılır.
          </p>
        </div>
      </div>
    </Dialog>
  )
}
