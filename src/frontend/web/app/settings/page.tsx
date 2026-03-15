import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro } from "@/components/ui/console";
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

    return (
      <AppShell title="Settings" currentPath="/settings">
        <SectionIntro
          eyebrow="Administration"
          title="Access, branches, and environment"
          description="Settings now surfaces the remaining admin endpoints so operational configuration is visible from the frontend."
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
