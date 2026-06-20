import type { ReactNode } from 'react'
import { Search, X } from 'lucide-react'
import { MultiSelect } from '@/components/ui/multi-select'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { LogsTimeRangeSelect } from '@/components/logs/LogsTimeRangeSelect'
import type { LogFilterOptions } from '@/types/log-entry'
import { SUPPORTED_LOG_LEVELS } from '@/lib/log-levels'
import {
  isRelativeTimePreset,
  type LogsFilterState,
  type QuickDatePreset,
} from '@/lib/log-date-filters'
import { cn } from '@/lib/utils'

export type { LogsFilterState, QuickDatePreset } from '@/lib/log-date-filters'
export {
  createDefaultLogsFilters,
  formatLocalDate,
  quickDatePresetToRange,
} from '@/lib/log-date-filters'

interface LogsFilterBarProps {
  filters: LogsFilterState
  filterOptions?: LogFilterOptions
  onSearchInputChange: (value: string) => void
  onLogLevelsChange: (values: string[]) => void
  onHttpMethodsChange: (values: string[]) => void
  onStatusCodesChange: (values: string[]) => void
  onApplicationNamesChange: (values: string[]) => void
  onQuickDateChange: (preset: QuickDatePreset) => void
  onFromDateChange: (value: string) => void
  onToDateChange: (value: string) => void
  onClear: () => void
  hasActiveFilters: boolean
}

function FilterField({
  label,
  children,
  className,
}: {
  label: string
  children: ReactNode
  className?: string
}) {
  return (
    <div className={cn('min-w-0 space-y-2', className)}>
      <Label className="text-[11px] font-semibold uppercase tracking-[0.14em] text-muted-foreground">
        {label}
      </Label>
      {children}
    </div>
  )
}

function DateInput({
  value,
  onChange,
  placeholder,
  disabled,
}: {
  value: string
  onChange: (value: string) => void
  placeholder: string
  disabled?: boolean
}) {
  return (
    <Input
      type="date"
      value={value}
      placeholder={placeholder}
      disabled={disabled}
      onChange={(e) => onChange(e.target.value)}
      className={cn(
        'date-input',
        value ? 'date-input-filled' : 'date-input-empty',
        disabled && 'cursor-not-allowed opacity-60',
      )}
    />
  )
}

export function LogsFilterBar({
  filters,
  filterOptions,
  onSearchInputChange,
  onLogLevelsChange,
  onHttpMethodsChange,
  onStatusCodesChange,
  onApplicationNamesChange,
  onQuickDateChange,
  onFromDateChange,
  onToDateChange,
  onClear,
  hasActiveFilters,
}: LogsFilterBarProps) {
  const applicationOptions =
    filterOptions?.applicationNames.map((name) => ({ value: name, label: name })) ?? []

  const httpMethodOptions =
    filterOptions?.httpMethods.map((method) => ({ value: method, label: method })) ?? []

  const statusCodeOptions =
    filterOptions?.statusCodes.map((code) => ({
      value: String(code),
      label: String(code),
    })) ?? []

  const dateInputsDisabled =
    filters.quickDate === 'all' || isRelativeTimePreset(filters.quickDate)

  return (
    <div className="glass-surface space-y-5 p-5">
      <div className="flex items-center justify-between gap-3">
        <div>
          <p className="text-[11px] font-semibold uppercase tracking-[0.16em] text-muted-foreground">
            Filtreler
          </p>
          <p className="mt-1 text-sm text-muted-foreground">Log listesini daraltın</p>
        </div>
        {hasActiveFilters && (
          <Button type="button" variant="ghost" size="sm" className="rounded-xl" onClick={onClear}>
            <X className="h-4 w-4" />
            Temizle
          </Button>
        )}
      </div>

      <FilterField label="Mesaj ara">
        <div className="relative">
          <Search className="absolute left-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            className="pl-10"
            placeholder="Log mesajında ara..."
            value={filters.searchInput}
            onChange={(e) => onSearchInputChange(e.target.value)}
          />
        </div>
      </FilterField>

      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <FilterField label="Log tipi">
          <MultiSelect
            placeholder="Tümü"
            options={SUPPORTED_LOG_LEVELS.map((level) => ({
              value: level.key,
              label: level.label,
            }))}
            values={filters.logLevels}
            onChange={onLogLevelsChange}
          />
        </FilterField>

        <FilterField label="İstek tipi">
          <MultiSelect
            placeholder="Tümü"
            emptyLabel="İstek tipi yok"
            options={httpMethodOptions}
            values={filters.httpMethods}
            onChange={onHttpMethodsChange}
          />
        </FilterField>

        <FilterField label="İstek sonucu">
          <MultiSelect
            placeholder="Tümü"
            emptyLabel="Sonuç kodu yok"
            options={statusCodeOptions}
            values={filters.statusCodes}
            onChange={onStatusCodesChange}
          />
        </FilterField>

        <FilterField label="Uygulama">
          <MultiSelect
            placeholder="Tümü"
            emptyLabel="Uygulama yok"
            options={applicationOptions}
            values={filters.applicationNames}
            onChange={onApplicationNamesChange}
          />
        </FilterField>
      </div>

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <FilterField label="Zaman aralığı">
          <LogsTimeRangeSelect value={filters.quickDate} onChange={onQuickDateChange} />
        </FilterField>

        <FilterField label="Başlangıç">
          <DateInput
            value={filters.fromDate}
            placeholder="Başlangıç"
            disabled={dateInputsDisabled}
            onChange={onFromDateChange}
          />
        </FilterField>

        <FilterField label="Bitiş">
          <DateInput
            value={filters.toDate}
            placeholder="Bitiş"
            disabled={dateInputsDisabled}
            onChange={onToDateChange}
          />
        </FilterField>
      </div>
    </div>
  )
}
