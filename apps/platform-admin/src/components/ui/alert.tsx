import { cva, type VariantProps } from 'class-variance-authority'
import { cn } from '@/lib/utils'

const alertVariants = cva('relative flex gap-3 rounded-xl border px-4 py-3 text-sm', {
  variants: {
    variant: {
      default: 'border-border bg-card text-foreground',
      info: 'border-status-info/30 bg-status-info-muted/80 text-foreground',
      warning: 'border-status-warning/30 bg-status-warning-muted/80 text-foreground',
      destructive: 'border-destructive/30 bg-destructive/5 text-destructive',
      success: 'border-status-success/30 bg-status-success-muted/80 text-foreground',
    },
  },
  defaultVariants: {
    variant: 'default',
  },
})

export interface AlertProps
  extends React.HTMLAttributes<HTMLDivElement>,
    VariantProps<typeof alertVariants> {}

export function Alert({ className, variant, ...props }: AlertProps) {
  return <div role="alert" className={cn(alertVariants({ variant }), className)} {...props} />
}

export function AlertTitle({ className, ...props }: React.HTMLAttributes<HTMLParagraphElement>) {
  return <p className={cn('font-medium leading-none tracking-tight', className)} {...props} />
}

export function AlertDescription({
  className,
  ...props
}: React.HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('text-sm opacity-90 [&_p]:leading-relaxed', className)} {...props} />
}
