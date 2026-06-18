import type { ReactNode } from 'react'
import { Activity } from 'lucide-react'
import { BrandPanel, brandPanelPositionClass, type AuthMode } from '@/components/auth/BrandPanel'
import { cn } from '@/lib/utils'

interface AuthShellProps {
  mode: AuthMode
  onSwitchMode: (mode: AuthMode) => void
  loginPanel: ReactNode
  registerPanel: ReactNode
}

export function AuthShell({ mode, onSwitchMode, loginPanel, registerPanel }: AuthShellProps) {
  return (
    <div className="flex min-h-screen flex-col">
      <div className="border-b bg-card p-4 lg:hidden">
        <div className="mx-auto flex max-w-md items-center justify-between">
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
              <Activity className="h-4 w-4" />
            </div>
            <span className="text-sm font-semibold">EnterpriseLogger</span>
          </div>
          <div className="flex rounded-lg border bg-muted/40 p-1 text-xs font-medium">
            <button
              type="button"
              className={cn(
                'rounded-md px-3 py-1.5 transition-colors',
                mode === 'login' ? 'bg-background shadow-sm' : 'text-muted-foreground',
              )}
              onClick={() => onSwitchMode('login')}
            >
              Giriş
            </button>
            <button
              type="button"
              className={cn(
                'rounded-md px-3 py-1.5 transition-colors',
                mode === 'register' ? 'bg-background shadow-sm' : 'text-muted-foreground',
              )}
              onClick={() => onSwitchMode('register')}
            >
              Kayıt
            </button>
          </div>
        </div>
      </div>

      <div className="relative flex-1 overflow-hidden">
        <div className="grid min-h-full lg:min-h-[calc(100vh-0px)] lg:grid-cols-2">
          <div
            className={cn(
              'flex w-full items-center justify-center p-6 sm:p-10 lg:px-12 xl:px-16',
              mode === 'login' ? 'hidden lg:flex' : 'flex',
            )}
          >
            {registerPanel}
          </div>

          <div
            className={cn(
              'flex w-full items-center justify-center p-6 sm:p-10 lg:px-12 xl:px-16',
              mode === 'register' ? 'hidden lg:flex' : 'flex',
            )}
          >
            {loginPanel}
          </div>
        </div>

        <div
          className={cn(
            'absolute inset-y-0 z-10 hidden w-1/2 lg:block',
            brandPanelPositionClass,
            mode === 'login' ? 'left-0' : 'left-1/2',
          )}
        >
          <BrandPanel mode={mode} onSwitchMode={onSwitchMode} className="h-full shadow-2xl" />
        </div>
      </div>
    </div>
  )
}
