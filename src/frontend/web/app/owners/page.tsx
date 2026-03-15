import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { OwnersWorkspace } from "@/app/owners/owners-workspace";
import { getOwners, getOwnerRevenue } from "@/lib/owners";
import { formatCurrency } from "@/lib/format";
import { handleProtectedPageError } from "@/lib/page-guards";

export default async function OwnersPage() {
  try {
    const [owners, ownerRevenue] = await Promise.all([getOwners(), getOwnerRevenue()]);
    const activeOwners = owners.data.filter((owner) => owner.isActive).length;
    const gross = ownerRevenue.reduce((sum, item) => sum + item.grossRevenue, 0);
    const activeRentalCount = ownerRevenue.reduce((sum, item) => sum + item.activeRentalCount, 0);

    return (
      <AppShell title="Owners & Partners" currentPath="/owners">
        <SectionIntro
          eyebrow="Partner Network"
          title="Owner and settlement control"
          description="Partner operations now use the same refined dashboard framing as the rest of the app, with revenue and fleet exposure summarized before the working surface."
        />
        <StatGrid>
          <StatCard label="Partners" value={String(owners.data.length)} note={`${activeOwners} active relationships`} />
          <StatCard label="Gross revenue" value={formatCurrency(gross)} note="Partner-linked earnings" tone="accent" />
          <StatCard label="Active rentals" value={String(activeRentalCount)} note="Across owner fleets" />
          <StatCard label="Settlement rows" value={String(ownerRevenue.length)} note="Revenue snapshots" />
        </StatGrid>
        <Surface
          eyebrow="Workspace intent"
          title="Partner-facing revenue control"
          description="This view now reads like a finance and partner operations dashboard instead of a plain record list."
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
