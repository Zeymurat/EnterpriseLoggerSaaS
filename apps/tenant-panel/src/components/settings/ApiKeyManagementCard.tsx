import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { KeyRound, RefreshCw } from 'lucide-react'
import { rotateTenantApiKey } from '@/lib/api'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { ApiKeyRevealDialog } from '@/components/settings/ApiKeyRevealDialog'
import { toast } from '@/components/ui/sonner'

export function ApiKeyManagementCard() {
  const { can } = useAuth()
  const [revealedKey, setRevealedKey] = useState<string | null>(null)

  const rotateMutation = useMutation({
    mutationFn: rotateTenantApiKey,
    onSuccess: (result) => {
      setRevealedKey(result.apiKey)
      toast.success('Yeni API anahtarı üretildi')
    },
    onError: (err: Error) => toast.error('API anahtarı üretilemedi', { description: err.message }),
  })

  if (!can('apikeys:rotate')) return null

  return (
    <>
      <Card id="api-key-management">
        <CardHeader className="pb-3">
          <div className="flex items-center gap-2">
            <KeyRound className="h-5 w-5 text-primary" />
            <CardTitle className="text-lg">API anahtarı</CardTitle>
          </div>
          <CardDescription>
            Uygulamalarınız log göndermek için bu anahtarı kullanır. Üretim sonrası anahtar yalnızca
            bir kez gösterilir; kaybederseniz yenileyin.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Button
            onClick={() => rotateMutation.mutate()}
            disabled={rotateMutation.isPending}
          >
            <RefreshCw className={rotateMutation.isPending ? 'h-4 w-4 animate-spin' : 'h-4 w-4'} />
            {rotateMutation.isPending ? 'Üretiliyor...' : 'API anahtarı üret / yenile'}
          </Button>
        </CardContent>
      </Card>

      <ApiKeyRevealDialog apiKey={revealedKey} onClose={() => setRevealedKey(null)} />
    </>
  )
}
