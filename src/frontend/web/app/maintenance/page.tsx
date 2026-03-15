import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro } from "@/components/ui/console";
import { MaintenanceWorkspace } from "@/app/maintenance/maintenance-workspace";
import { getMaintenanceRecords } from "@/lib/maintenance";
import { handleProtectedPageError } from "@/lib/page-guards";
import { getVehicles } from "@/lib/vehicles";

export default async function MaintenancePage() {
  try {
    const [records, vehicles] = await Promise.all([getMaintenanceRecords(), getVehicles()]);

    return (
      <AppShell title="Maintenance" currentPath="/maintenance">
        <SectionIntro
          eyebrow="Fleet Care"
          title="Maintenance planning"
          description="The maintenance endpoint is now surfaced in the frontend so service schedules and costs are not hidden from operations."
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
