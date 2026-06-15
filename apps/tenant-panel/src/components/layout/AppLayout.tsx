import { Link, Outlet, useLocation } from 'react-router-dom'
import { LogOut } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/contexts/AuthContext'
import { cn } from '@/lib/utils'

const navItems = [
  { to: '/', label: 'Dashboard' },
  { to: '/logs', label: 'Logs', permission: 'logs:read' },
  { to: '/users', label: 'Users', permission: 'users:read' },
]

export function AppLayout() {
  const { user, logout, can } = useAuth()
  const location = useLocation()

  const visibleNav = navItems.filter((item) => !item.permission || can(item.permission))

  return (
    <div className="min-h-screen bg-muted/30">
      <header className="border-b bg-background">
        <div className="mx-auto flex h-16 max-w-6xl items-center justify-between px-4">
          <div className="flex items-center gap-8">
            <div>
              <p className="text-sm font-semibold">EnterpriseLogger</p>
              <p className="text-xs text-muted-foreground">{user?.tenantName}</p>
            </div>
            <nav className="flex items-center gap-1">
              {visibleNav.map((item) => (
                <Link
                  key={item.to}
                  to={item.to}
                  className={cn(
                    'rounded-md px-3 py-2 text-sm font-medium transition-colors',
                    location.pathname === item.to
                      ? 'bg-primary text-primary-foreground'
                      : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground',
                  )}
                >
                  {item.label}
                </Link>
              ))}
            </nav>
          </div>
          <div className="flex items-center gap-3">
            <div className="text-right text-sm">
              <p className="font-medium">{user?.email}</p>
              <p className="text-xs text-muted-foreground">{user?.role}</p>
            </div>
            <Button variant="outline" size="sm" onClick={logout}>
              <LogOut className="h-4 w-4" />
              Çıkış
            </Button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  )
}
