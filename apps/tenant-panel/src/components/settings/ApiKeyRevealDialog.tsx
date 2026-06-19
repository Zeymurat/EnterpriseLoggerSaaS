import { Copy, KeyRound } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { toast } from '@/components/ui/sonner'

interface ApiKeyRevealDialogProps {
  apiKey: string | null
  onClose: () => void
}

export function ApiKeyRevealDialog({ apiKey, onClose }: ApiKeyRevealDialogProps) {
  const copyApiKey = async () => {
    if (!apiKey) return
    await navigator.clipboard.writeText(apiKey)
    toast.success('API anahtarı kopyalandı')
  }

  return (
    <Dialog
      open={apiKey !== null}
      onClose={onClose}
      title="API anahtarınız"
      description="Bu anahtar yalnızca bir kez gösterilir. Güvenli bir yere kaydedin; tekrar görüntülenemez."
    >
      {apiKey && (
        <div className="space-y-4">
          <div className="rounded-xl border border-amber-500/30 bg-amber-500/5 p-4">
            <div className="mb-2 flex items-center gap-2 text-amber-700 dark:text-amber-400">
              <KeyRound className="h-4 w-4" />
              <p className="text-xs font-medium uppercase tracking-wide">Tek seferlik gösterim</p>
            </div>
            <p className="break-all font-mono text-sm">{apiKey}</p>
          </div>

          <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
            <Button type="button" variant="outline" onClick={copyApiKey}>
              <Copy className="h-4 w-4" />
              Kopyala
            </Button>
            <Button type="button" onClick={onClose}>
              Anladım, kapattım
            </Button>
          </div>
        </div>
      )}
    </Dialog>
  )
}
