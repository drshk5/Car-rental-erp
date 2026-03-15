"use client";

import Link from "next/link";
import { useMemo, useState, type ReactNode } from "react";
import {
  Bell,
  BookOpenCheck,
  BriefcaseBusiness,
  CalendarRange,
  CarFront,
  ChevronsLeft,
  ChevronsRight,
  ChevronRight,
  Command,
  CreditCard,
  LayoutDashboard,
  Menu,
  Search,
  Settings2,
  ShieldCheck,
  Sparkles,
  Users2,
  Wrench,
} from "lucide-react";
import { AppearancePanel } from "@/components/theme/appearance-panel";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Sheet, SheetContent } from "@/components/ui/sheet";
import { cn } from "@/lib/utils";
import type { AppShellSession } from "@/components/layout/app-shell";

type NavItem = {
  href: string;
  label: string;
  icon: typeof LayoutDashboard;
  section: "Operations" | "Administration";
  description: string;
};

const navItems: NavItem[] = [
  { href: "/dashboard", label: "Overview", icon: LayoutDashboard, section: "Operations", description: "Summary and analytics" },
  { href: "/bookings", label: "Bookings", icon: CalendarRange, section: "Operations", description: "Reservation pipeline" },
  { href: "/rentals", label: "Rentals", icon: BriefcaseBusiness, section: "Operations", description: "Live handovers" },
  { href: "/vehicles", label: "Vehicles", icon: CarFront, section: "Operations", description: "Fleet inventory" },
  { href: "/customers", label: "Customers", icon: Users2, section: "Operations", description: "Profiles and KYC" },
  { href: "/payments", label: "Payments", icon: CreditCard, section: "Operations", description: "Collections and refunds" },
  { href: "/maintenance", label: "Maintenance", icon: Wrench, section: "Operations", description: "Service schedules" },
  { href: "/owners", label: "Owners", icon: ShieldCheck, section: "Administration", description: "Partners and settlements" },
  { href: "/settings", label: "Settings", icon: Settings2, section: "Administration", description: "Roles, users, branches" },
];

const quickActions = [
  { href: "/dashboard", label: "View command center", icon: Sparkles },
  { href: "/bookings", label: "Open reservations", icon: BookOpenCheck },
  { href: "/settings", label: "Manage access", icon: ShieldCheck },
];

