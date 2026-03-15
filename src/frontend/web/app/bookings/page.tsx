import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { BookingsWorkspace } from "@/app/bookings/bookings-workspace";
import { getBookings } from "@/lib/bookings";
import { getBranches } from "@/lib/settings";
import { getCustomers } from "@/lib/customers";
import { getVehicles } from "@/lib/vehicles";
import { formatCurrency } from "@/lib/format";
import { handleProtectedPageError } from "@/lib/page-guards";

export default async function BookingsPage() {
  try {
    const [bookings, customers, vehicles, branches] = await Promise.all([
      getBookings(),
      getCustomers(),
      getVehicles(),
      getBranches(),
    ]);
    const confirmed = bookings.data.filter((booking) => String(booking.status).toLowerCase() === "confirmed" || String(booking.status) === "2").length;
    const quoteVolume = bookings.data.reduce((sum, booking) => sum + booking.quotedTotal, 0);
    const outstanding = bookings.data.reduce((sum, booking) => sum + (booking.outstandingBalance ?? 0), 0);

    return (
      <AppShell title="Bookings" currentPath="/bookings">
        <SectionIntro
          eyebrow="Reservations"
          title="Bookings pipeline"
          description="Reservation flow is reframed as a richer dashboard workspace with pipeline metrics up front and the full booking queue underneath."
        />
        <StatGrid>
          <StatCard label="Reservations" value={String(bookings.data.length)} note={`${customers.data.length} customers in view`} />
          <StatCard label="Confirmed" value={String(confirmed)} note="Ready for dispatch" tone="accent" />
          <StatCard label="Quote volume" value={formatCurrency(quoteVolume)} note={`${vehicles.data.length} fleet options`} />
          <StatCard label="Outstanding" value={formatCurrency(outstanding)} note={`${branches.data.length} branch network`} />
        </StatGrid>
        <Surface
          eyebrow="Workflow framing"
          title="Reservation control with stronger hierarchy"
          description="This layout follows the reference pattern more closely: quick KPI scan first, then a denser operations surface for booking creation, confirmation, and cancellation."
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
