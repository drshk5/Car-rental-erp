import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { SettingsWorkspace } from "@/app/settings/settings-workspace";
import { handleProtectedPageError } from "@/lib/page-guards";
import { getBranches, getCurrentUserProfile, getPermissions, getRoles, getUsers } from "@/lib/settings";
import { getSystemHealth } from "@/lib/system";

export default async function SettingsPage() {
  try {
    const [profile, users, roles, branches, permissions, system] = await Promise.all([
      getCurrentUserProfile(),
      getUsers(),
      getRoles(),
      getBranches(),
      getPermissions(),
      getSystemHealth(),
    ]);
    const activeUsers = users.data.filter((user) => user.isActive).length;
    const activeBranches = branches.data.filter((branch) => branch.isActive).length;

    return (
      <AppShell title="Settings" currentPath="/settings">
        <SectionIntro
          eyebrow="Administration"
          title="Access, branches, and environment"
          description="Administration is now framed like an enterprise control panel with clearer system health and access-management summaries."
        />
        <StatGrid>
          <StatCard label="Users" value={String(users.data.length)} note={`${activeUsers} active accounts`} />
          <StatCard label="Roles" value={String(roles.length)} note={`${permissions.length} permission entries`} tone="accent" />
          <StatCard label="Branches" value={String(branches.data.length)} note={`${activeBranches} active locations`} />
          <StatCard label="System" value={system.status} note={system.environment} />
        </StatGrid>
        <Surface
          eyebrow="Workspace intent"
          title="Administrative controls with cleaner hierarchy"
          description="The admin area now follows the same richer dashboard pattern as operations pages, while still exposing user, role, branch, and health controls."
        />
        <SettingsWorkspace
          profile={profile}
          users={users.data}
          roles={roles}
          branches={branches.data}
          permissions={permissions}
          system={system}
        />
      </AppShell>
    );
  } catch (error) {
    handleProtectedPageError(error, "/settings");

    return (
      <AppShell title="Settings" currentPath="/settings">
        <ErrorState message="Settings data could not be loaded from the backend." error={error} />
      </AppShell>
    );
  }
}
