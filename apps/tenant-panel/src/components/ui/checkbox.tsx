import { cn } from '@/lib/utils'

interface CheckboxProps extends Omit<React.InputHTMLAttributes<HTMLInputElement>, 'type'> {
  label?: React.ReactNode
}

export function Checkbox({ className, label, id, ...props }: CheckboxProps) {
  const input = (
    <input
      id={id}
      type="checkbox"
      className={cn(
        'mt-0.5 h-4 w-4 shrink-0 rounded border border-input accent-primary shadow-sm transition-colors',
        'focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2',
        className,
      )}
      {...props}
    />
  )

  if (!label) return input

  return (
    <label
      htmlFor={id}
      className={cn(
        'flex cursor-pointer items-start gap-3 rounded-lg p-2 transition-colors hover:bg-muted/50',
        props.disabled && 'cursor-not-allowed opacity-50',
      )}
    >
      {input}
      <span className="flex-1 text-sm leading-snug">{label}</span>
    </label>
  )
}
