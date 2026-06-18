import { Activity } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

export type AuthMode = 'login' | 'register'

interface BrandPanelProps {
  mode: AuthMode
  onSwitchMode: (mode: AuthMode) => void
  className?: string
}

/** AuthShell panel kayması ile aynı süre/easing */
const brandMotion =
  'duration-700 ease-in-out motion-reduce:transition-none motion-reduce:transform-none'

export const brandSlideClass = `transition-transform ${brandMotion}`
export const brandPanelPositionClass = `transition-[left] ${brandMotion}`

const copy = {
  login: {
    title: 'Loglarınızı ve ekibinizi tek yerden yönetin.',
    description:
      'Çoklu tenant desteği, rol tabanlı erişim ve gerçek zamanlı log izleme — hepsi bir arada.',
    cta: 'Henüz hesabınız yok mu?',
    action: 'Şirket kaydı oluştur',
    target: 'register' as const,
  },
  register: {
    title: 'Dakikalar içinde kendi log altyapınızı kurun.',
    description:
      'Yeni bir tenant açın, API anahtarınızı alın ve ekibinizi davet etmeye başlayın.',
    cta: 'Zaten hesabınız var mı?',
    action: 'Giriş yap',
    target: 'login' as const,
  },
} as const

function BrandColumn({
  side,
  onSwitchMode,
}: {
  side: AuthMode
  onSwitchMode: (mode: AuthMode) => void
}) {
  const content = copy[side]
  const isRegister = side === 'register'

  return (
    <div
      className={cn(
        'flex h-full w-1/2 shrink-0 flex-col justify-between',
        isRegister ? 'items-end text-right' : 'items-start text-left',
      )}
    >
      <div className={cn('flex items-center gap-3', isRegister && 'flex-row-reverse')}>
        <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-lg">
          <Activity className="h-6 w-6" />
        </div>
        <div>
          <p className="text-lg font-semibold">EnterpriseLogger</p>
          <p className="text-sm text-sidebar-muted">Tenant Yönetim Paneli</p>
        </div>
      </div>

      <div className="w-full max-w-4xl space-y-5">
        <h1 className="text-4xl font-bold leading-[1.15] tracking-tight xl:text-[2.75rem]">
          {content.title}
        </h1>
        <p className="text-base leading-relaxed text-sidebar-muted xl:text-lg">
          {content.description}
        </p>
      </div>

      <div className={cn('w-full max-w-xl space-y-4', isRegister && 'flex flex-col items-end')}>
        <p className="text-sm text-sidebar-muted">{content.cta}</p>
        <Button
          variant="outline"
          className="border-sidebar-muted/30 bg-transparent text-sidebar-foreground hover:bg-white/10 hover:text-sidebar-foreground"
          onClick={() => onSwitchMode(content.target)}
        >
          {content.action}
        </Button>
        <p className="text-xs text-sidebar-muted">© EnterpriseLogger SaaS</p>
      </div>
    </div>
  )
}

export function BrandPanel({ mode, onSwitchMode, className }: BrandPanelProps) {
  const isRegister = mode === 'register'

  return (
    <div
      className={cn(
        'relative h-full overflow-hidden bg-sidebar p-10 text-sidebar-foreground',
        className,
      )}
    >
      <div
        className={cn(
          'absolute inset-0 bg-[radial-gradient(ellipse_at_top_right,_hsl(221_83%_53%_/_0.35),_transparent_55%)]',
          'transition-opacity duration-700 ease-in-out motion-reduce:transition-none',
          isRegister ? 'opacity-0' : 'opacity-100',
        )}
        aria-hidden={isRegister}
      />
      <div
        className={cn(
          'absolute inset-0 bg-[radial-gradient(ellipse_at_top_left,_hsl(221_83%_53%_/_0.35),_transparent_55%)]',
          'transition-opacity duration-700 ease-in-out motion-reduce:transition-none',
          isRegister ? 'opacity-100' : 'opacity-0',
        )}
        aria-hidden={!isRegister}
      />

      <div className="relative z-10 h-full w-full overflow-hidden">
        <div
          className={cn('flex h-full w-[200%]', brandSlideClass, isRegister && '-translate-x-1/2')}
        >
          <BrandColumn side="login" onSwitchMode={onSwitchMode} />
          <BrandColumn side="register" onSwitchMode={onSwitchMode} />
        </div>
      </div>
    </div>
  )
}
