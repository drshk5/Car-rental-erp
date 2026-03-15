import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { MaintenanceWorkspace } from "@/app/maintenance/maintenance-workspace";
import { getMaintenanceRecords } from "@/lib/maintenance";
import { handleProtectedPageError } from "@/lib/page-guards";
import { formatCurrency } from "@/lib/format";
import { getVehicles } from "@/lib/vehicles";

export default async function MaintenancePage() {
  try {
    const [records, vehicles] = await Promise.all([getMaintenanceRecords(), getVehicles()]);
    const openCount = records.data.filter((record) => !record.completedAtUtc).length;
    const completed = records.data.filter((record) => Boolean(record.completedAtUtc)).length;
    const totalCost = records.data.reduce((sum, record) => sum + record.cost, 0);

    return (
      <AppShell title="Maintenance" currentPath="/maintenance">
        <SectionIntro
          eyebrow="Fleet Care"
          title="Maintenance planning"
          description="Service schedules and workshop costs now live in a denser fleet-care workspace with clearer top-level signals and cleaner operational framing."
        />
        <StatGrid>
          <StatCard label="Service records" value={String(records.data.length)} note={`${vehicles.data.length} vehicles tracked`} />
          <StatCard label="Open jobs" value={String(openCount)} note="Still in progress" tone="accent" />
          <StatCard label="Completed" value={String(completed)} note="Closed services" />
          <StatCard label="Spend" value={formatCurrency(totalCost)} note="Recorded workshop cost" />
        </StatGrid>
        <Surface
          eyebrow="Workspace intent"
          title="Maintenance operations with admin-grade chrome"
          description="This route now follows the same premium dashboard structure as reservations, payments, and fleet inventory."
        />
        <MaintenanceWorkspace records={records.data} vehicles={vehicles.data} />
      </AppShell>
    );
  } catch (error) {
    handleProtectedPageError(error, "/maintenance");

    return (
      <AppShell title="Maintenance" currentPath="/maintenance">
        <ErrorState message="Maintenance data could not be loaded from the backend." error={error} />
      </AppShell>
    );
  }
}
