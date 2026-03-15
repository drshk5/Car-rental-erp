import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro } from "@/components/ui/console";
import { BookingsWorkspace } from "@/app/bookings/bookings-workspace";
import { getBookings } from "@/lib/bookings";
import { getBranches } from "@/lib/settings";
import { getCustomers } from "@/lib/customers";
import { getVehicles } from "@/lib/vehicles";
import { handleProtectedPageError } from "@/lib/page-guards";

export default async function BookingsPage() {
  try {
    const [bookings, customers, vehicles, branches] = await Promise.all([
      getBookings(),
      getCustomers(),
      getVehicles(),
      getBranches(),
    ]);

    return (
      <AppShell title="Bookings" currentPath="/bookings">
        <SectionIntro
          eyebrow="Reservations"
          title="Bookings pipeline"
          description="Pickup and return logistics, pricing plan, and quoted totals are grouped into a cleaner reservation workflow view."
        />
        <BookingsWorkspace bookings={bookings.data} customers={customers.data} vehicles={vehicles.data} branches={branches.data} />
      </AppShell>
    );
  } catch (error) {
    handleProtectedPageError(error, "/bookings");

    return (
      <AppShell title="Bookings" currentPath="/bookings">
        <ErrorState message="Booking data could not be loaded from the backend." error={error} />
      </AppShell>
    );
  }
}
