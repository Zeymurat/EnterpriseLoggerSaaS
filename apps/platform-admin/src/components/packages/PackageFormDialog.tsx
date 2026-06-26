import { useEffect, useState } from 'react'
import {
  PACKAGE_LOG_LEVEL_OPTIONS,
  parsePackageLogLevels,
  serializePackageLogLevels,
  type PlatformPackage,
  type PlatformPackageFormData,
} from '@/lib/api'
import { Dialog } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { MultiSelect } from '@/components/ui/multi-select'
import { toast } from 'sonner'

interface PackageFormDialogProps {
  open: boolean
  mode: 'create' | 'edit'
  pkg: PlatformPackage | null
  isSaving: boolean
  onClose: () => void
  onSubmit: (data: PlatformPackageFormData) => void
}

const defaultForm: PlatformPackageFormData = {
  code: '',
  name: '',
  description: '',
  allowedLogLevels: 'INFO,WARNING,ERROR',
  isMailEnabled: false,
  isSmsEnabled: false,
  monthlyRequestLimit: 100_000,
  maxLogsPerMinute: 500,
  storageRetentionDays: 90,
  priceMonthly: 0,
  priceQuarterly: 0,
  priceSemiAnnual: 0,
  priceAnnual: 0,
  isAvailable: true,
  sortOrder: 10,
}

function packageToForm(pkg: PlatformPackage): PlatformPackageFormData {
  return {
    code: pkg.code,
    name: pkg.name,
    description: pkg.description,
    allowedLogLevels: serializePackageLogLevels(parsePackageLogLevels(pkg.allowedLogLevels)),
    isMailEnabled: pkg.isMailEnabled,
    isSmsEnabled: pkg.isSmsEnabled,
    monthlyRequestLimit: pkg.monthlyRequestLimit,
    maxLogsPerMinute: pkg.maxLogsPerMinute,
    storageRetentionDays: pkg.storageRetentionDays,
    priceMonthly: pkg.priceMonthly,
    priceQuarterly: pkg.priceQuarterly,
    priceSemiAnnual: pkg.priceSemiAnnual,
    priceAnnual: pkg.priceAnnual,
    isAvailable: pkg.isAvailable,
    sortOrder: pkg.sortOrder,
  }
}

