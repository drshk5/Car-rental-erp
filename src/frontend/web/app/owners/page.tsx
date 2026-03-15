import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro } from "@/components/ui/console";
import { OwnersWorkspace } from "@/app/owners/owners-workspace";
import { getOwners, getOwnerRevenue } from "@/lib/owners";
import { handleProtectedPageError } from "@/lib/page-guards";

export default async function OwnersPage() {
  try {
    const [owners, ownerRevenue] = await Promise.all([getOwners(), getOwnerRevenue()]);

    return (
      <AppShell title="Owners & Partners" currentPath="/owners">
        <SectionIntro
          eyebrow="Partner Network"
          title="Owner and settlement control"
          description="Every fleet partner should be traceable for revenue share, contact ownership, and settlement exposure."
        />
        <OwnersWorkspace owners={owners.data} revenue={ownerRevenue} />
      </AppShell>
    );
  } catch (error) {
    handleProtectedPageError(error, "/owners");

    return (
      <AppShell title="Owners & Partners" currentPath="/owners">
        <ErrorState message="Owner data could not be loaded from the backend." error={error} />
      </AppShell>
    );
  }
}
