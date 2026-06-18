import { Copy } from 'lucide-react'
import type { TenantRegistration } from '@/lib/api'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { toast } from '@/components/ui/sonner'

interface ApiKeySuccessDialogProps {
  registration: TenantRegistration | null
  onClose: () => void
  onContinue: () => void
}

export function ApiKeySuccessDialog({
  registration,
  onClose,
  onContinue,
}: ApiKeySuccessDialogProps) {
  const copyApiKey = async () => {
    if (!registration) return
    await navigator.clipboard.writeText(registration.apiKey)
    toast.success('API anahtarı kopyalandı')
  }

  return (
    <Dialog
      open={registration !== null}
      onClose={onClose}
      title="Şirket kaydı tamamlandı"
      description={`${registration?.name ?? ''} tenant'ı oluşturuldu. API anahtarını güvenli bir yere kaydedin — yalnızca bir kez gösterilir.`}
    >
      {registration && (
        <div className="space-y-4">
          <div className="rounded-xl border bg-muted/30 p-4">
            <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
              API Anahtarı
            </p>
            <p className="mt-2 break-all font-mono text-sm">{registration.apiKey}</p>
          </div>

          <div className="flex flex-col gap-2 sm:flex-row sm:justify-end">
            <Button type="button" variant="outline" onClick={copyApiKey}>
              <Copy className="h-4 w-4" />
              Kopyala
            </Button>
            <Button type="button" onClick={onContinue}>
              Giriş sayfasına git
            </Button>
          </div>
        </div>
      )}
    </Dialog>
  )
}
