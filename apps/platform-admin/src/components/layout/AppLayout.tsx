import { Link, Outlet, useLocation } from 'react-router-dom'
import { Building2, CreditCard, LayoutDashboard, LogOut, Package, RefreshCw, ScrollText } from 'lucide-react'
import { useAuth } from '@/contexts/AuthContext'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const navItems = [
  { to: '/dashboard', label: 'Kontrol Paneli', icon: LayoutDashboard },
  { to: '/customers', label: 'Müşteriler', icon: Building2 },
  { to: '/packages', label: 'Paketler', icon: Package },
  { to: '/payments', label: 'Ödemeler', icon: CreditCard },
  { to: '/renewals', label: 'Yenilemeler', icon: RefreshCw },
  { to: '/audit-logs', label: 'Denetim', icon: ScrollText },
]

function SidebarLink({
  to,
  label,
  icon: Icon,
}: {
  to: string
  label: string
  icon: typeof Building2
}) {
  const location = useLocation()
  const isActive = location.pathname === to || (to !== '/' && location.pathname.startsWith(to))

  return (
    <Link
      to={to}
      className={cn(
        'flex items-center gap-3 rounded-xl px-3.5 py-2.5 text-sm font-medium transition-colors',
        isActive
          ? 'bg-primary text-primary-foreground shadow-sm'
          : 'text-sidebar-muted hover:bg-white/8 hover:text-sidebar-foreground',
      )}
    >
      <Icon className="h-4 w-4 shrink-0 opacity-80" />
      {label}
    </Link>
  )
}

export function AppLayout() {
  const { admin, logout } = useAuth()

  return (
    <div className="min-h-screen bg-background lg:flex">
      <aside className="border-b bg-sidebar text-sidebar-foreground lg:flex lg:w-64 lg:shrink-0 lg:flex-col lg:border-b-0 lg:border-r">
        <div className="flex items-center justify-between gap-3 px-4 py-4 lg:flex-col lg:items-stretch lg:px-4 lg:py-6">
          <div className="lg:px-2">
            <p className="text-xs font-medium uppercase tracking-wide text-sidebar-muted">
              Platform Admin
            </p>
            <h1 className="text-lg font-semibold">EnterpriseLogger</h1>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={logout}
            className="border-white/15 bg-transparent text-sidebar-foreground hover:bg-white/10 lg:w-full"
          >
            <LogOut className="mr-2 h-4 w-4" />
            Çıkış
          </Button>
        </div>

        <nav className="flex gap-1 overflow-x-auto px-3 pb-3 lg:flex-1 lg:flex-col lg:gap-1 lg:overflow-visible lg:px-3 lg:pb-6">
          {navItems.map((item) => (
            <SidebarLink key={item.to} {...item} />
          ))}
        </nav>

        <div className="hidden border-t border-white/10 px-4 py-4 text-xs text-sidebar-muted lg:block">
          {admin?.email}
        </div>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="border-b bg-card px-4 py-4 lg:hidden">
          <p className="text-sm text-muted-foreground">{admin?.email}</p>
        </header>
        <main className="flex-1 px-4 py-6 sm:px-6 lg:px-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