export function PackageFormDialog({
  open,
  mode,
  pkg,
  isSaving,
  onClose,
  onSubmit,
}: PackageFormDialogProps) {
  const [form, setForm] = useState<PlatformPackageFormData>(defaultForm)
  const selectedLogLevels = parsePackageLogLevels(form.allowedLogLevels)

  useEffect(() => {
    if (!open) return
    setForm(mode === 'edit' && pkg ? packageToForm(pkg) : defaultForm)
  }, [open, mode, pkg])

  const setField = <K extends keyof PlatformPackageFormData>(
    key: K,
    value: PlatformPackageFormData[K],
  ) => {
    setForm((current) => ({ ...current, [key]: value }))
  }

  const handleSubmit = () => {
    if (selectedLogLevels.length === 0) {
      toast.error('En az bir log seviyesi seçin.')
      return
    }

    onSubmit({
      ...form,
      allowedLogLevels: serializePackageLogLevels(selectedLogLevels),
    })
  }

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title={mode === 'create' ? 'Yeni paket' : 'Paketi düzenle'}
      description={
        mode === 'create'
          ? 'Kod oluşturulduktan sonra değiştirilemez.'
          : pkg
            ? `${pkg.code} · ${pkg.isDefault ? 'Varsayılan paket' : 'Düzenlenebilir'}`
            : undefined
      }
      className="w-full max-w-2xl"
      footer={
        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={onClose} disabled={isSaving}>
            Vazgeç
          </Button>
          <Button onClick={handleSubmit} disabled={isSaving}>
            {isSaving ? 'Kaydediliyor…' : mode === 'create' ? 'Oluştur' : 'Kaydet'}
          </Button>
        </div>
      }
    >
      <div className="grid gap-4 sm:grid-cols-2">
        {mode === 'create' ? (
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="pkg-code">Kod</Label>
            <Input
              id="pkg-code"
              className="h-9"
              placeholder="ornek-paket"
              value={form.code}
              onChange={(event) => setField('code', event.target.value.toLowerCase())}
            />
          </div>
        ) : null}

        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="pkg-name">Ad</Label>
          <Input
            id="pkg-name"
            className="h-9"
            value={form.name}
            onChange={(event) => setField('name', event.target.value)}
          />
        </div>

        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="pkg-description">Açıklama</Label>
          <Input
            id="pkg-description"
            className="h-9"
            value={form.description}
            onChange={(event) => setField('description', event.target.value)}
          />
        </div>

        <div className="space-y-2 sm:col-span-2">
          <Label htmlFor="pkg-levels">İzin verilen log seviyeleri</Label>
          <MultiSelect
            options={[...PACKAGE_LOG_LEVEL_OPTIONS]}
            values={selectedLogLevels}
            onChange={(levels) => setField('allowedLogLevels', serializePackageLogLevels(levels))}
            placeholder="Seviye seçin"
            emptyLabel="Seviye yok"
            className="[&_button]:h-9"
          />
          <p className="text-xs text-muted-foreground">
            Örn. Free pakette Error kapalıysa tenant Error log gönderemez.
          </p>
        </div>

        <div className="space-y-2">
          <Label htmlFor="pkg-minute-limit">Dakikalık log kotası</Label>
          <Input
            id="pkg-minute-limit"
            className="h-9"
            type="number"
            min={1}
            value={form.maxLogsPerMinute}
            onChange={(event) => setField('maxLogsPerMinute', Number(event.target.value))}
          />
          <p className="text-xs text-muted-foreground">Dakikada kabul edilen maksimum log sayısı.</p>
        </div>

        <div className="space-y-2">
          <Label htmlFor="pkg-monthly-limit">Aylık log kotası</Label>
          <Input
            id="pkg-monthly-limit"
            className="h-9"
            type="number"
            min={1}
            value={form.monthlyRequestLimit}
            onChange={(event) => setField('monthlyRequestLimit', Number(event.target.value))}
          />
          <p className="text-xs text-muted-foreground">Takvim ayı boyunca toplam log limiti.</p>
        </div>

        <div className="space-y-2">
          <Label htmlFor="pkg-retention">Saklama (gün)</Label>
          <Input
            id="pkg-retention"
            className="h-9"
            type="number"
            min={1}
            value={form.storageRetentionDays}
            onChange={(event) => setField('storageRetentionDays', Number(event.target.value))}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="pkg-sort">Sıra</Label>
          <Input
            id="pkg-sort"
            className="h-9"
            type="number"
            min={0}
            value={form.sortOrder}
            onChange={(event) => setField('sortOrder', Number(event.target.value))}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="pkg-price-monthly">Aylık fiyat (TRY)</Label>
          <Input
            id="pkg-price-monthly"
            className="h-9"
            type="number"
            min={0}
            value={form.priceMonthly}
            onChange={(event) => setField('priceMonthly', Number(event.target.value))}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="pkg-price-quarterly">3 aylık fiyat</Label>
          <Input
            id="pkg-price-quarterly"
            className="h-9"
            type="number"
            min={0}
            value={form.priceQuarterly}
            onChange={(event) => setField('priceQuarterly', Number(event.target.value))}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="pkg-price-semi">6 aylık fiyat</Label>
          <Input
            id="pkg-price-semi"
            className="h-9"
            type="number"
            min={0}
            value={form.priceSemiAnnual}
            onChange={(event) => setField('priceSemiAnnual', Number(event.target.value))}
          />
        </div>

        <div className="space-y-2">
          <Label htmlFor="pkg-price-annual">Yıllık fiyat</Label>
          <Input
            id="pkg-price-annual"
            className="h-9"
            type="number"
            min={0}
            value={form.priceAnnual}
            onChange={(event) => setField('priceAnnual', Number(event.target.value))}
          />
        </div>

        <div className="flex flex-col gap-2 sm:col-span-2">
          <Checkbox
            id="pkg-mail"
            checked={form.isMailEnabled}
            onChange={(event) => setField('isMailEnabled', event.target.checked)}
            label="E-posta bildirimleri"
          />
          <Checkbox
            id="pkg-sms"
            checked={form.isSmsEnabled}
            onChange={(event) => setField('isSmsEnabled', event.target.checked)}
            label="SMS bildirimleri"
          />
          <Checkbox
            id="pkg-available"
            checked={form.isAvailable}
            onChange={(event) => setField('isAvailable', event.target.checked)}
            label="Satışta (yeni atamalara açık)"
          />
        </div>
      </div>
    </Dialog>
  )
}
