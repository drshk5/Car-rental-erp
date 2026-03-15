import type { ReactNode } from "react";
import { getSession } from "@/lib/auth";
import type { UserSession } from "@/types/auth";
import { AppShellClient } from "@/components/layout/app-shell-client";

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

  return (
    <AppShellClient title={title} currentPath={currentPath} session={session}>
      {children}
    </AppShellClient>
  );
}

export type AppShellSession = UserSession | null;