export function AppShellClient({
  title,
  currentPath,
  session,
  children,
}: {
  title: string;
  currentPath: string;
  session: AppShellSession;
  children: ReactNode;
}) {
  const [collapsed, setCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  const currentItem = navItems.find((item) => currentPath === item.href || currentPath.startsWith(`${item.href}/`)) ?? navItems[0];
  const sections = useMemo(() => {
    return ["Operations", "Administration"].map((section) => ({
      section,
      items: navItems.filter((item) => item.section === section),
    }));
  }, []);

  const breadcrumb = currentPath
    .split("/")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1));

  const todayLabel = new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
  }).format(new Date());

  return (
    <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,hsl(var(--primary)/0.06),transparent_22%),linear-gradient(180deg,hsl(var(--background))_0%,hsl(var(--background))_100%)] text-foreground">
      <div className="flex min-h-screen">
        <aside
          className={cn(
            "hidden h-screen overflow-hidden border-r border-white/6 bg-[#121923] text-slate-100 md:sticky md:top-0 md:flex md:flex-col",
            collapsed ? "md:w-[78px]" : "md:w-[284px]",
          )}
        >
          <SidebarContent
            collapsed={collapsed}
            currentPath={currentPath}
            session={session}
            sections={sections}
            onCollapseToggle={() => setCollapsed((value) => !value)}
          />
        </aside>

        <Sheet open={mobileOpen} onOpenChange={setMobileOpen}>
          <SheetContent side="left" className="w-[290px] border-border/60 bg-card/95 p-0 backdrop-blur-xl">
            <SidebarContent
              collapsed={false}
              currentPath={currentPath}
              session={session}
              sections={sections}
              onNavigate={() => setMobileOpen(false)}
              onCollapseToggle={() => setMobileOpen(false)}
            />
          </SheetContent>
        </Sheet>

        <div className="flex min-w-0 flex-1 flex-col">
          <header className="sticky top-0 z-20 border-b border-border/50 bg-background/94 backdrop-blur-xl">
            <div className="flex w-full items-center justify-between gap-4 px-3 py-3 lg:px-5">
              <div className="flex min-w-0 items-center gap-2.5">
                <Button variant="ghost" size="icon" className="md:hidden" onClick={() => setMobileOpen(true)}>
                  <Menu className="h-5 w-5" />
                </Button>
                <div className="hidden items-center gap-2 md:flex">
                  <Button
                    variant="ghost"
                    size="icon"
                    onClick={() => setCollapsed((value) => !value)}
                    aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"}
                    className="h-10 w-10 rounded-xl border border-border/60 bg-background text-foreground shadow-none hover:bg-muted"
                  >
                    {collapsed ? <ChevronsRight className="h-4.5 w-4.5" /> : <ChevronsLeft className="h-4.5 w-4.5" />}
                  </Button>
                  <div className="h-5 w-px bg-border/70" />
                </div>
                <div className="min-w-0">
                  <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.18em] text-muted-foreground">
                    <span>Workspace</span>
                    {breadcrumb.slice(-1).map((item) => (
                      <span key={item} className="inline-flex items-center gap-2">
                        <ChevronRight className="h-3.5 w-3.5" />
                        {item}
                      </span>
                    ))}
                  </div>
                  <div className="mt-1 text-[1.7rem] font-semibold tracking-tight">{title}</div>
                </div>
              </div>

              <div className="flex items-center gap-2">
                <div className="hidden w-[300px] items-center gap-2 rounded-2xl border border-border/70 bg-card px-3 py-2 lg:flex">
                  <Search className="h-4 w-4 text-muted-foreground" />
                  <Input
                    className="h-auto border-0 bg-transparent px-0 py-0 text-sm shadow-none focus-visible:ring-0"
                    placeholder="Search modules, customers, bookings..."
                  />
                  <div className="inline-flex items-center gap-1 rounded-md border border-border/70 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-[0.16em] text-muted-foreground">
                    <Command className="h-3 w-3" />
                    K
                  </div>
                </div>
                <div className="hidden items-center gap-2 rounded-2xl border border-border/70 bg-card px-3 py-2 text-sm text-muted-foreground xl:flex">
                  <Bell className="h-4 w-4" />
                  <span>{todayLabel}</span>
                </div>
                <AppearancePanel />
              </div>
            </div>
          </header>

          <main className="flex-1">
            <div className="flex w-full flex-col gap-6 px-3 py-6 lg:px-5">
              <section className="overflow-hidden rounded-[30px] border border-border/70 bg-card shadow-[0_1px_2px_hsl(var(--foreground)/0.03),0_24px_50px_hsl(var(--foreground)/0.05)]">
                <div className="grid gap-5 p-6 lg:grid-cols-[1.1fr_0.9fr] lg:p-7">
                  <div className="space-y-5">
                    <div className="inline-flex items-center gap-2 rounded-full border border-primary/18 bg-primary/8 px-3 py-1 text-[11px] font-semibold uppercase tracking-[0.2em] text-primary">
                      {currentItem.section}
                    </div>
                    <div className="max-w-3xl space-y-2.5">
                      <h1 className="text-[2rem] font-semibold leading-[1.06] tracking-[-0.04em] text-balance lg:text-[2.9rem]">
                        {currentItem.label} workspace with cleaner alignment, tighter spacing, and richer admin polish.
                      </h1>
                      <p className="max-w-2xl text-sm leading-7 text-muted-foreground lg:text-[15px]">
                        Refined spacing, quieter surfaces, and stronger component alignment make the product feel more disciplined and professional across every route.
                      </p>
                    </div>
                    <div className="grid gap-3 sm:grid-cols-3">
                      <div className="rounded-2xl border border-border/70 bg-background px-4 py-3.5">
                        <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Current module</div>
                        <div className="mt-1.5 text-sm font-semibold">{currentItem.description}</div>
                      </div>
                      <div className="rounded-2xl border border-border/70 bg-background px-4 py-3.5">
                        <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Signed in</div>
                        <div className="mt-1.5 text-sm font-semibold">{session?.fullName ?? "Authenticated user"}</div>
                      </div>
                      <div className="rounded-2xl border border-border/70 bg-background px-4 py-3.5">
                        <div className="text-[11px] font-semibold uppercase tracking-[0.18em] text-muted-foreground">Role</div>
                        <div className="mt-1.5 text-sm font-semibold">{session?.role ?? "Staff access"}</div>
                      </div>
                    </div>
                  </div>

                  <div className="grid gap-3 sm:grid-cols-3 lg:grid-cols-1">
                    {quickActions.map((action) => {
                      const Icon = action.icon;

                      return (
                        <Link
                          key={action.href}
                          href={action.href}
                          className="group rounded-[22px] border border-border/70 bg-background px-4 py-4 transition-all hover:-translate-y-0.5 hover:border-primary/20 hover:shadow-[0_12px_24px_hsl(var(--foreground)/0.06)]"
                        >
                          <div className="flex items-center justify-between gap-3">
                            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10 text-primary">
                              <Icon className="h-4 w-4" />
                            </div>
                            <ChevronRight className="h-4 w-4 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
                          </div>
                          <div className="mt-5 text-sm font-semibold">{action.label}</div>
                        </Link>
                      );
                    })}
                  </div>
                </div>
              </section>

              <section className="min-w-0">{children}</section>
            </div>
          </main>
        </div>
      </div>
    </div>
  );
}

