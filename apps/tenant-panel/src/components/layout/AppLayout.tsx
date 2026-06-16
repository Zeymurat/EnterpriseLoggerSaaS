import { Link, Outlet, useLocation } from 'react-router-dom'
import {
  LayoutDashboard,
  LogOut,
  ScrollText,
  Users,
  Activity,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { UserRoleBadge } from '@/components/users/UserRoleBadge'
import { useAuth } from '@/contexts/AuthContext'
import { cn } from '@/lib/utils'

const navItems = [
  { to: '/', label: 'Kontrol Paneli', icon: LayoutDashboard },
  { to: '/logs', label: 'Loglar', icon: ScrollText, permission: 'logs:read' },
  { to: '/users', label: 'Kullanıcılar', icon: Users, permission: 'users:read' },
]

function NavLink({ to, label, icon: Icon }: { to: string; label: string; icon: typeof LayoutDashboard }) {
  const location = useLocation()
  const isActive = location.pathname === to

  return (
    <Link
      to={to}
      className={cn(
        'flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition-all',
        isActive
          ? 'bg-primary text-primary-foreground shadow-sm'
          : 'text-muted-foreground hover:bg-muted hover:text-foreground',
      )}
    >
      <Icon className="h-4 w-4 shrink-0" />
      {label}
    </Link>
  )
}

export function AppLayout() {
  const { user, logout, can } = useAuth()
  const location = useLocation()
  const visibleNav = navItems.filter((item) => !item.permission || can(item.permission))

  const initials = user?.email?.slice(0, 2).toUpperCase() ?? '?'

  return (
    <div className="flex min-h-screen">
      <aside className="fixed inset-y-0 left-0 z-50 hidden w-64 flex-col border-r bg-card lg:flex">
        <div className="flex h-16 items-center gap-3 border-b px-5">
          <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-primary text-primary-foreground shadow-sm">
            <Activity className="h-5 w-5" />
          </div>
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold">EnterpriseLogger</p>
            <p className="truncate text-xs text-muted-foreground">{user?.tenantName}</p>
          </div>
        </div>

        <nav className="flex-1 space-y-1 p-4">
          {visibleNav.map((item) => (
            <NavLink key={item.to} {...item} />
          ))}
        </nav>

        <div className="border-t p-4">
          <div className="flex items-center gap-3 rounded-xl bg-muted/50 p-3">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
              {initials}
            </div>
            <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-medium">{user?.email}</p>
              <div className="mt-1">
                <UserRoleBadge role={user?.role ?? 'User'} />
              </div>
            </div>
          </div>
          <Button variant="outline" size="sm" className="mt-3 w-full" onClick={logout}>
            <LogOut className="h-4 w-4" />
            Çıkış yap
          </Button>
        </div>
      </aside>

      <div className="flex min-h-screen flex-1 flex-col lg:pl-64">
        <header className="sticky top-0 z-40 border-b bg-background/80 backdrop-blur-md lg:hidden">
          <div className="flex h-14 items-center justify-between px-4">
            <div className="flex items-center gap-2">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
                <Activity className="h-4 w-4" />
              </div>
              <span className="text-sm font-semibold">{user?.tenantName}</span>
            </div>
            <Button variant="ghost" size="sm" onClick={logout}>
              <LogOut className="h-4 w-4" />
            </Button>
          </div>
          <nav className="flex gap-1 overflow-x-auto border-t px-2 py-2">
            {visibleNav.map((item) => {
              const isActive = location.pathname === item.to
              return (
                <Link
                  key={item.to}
                  to={item.to}
                  className={cn(
                    'flex shrink-0 items-center gap-1.5 rounded-lg px-3 py-2 text-xs font-medium',
                    isActive
                      ? 'bg-primary text-primary-foreground'
                      : 'text-muted-foreground hover:bg-muted',
                  )}
                >
                  <item.icon className="h-3.5 w-3.5" />
                  {item.label}
                </Link>
              )
            })}
          </nav>
        </header>

        <main className="flex-1 px-4 py-6 lg:px-8 lg:py-8">
          <div className="mx-auto max-w-6xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
