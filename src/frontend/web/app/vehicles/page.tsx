import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { VehiclesWorkspace } from "@/app/vehicles/vehicles-workspace";
import { handleProtectedPageError } from "@/lib/page-guards";
import { getOwners } from "@/lib/owners";
import { getBranches } from "@/lib/settings";
import { formatCurrency } from "@/lib/format";
import { getVehicles } from "@/lib/vehicles";

export default async function VehiclesPage() {
  try {
    const [vehicles, owners, branches] = await Promise.all([getVehicles(), getOwners(), getBranches()]);
    const available = vehicles.data.filter((vehicle) => String(vehicle.status).toLowerCase() === "available" || String(vehicle.status) === "1").length;
    const maintenance = vehicles.data.filter((vehicle) => String(vehicle.status).toLowerCase().includes("maintenance") || String(vehicle.status) === "4").length;
    const avgDailyRate =
      vehicles.data.length > 0
        ? vehicles.data.reduce((sum, vehicle) => sum + vehicle.dailyRate, 0) / vehicles.data.length
        : 0;

    return (
      <AppShell title="Vehicles" currentPath="/vehicles">
        <SectionIntro
          eyebrow="Fleet"
          title="Vehicle inventory"
          description="Inventory is now framed like a modern fleet console with operational metrics, cleaner surface grouping, and more deliberate information density."
        />
        <StatGrid>
          <StatCard label="Tracked vehicles" value={String(vehicles.data.length)} note={`${branches.data.length} branch locations`} />
          <StatCard label="Available now" value={String(available)} note="Ready to allocate" tone="accent" />
          <StatCard label="In maintenance" value={String(maintenance)} note="Workshop load" />
          <StatCard label="Avg daily rate" value={formatCurrency(avgDailyRate)} note={`${owners.data.length} active partners`} />
        </StatGrid>
        <Surface
          eyebrow="Workspace intent"
          title="Reference-style fleet management"
          description="The page now follows a tighter dashboard pattern: high-signal summaries first, then the working surface for edits, status changes, and catalog management."
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
