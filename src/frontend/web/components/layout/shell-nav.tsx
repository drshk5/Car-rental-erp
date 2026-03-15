"use client";

import Link from "next/link";

type NavItem = {
  href: string;
  label: string;
};

export function ShellNav({
  currentPath,
  items,
}: {
  currentPath: string;
  items: NavItem[];
}) {
  return (
    <nav className="shell-nav" aria-label="Primary">
      {items.map((item) => {
        const isActive = currentPath === item.href || currentPath.startsWith(`${item.href}/`);

        return (
          <Link
            key={item.href}
            href={item.href}
            className={`shell-nav__link${isActive ? " shell-nav__link--active" : ""}`}
            aria-current={isActive ? "page" : undefined}
          >
            {item.label}
          </Link>
        );
      })}
    </nav>
  );
}
