import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro } from "@/components/ui/console";
import { VehiclesWorkspace } from "@/app/vehicles/vehicles-workspace";
import { handleProtectedPageError } from "@/lib/page-guards";
import { getOwners } from "@/lib/owners";
import { getBranches } from "@/lib/settings";
import { getVehicles } from "@/lib/vehicles";

export default async function VehiclesPage() {
  try {
    const [vehicles, owners, branches] = await Promise.all([getVehicles(), getOwners(), getBranches()]);

    return (
      <AppShell title="Vehicles" currentPath="/vehicles">
        <SectionIntro
          eyebrow="Fleet"
          title="Vehicle inventory"
          description="Track owner assignment, branch placement, pricing, and status without relying on fixed-width desktop grids."
        />
        <VehiclesWorkspace vehicles={vehicles.data} owners={owners.data} branches={branches.data} />
      </AppShell>
    );
  } catch (error) {
    handleProtectedPageError(error, "/vehicles");

    return (
      <AppShell title="Vehicles" currentPath="/vehicles">
        <ErrorState message="Vehicle data could not be loaded from the backend." error={error} />
      </AppShell>
    );
  }
}
