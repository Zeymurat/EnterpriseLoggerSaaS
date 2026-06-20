import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Dialog } from '@/components/ui/dialog'
import { cn } from '@/lib/utils'
import type { LogExportScope, QuickDatePreset } from '@/lib/log-date-filters'

export type { LogExportScope } from '@/lib/log-date-filters'

interface LogExportScopeDialogProps {
  open: boolean
  onClose: () => void
  onConfirm: (scope: LogExportScope) => void
  screenLabel: string
  screenCount?: number
  allCount?: number
  isLoading?: boolean
}

function ScopeOption({
  id,
  name,
  value,
  checked,
  onChange,
  title,
  description,
  count,
}: {
  id: string
  name: string
  value: LogExportScope
  checked: boolean
  onChange: (value: LogExportScope) => void
  title: string
  description: string
  count?: number
}) {
  return (
    <label
      htmlFor={id}
      className={cn(
        'flex cursor-pointer gap-3 rounded-xl border p-4 transition-colors',
        checked ? 'border-primary bg-primary/5' : 'border-border hover:bg-muted/40',
      )}
    >
      <input
        id={id}
        type="radio"
        name={name}
        value={value}
        checked={checked}
        onChange={() => onChange(value)}
        className="mt-1 h-4 w-4 accent-primary"
      />
      <span className="min-w-0 flex-1">
        <span className="block text-sm font-medium">{title}</span>
        <span className="mt-1 block text-sm text-muted-foreground">{description}</span>
        {count !== undefined && (
          <span className="mt-2 block text-xs font-medium text-foreground">
            {count} kayıt
          </span>
        )}
      </span>
    </label>
  )
}

export function getQuickDateLabel(preset: QuickDatePreset): string {
  switch (preset) {
    case 'all':
      return 'Tüm zamanlar'
    case '10m':
      return 'Son 10 dakika'
    case '30m':
      return 'Son 30 dakika'
    case '1h':
      return 'Son 1 saat'
    case '3h':
      return 'Son 3 saat'
    case '12h':
      return 'Son 12 saat'
    case 'today':
      return 'Bugün'
    case '7d':
      return 'Son 7 gün'
    case '30d':
      return 'Son 30 gün'
    case 'month':
      return 'Bu ay'
    case 'year':
      return 'Bu yıl'
    case '':
      return 'Özel aralık'
    default:
      return 'Seçili aralık'
  }
}

export function LogExportScopeDialog({
  open,
  onClose,
  onConfirm,
  screenLabel,
  screenCount,
  allCount,
  isLoading = false,
}: LogExportScopeDialogProps) {
  const [scope, setScope] = useState<LogExportScope>('screen')

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="CSV dışa aktar"
      description="Henüz ek filtre uygulamadınız. Hangi kayıtları indirmek istiyorsunuz?"
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose} disabled={isLoading}>
            İptal
          </Button>
          <Button onClick={() => onConfirm(scope)} disabled={isLoading}>
            {isLoading ? 'İndiriliyor...' : 'İndir'}
          </Button>
        </div>
      }
    >
      <div className="space-y-3">
        <ScopeOption
          id="export-scope-screen"
          name="export-scope"
          value="screen"
          checked={scope === 'screen'}
          onChange={setScope}
          title="Görünümdeki veri"
          description={`Şu an ekranda gördüğünüz zaman aralığı: ${screenLabel}`}
          count={screenCount}
        />
        <ScopeOption
          id="export-scope-all"
          name="export-scope"
          value="all"
          checked={scope === 'all'}
          onChange={setScope}
          title="Tüm zamanlar"
          description="Tenant'a ait tüm log kayıtları (en fazla 10.000 satır)"
          count={allCount}
        />
      </div>
    </Dialog>
  )
}
