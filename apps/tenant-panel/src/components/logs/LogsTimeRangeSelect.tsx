import { Select } from '@/components/ui/select'
import type { QuickDatePreset } from '@/lib/log-date-filters'

interface LogsTimeRangeSelectProps {
  value: QuickDatePreset
  onChange: (preset: QuickDatePreset) => void
  className?: string
}

export function LogsTimeRangeSelect({ value, onChange, className }: LogsTimeRangeSelectProps) {
  return (
    <Select
      className={className}
      value={value}
      onChange={(e) => onChange(e.target.value as QuickDatePreset)}
    >
      <optgroup label="Canlı">
        <option value="10m">Son 10 dakika</option>
        <option value="30m">Son 30 dakika</option>
        <option value="1h">Son 1 saat</option>
        <option value="3h">Son 3 saat</option>
        <option value="12h">Son 12 saat</option>
      </optgroup>
      <optgroup label="Tarih">
        <option value="all">Tüm zamanlar</option>
        <option value="today">Bugün</option>
        <option value="7d">Son 7 gün</option>
        <option value="30d">Son 30 gün</option>
        <option value="month">Bu ay</option>
        <option value="year">Bu yıl</option>
        <option value="">Özel aralık</option>
      </optgroup>
    </Select>
  )
}
