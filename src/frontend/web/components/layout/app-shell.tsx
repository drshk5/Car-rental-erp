"use client";

import Link from "next/link";
import type { ReactNode } from "react";
import { AppearancePanel } from "@/components/theme/appearance-panel";

const navItems = [
  { href: "/dashboard", label: "Dashboard" },
  { href: "/owners", label: "Owners" },
  { href: "/vehicles", label: "Vehicles" },
  { href: "/customers", label: "Customers" },
  { href: "/bookings", label: "Bookings" },
  { href: "/rentals", label: "Rentals" },
  { href: "/payments", label: "Payments" },
  { href: "/maintenance", label: "Maintenance" },
  { href: "/settings", label: "Settings" },
];

export function AppShell({
  title,
  currentPath,
  children,
}: {
  title: string;
  currentPath: string;
  children: ReactNode;
}) {
  return (
    <div className="shell">
      <aside className="shell__sidebar">
        <div className="shell__brand">
          <div className="shell__eyebrow">Car Rental ERP</div>
          <h1 className="shell__title">Operations Console</h1>
          <p className="shell__description">
            Protected workspace for fleet control, settlements, and active rental operations.
          </p>
        </div>

        <div className="shell__profile">
          <div className="shell__profile-label">Signed in as</div>
          <div className="shell__profile-name">Authenticated User</div>
          <div className="shell__profile-email">Active Session</div>
          <div className="shell__role-pill">Staff</div>
        </div>

        <nav className="shell-nav">
          {navItems.map((item) => (
            <Link
              key={item.href}
              href={item.href}
              className={`shell-nav__link ${currentPath === item.href ? "shell-nav__link--active" : ""}`}
            >
              {item.label}
            </Link>
          ))}
        </nav>

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
          <div>
            <div className="shell__header-label">Secure Operations Workspace</div>
            <h2 className="shell__header-title">{title}</h2>
          </div>
          <div className="shell__header-actions">
            <div className="shell__session-card">
              <div className="shell__session-label">Session</div>
              <div className="shell__session-value">JWT cookies and protected API routes</div>
            </div>
            <AppearancePanel />
          </div>
        </header>
        <div className="page-stack">{children}</div>
      </main>
    </div>
  );
}