function SidebarContent({
  collapsed,
  currentPath,
  session,
  sections,
  onNavigate,
  onCollapseToggle,
}: {
  collapsed: boolean;
  currentPath: string;
  session: AppShellSession;
  sections: Array<{ section: string; items: NavItem[] }>;
  onNavigate?: () => void;
  onCollapseToggle: () => void;
}) {
  return (
    <>
      <div className={cn("flex items-center border-b border-white/6 px-3 py-4", collapsed ? "justify-center" : "justify-between")}>
        <div className={cn("flex items-center gap-3", collapsed && "justify-center")}>
          <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-primary text-primary-foreground shadow-[0_10px_24px_hsl(var(--primary)/0.24)]">
            <CarFront className="h-5 w-5" />
          </div>
          {!collapsed ? (
            <div>
              <div className="text-sm font-semibold text-white">Car Rental ERP</div>
              <div className="text-xs text-slate-400">Operations console</div>
            </div>
          ) : null}
        </div>
        <Button
          variant="ghost"
          size="icon"
          onClick={onCollapseToggle}
          className={cn(
            "hidden h-8 w-8 rounded-lg text-slate-300 hover:bg-white/5 hover:text-white md:inline-flex",
            collapsed && "absolute left-1/2 top-4 -translate-x-1/2 opacity-0 pointer-events-none",
          )}
        >
          {collapsed ? <ChevronsRight className="h-4 w-4" /> : <ChevronsLeft className="h-4 w-4" />}
        </Button>
      </div>

      <div className="min-h-0 flex-1 overflow-y-auto px-2.5 py-4">
        {sections.map(({ section, items }) => (
          <div key={section} className="mb-6 space-y-2">
            {!collapsed ? (
              <div className="px-2 text-[11px] font-semibold uppercase tracking-[0.2em] text-slate-500">
                {section}
              </div>
            ) : null}
            <div className="space-y-1">
              {items.map((item) => {
                const Icon = item.icon;
                const isActive = currentPath === item.href || currentPath.startsWith(`${item.href}/`);

                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    onClick={onNavigate}
                    className={cn(
                      "group flex items-center gap-3 rounded-2xl border px-3 py-3 transition-all",
                      collapsed ? "justify-center px-1.5 py-3" : "",
                      isActive
                        ? "border-primary/10 bg-primary text-primary-foreground shadow-[0_10px_22px_hsl(var(--primary)/0.2)]"
                        : "border-transparent text-slate-100 hover:border-white/8 hover:bg-white/5",
                    )}
                  >
                    <div
                      className={cn(
                        "flex h-9 w-9 items-center justify-center rounded-xl",
                        isActive ? "bg-white/14" : "bg-white/6 text-slate-400 group-hover:text-white",
                      )}
                    >
                      <Icon className="h-4 w-4" />
                    </div>
                    {!collapsed ? (
                      <div className="min-w-0">
                        <div className={cn("text-sm font-medium", isActive && "text-primary-foreground")}>{item.label}</div>
                        <div className={cn("truncate text-xs", isActive ? "text-primary-foreground/75" : "text-slate-400")}>
                          {item.description}
                        </div>
                      </div>
                    ) : null}
                  </Link>
                );
              })}
            </div>
          </div>
        ))}
      </div>

      <div className="mt-auto shrink-0 border-t border-white/6 bg-[#121923] px-2.5 py-4">
        <div className={cn("rounded-2xl border border-white/8 bg-white/[0.03] p-3", collapsed && "p-2")}>
          <div className={cn("flex items-center gap-3", collapsed && "justify-center")}>
            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/12 text-primary">
              <Users2 className="h-4 w-4" />
            </div>
            {!collapsed ? (
              <div className="min-w-0">
                <div className="truncate text-sm font-medium text-white">{session?.fullName ?? "Authenticated User"}</div>
                <div className="truncate text-xs text-slate-400">{session?.email ?? "Protected session"}</div>
              </div>
            ) : null}
          </div>
          {!collapsed ? (
            <form action="/auth/logout" method="post" className="mt-3">
              <Button type="submit" variant="outline" className="w-full justify-start rounded-xl border-white/10 bg-white/[0.03] text-white hover:bg-white/[0.06] hover:text-white">
                Sign out
              </Button>
            </form>
          ) : (
            <form action="/auth/logout" method="post" className="mt-2">
              <Button
                type="submit"
                variant="outline"
                size="icon"
                className="h-11 w-full rounded-xl border-white/10 bg-white/[0.03] text-white hover:bg-white/[0.06] hover:text-white"
              >
                <Users2 className="h-4 w-4" />
              </Button>
            </form>
          )}
        </div>
      </div>
    </>
  );
}
