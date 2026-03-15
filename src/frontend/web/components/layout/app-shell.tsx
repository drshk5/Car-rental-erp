import Link from "next/link";
import type { ReactNode } from "react";
import {
  ArrowUpRight,
  BookOpenCheck,
  CalendarRange,
  CarFront,
  CreditCard,
  Gauge,
  LayoutDashboard,
  Settings2,
  ShieldCheck,
  Users2,
  Wrench,
} from "lucide-react";
import { AppearancePanel } from "@/components/theme/appearance-panel";
import { getSession } from "@/lib/auth";

const navItems = [
  { href: "/dashboard", label: "Dashboard", description: "Live command center", icon: LayoutDashboard },
  { href: "/owners", label: "Owners", description: "Partners and revenue", icon: Users2 },
  { href: "/vehicles", label: "Vehicles", description: "Fleet inventory", icon: CarFront },
  { href: "/customers", label: "Customers", description: "Profiles and verification", icon: ShieldCheck },
  { href: "/bookings", label: "Bookings", description: "Reservation pipeline", icon: CalendarRange },
  { href: "/rentals", label: "Rentals", description: "Active handovers", icon: Gauge },
  { href: "/payments", label: "Payments", description: "Cashflow and reconciliation", icon: CreditCard },
  { href: "/maintenance", label: "Maintenance", description: "Service scheduling", icon: Wrench },
  { href: "/settings", label: "Settings", description: "Access and branches", icon: Settings2 },
] as const;

const quickLinks = [
  { href: "/bookings", label: "Open reservations", icon: BookOpenCheck },
  { href: "/payments", label: "Review collections", icon: CreditCard },
  { href: "/settings", label: "Manage access", icon: ShieldCheck },
] as const;

export async function AppShell({
  title,
  currentPath,
  children,
}: {
  title: string;
  currentPath: string;
  children: ReactNode;
}) {
  const session = await getSession();
  const currentItem = navItems.find((item) => item.href === currentPath) ?? navItems[0];
  const todayLabel = new Intl.DateTimeFormat("en-US", {
    month: "long",
    day: "numeric",
    year: "numeric",
  }).format(new Date());
  const expiresLabel = session?.expiresAtUtc
    ? new Intl.DateTimeFormat("en-US", {
        month: "short",
        day: "numeric",
        hour: "numeric",
        minute: "2-digit",
      }).format(new Date(session.expiresAtUtc))
    : "Session active";

  return (
    <div className="shell">
      <aside className="shell__sidebar">
        <div className="shell__brand">
          <div className="shell__brand-mark">CR</div>
          <div className="shell__brand-copy">
            <div className="shell__eyebrow">Car Rental ERP</div>
            <h1 className="shell__title">Operations Atlas</h1>
            <p className="shell__description">
              A richer control surface for fleet movement, reservations, collections, and partner operations.
            </p>
          </div>
        </div>

        <div className="shell__profile">
          <div className="shell__profile-row">
            <div>
              <div className="shell__profile-label">Signed in as</div>
              <div className="shell__profile-name">{session?.fullName ?? "Authenticated User"}</div>
            </div>
            <div className="shell__role-pill">{session?.role ?? "Staff"}</div>
          </div>
          <div className="shell__profile-email">{session?.email ?? "Protected session"}</div>
          <div className="shell__meta-strip">
            <span>Branch {session?.branchId || "Global"}</span>
            <span>Expiry {expiresLabel}</span>
          </div>
        </div>

        <nav className="shell-nav" aria-label="Primary">
          {navItems.map((item) => {
            const Icon = item.icon;
            const isActive = currentPath === item.href || currentPath.startsWith(`${item.href}/`);

            return (
              <Link
                key={item.href}
                href={item.href}
                className={`shell-nav__link${isActive ? " shell-nav__link--active" : ""}`}
                aria-current={isActive ? "page" : undefined}
              >
                <span className="shell-nav__icon-wrap">
                  <Icon className="shell-nav__icon" />
                </span>
                <span className="shell-nav__copy">
                  <span className="shell-nav__label">{item.label}</span>
                  <span className="shell-nav__meta">{item.description}</span>
                </span>
              </Link>
            );
          })}
        </nav>

        <div className="shell__quick-panel">
          <div className="shell__session-label">Quick launch</div>
          <div className="shell__quick-list">
            {quickLinks.map((item) => {
              const Icon = item.icon;
              return (
                <Link key={item.href} href={item.href} className="shell__quick-link">
                  <span className="shell__quick-link-copy">
                    <Icon className="shell__quick-link-icon" />
                    {item.label}
                  </span>
                  <ArrowUpRight className="shell__quick-link-arrow" />
                </Link>
              );
            })}
          </div>
        </div>

        <div className="shell__footer">
          <form action="/auth/logout" method="post">
            <button type="submit" className="shell__logout">
              Sign out
            </button>
          </form>
        </div>
      </aside>

      <main className="shell__main">
        <header className="shell__header">
          <div className="shell__header-copy">
            <div className="shell__header-label">Unified Fleet Workspace</div>
            <h2 className="shell__header-title">{title}</h2>
            <p className="shell__header-description">
              {currentItem.description}. Navigate operational modules with the same polished dashboard rhythm.
            </p>
          </div>

          <div className="shell__header-actions">
            <div className="shell__session-card">
              <div className="shell__session-label">Live context</div>
              <div className="shell__session-value">{todayLabel}</div>
              <div className="shell__session-meta">Protected API routes, session cookies, and server-rendered views.</div>
            </div>
            <AppearancePanel />
          </div>
        </header>

        <section className="shell__hero">
          <div className="shell__hero-copy">
            <div className="shell__hero-badge">{currentItem.label}</div>
            <h3 className="shell__hero-title">A cleaner, richer dashboard shell for every operational workflow.</h3>
            <p className="shell__hero-description">
              The interface now mirrors a modern analytics console with stronger hierarchy, deeper visual rhythm, and
              better cross-module wayfinding.
            </p>
          </div>
          <div className="shell__hero-grid">
            <article className="shell__hero-card shell__hero-card--primary">
              <span>Workspace status</span>
              <strong>Ready for production-grade operations</strong>
            </article>
            <article className="shell__hero-card">
              <span>Navigation</span>
              <strong>Module-aware sidebar and quick actions</strong>
            </article>
            <article className="shell__hero-card">
              <span>Experience</span>
              <strong>Sharper glass surfaces, analytics framing, responsive layout</strong>
            </article>
          </div>
        </section>

        <div className="shell__mobile-nav" aria-label="Module shortcuts">
          {navItems.map((item) => {
            const isActive = currentPath === item.href || currentPath.startsWith(`${item.href}/`);
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`shell__mobile-pill${isActive ? " shell__mobile-pill--active" : ""}`}
              >
                {item.label}
              </Link>
            );
          })}
        </div>

        <div className="page-stack">{children}</div>
      </main>
    </div>
  );
}
