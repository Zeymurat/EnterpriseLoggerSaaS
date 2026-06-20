import { useEffect, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { ChevronDown } from 'lucide-react'
import { Checkbox } from '@/components/ui/checkbox'
import { cn } from '@/lib/utils'

export interface MultiSelectOption {
  value: string
  label: string
}

interface MultiSelectProps {
  options: MultiSelectOption[]
  values: string[]
  onChange: (values: string[]) => void
  placeholder?: string
  emptyLabel?: string
  className?: string
  disabled?: boolean
}

export function MultiSelect({
  options,
  values,
  onChange,
  placeholder = 'Tümü',
  emptyLabel = 'Kayıt yok',
  className,
  disabled,
}: MultiSelectProps) {
  const [open, setOpen] = useState(false)
  const [menuStyle, setMenuStyle] = useState<{ top: number; left: number; width: number } | null>(
    null,
  )
  const containerRef = useRef<HTMLDivElement>(null)
  const menuRef = useRef<HTMLDivElement>(null)

  const updateMenuPosition = () => {
    if (!containerRef.current) return
    const rect = containerRef.current.getBoundingClientRect()
    setMenuStyle({
      top: rect.bottom + 6,
      left: rect.left,
      width: rect.width,
    })
  }

  useEffect(() => {
    if (!open) return

    updateMenuPosition()

    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as Node
      if (
        !containerRef.current?.contains(target) &&
        !menuRef.current?.contains(target)
      ) {
        setOpen(false)
      }
    }

    const handleReposition = () => updateMenuPosition()

    document.addEventListener('mousedown', handleClickOutside)
    window.addEventListener('resize', handleReposition)
    window.addEventListener('scroll', handleReposition, true)

    return () => {
      document.removeEventListener('mousedown', handleClickOutside)
      window.removeEventListener('resize', handleReposition)
      window.removeEventListener('scroll', handleReposition, true)
    }
  }, [open])

  const toggleValue = (value: string) => {
    onChange(
      values.includes(value) ? values.filter((item) => item !== value) : [...values, value],
    )
  }

  const displayLabel = (() => {
    if (values.length === 0) return placeholder
    if (values.length === 1) {
      return options.find((option) => option.value === values[0])?.label ?? values[0]
    }
    return `${values.length} seçili`
  })()

  const menu = open && menuStyle
    ? createPortal(
        <div
          ref={menuRef}
          style={{
            position: 'fixed',
            top: menuStyle.top,
            left: menuStyle.left,
            width: menuStyle.width,
            zIndex: 80,
          }}
          className="max-h-56 overflow-y-auto rounded-xl border border-border/70 bg-popover p-1.5 text-popover-foreground shadow-warm-lg"
        >
          {options.length === 0 ? (
            <p className="px-3 py-2 text-sm text-muted-foreground">{emptyLabel}</p>
          ) : (
            options.map((option) => (
              <Checkbox
                key={option.value}
                id={`multi-${option.value}`}
                checked={values.includes(option.value)}
                onChange={() => toggleValue(option.value)}
                label={option.label}
              />
            ))
          )}
        </div>,
        document.body,
      )
    : null

  return (
    <>
      <div ref={containerRef} className={cn('relative', className)}>
        <button
          type="button"
          disabled={disabled}
          onClick={() => setOpen((current) => !current)}
          className={cn(
            'flex h-11 w-full items-center justify-between gap-2 rounded-xl border border-input/80 bg-card px-3.5 text-sm shadow-sm transition-all',
            'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
            'disabled:cursor-not-allowed disabled:opacity-50',
            values.length > 0 ? 'text-foreground' : 'text-muted-foreground',
            open && 'ring-2 ring-ring ring-offset-2',
          )}
        >
          <span className="truncate text-left">{displayLabel}</span>
          <ChevronDown
            className={cn(
              'h-4 w-4 shrink-0 text-muted-foreground transition-transform',
              open && 'rotate-180',
            )}
          />
        </button>
      </div>
      {menu}
    </>
  )
}
