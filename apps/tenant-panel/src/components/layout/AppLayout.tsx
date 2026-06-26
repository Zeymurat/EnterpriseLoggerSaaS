import { Link, Outlet, useLocation } from 'react-router-dom'

import {

  LayoutDashboard,

  LogOut,

  ScrollText,

  Users,

  Activity,

  CreditCard,

} from 'lucide-react'

import { Button } from '@/components/ui/button'

import { BillingNoticeBanner } from '@/components/billing/BillingNoticeBanner'
import { QuotaUsageBanner } from '@/components/billing/QuotaUsageBanner'

import { UserRoleBadge } from '@/components/users/UserRoleBadge'

import { useAuth } from '@/contexts/AuthContext'

import { cn } from '@/lib/utils'



const navItems = [

  { to: '/', label: 'Kontrol Paneli', icon: LayoutDashboard },

  { to: '/billing', label: 'Abonelik', icon: CreditCard },

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

        'group flex items-center gap-3 rounded-xl px-3.5 py-2.5 text-sm font-medium transition-all duration-200',

        isActive

          ? 'bg-primary text-primary-foreground shadow-[0_8px_24px_hsl(21_96%_28%_/0.35)]'

          : 'text-sidebar-muted hover:bg-white/8 hover:text-sidebar-foreground',

      )}

    >

      <Icon className={cn('h-4 w-4 shrink-0', isActive ? 'opacity-100' : 'opacity-70 group-hover:opacity-100')} />

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

      <aside className="fixed inset-y-0 left-0 z-50 hidden w-[17.5rem] flex-col border-r border-white/8 bg-[linear-gradient(180deg,hsl(var(--sidebar))_0%,hsl(22_52%_7%)_100%)] text-sidebar-foreground lg:flex">

        <div className="flex h-[4.25rem] items-center gap-3 px-5">

          <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-primary text-primary-foreground shadow-[0_8px_20px_hsl(21_96%_28%_/0.4)]">

            <Activity className="h-5 w-5" />

          </div>

          <div className="min-w-0">

            <p className="truncate text-sm font-semibold tracking-tight">EnterpriseLogger</p>

            <p className="truncate text-xs text-sidebar-muted">{user?.tenantName}</p>

          </div>

        </div>



        <nav className="flex-1 space-y-1.5 px-4 py-2">

          <p className="px-3.5 pb-1 text-[10px] font-semibold uppercase tracking-[0.16em] text-sidebar-muted/80">

            Menü

          </p>

          {visibleNav.map((item) => (

            <NavLink key={item.to} {...item} />

          ))}

        </nav>



        <div className="border-t border-white/8 p-4">

          <div className="flex items-center gap-3 rounded-2xl border border-white/8 bg-white/5 p-3 backdrop-blur-sm">

            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-primary/25 text-sm font-semibold text-primary-foreground ring-2 ring-white/10">

              {initials}

            </div>

            <div className="min-w-0 flex-1">

              <p className="truncate text-sm font-medium">{user?.email}</p>

              <div className="mt-1">

                <UserRoleBadge role={user?.role ?? 'User'} />

              </div>

            </div>

          </div>

          <Button

            variant="outline"

            size="sm"

            className="mt-3 w-full rounded-xl border-white/12 bg-transparent text-sidebar-foreground hover:bg-white/10 hover:text-sidebar-foreground"

            onClick={logout}

          >

            <LogOut className="h-4 w-4" />

            Çıkış yap

          </Button>

        </div>

      </aside>



      <div className="flex min-h-screen flex-1 flex-col lg:pl-[17.5rem]">

        <header className="sticky top-0 z-40 border-b border-border/60 bg-card/75 backdrop-blur-xl lg:hidden">

          <div className="flex h-14 items-center justify-between px-4">

            <div className="flex items-center gap-2">

              <div className="flex h-8 w-8 items-center justify-center rounded-xl bg-primary text-primary-foreground">

                <Activity className="h-4 w-4" />

              </div>

              <span className="text-sm font-semibold">{user?.tenantName}</span>

            </div>

            <Button variant="ghost" size="sm" onClick={logout}>

              <LogOut className="h-4 w-4" />

            </Button>

          </div>

          <nav className="flex gap-1 overflow-x-auto px-3 pb-3">

            {visibleNav.map((item) => {

              const isActive = location.pathname === item.to

              return (

                <Link

                  key={item.to}

                  to={item.to}

                  className={cn(

                    'flex shrink-0 items-center gap-1.5 rounded-xl px-3 py-2 text-xs font-medium transition-colors',

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



        <main className="flex-1 px-4 py-6 lg:px-10 lg:py-10">

          <div className="mx-auto max-w-7xl">

            <BillingNoticeBanner />
            <QuotaUsageBanner />

            <Outlet />

          </div>

        </main>

      </div>

    </div>

  )

}


