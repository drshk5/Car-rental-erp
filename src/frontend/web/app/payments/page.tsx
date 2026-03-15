import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro } from "@/components/ui/console";
import { PaymentsWorkspace } from "@/app/payments/payments-workspace";
import { getBookings } from "@/lib/bookings";
import { getPayments } from "@/lib/payments";
import { handleProtectedPageError } from "@/lib/page-guards";

export default async function PaymentsPage() {
  try {
    const [payments, bookings] = await Promise.all([getPayments(), getBookings()]);

    return (
      <AppShell title="Payments" currentPath="/payments">
        <SectionIntro
          eyebrow="Cashflow"
          title="Payment activity"
          description="Review collection volume, refund exposure, and payment metadata from the backend without losing readability."
        />
        <PaymentsWorkspace payments={payments.data} bookings={bookings.data} />
      </AppShell>
    );
  } catch (error) {
    handleProtectedPageError(error, "/payments");

    return (
      <AppShell title="Payments" currentPath="/payments">
        <ErrorState message="Payment data could not be loaded from the backend." error={error} />
      </AppShell>
    );
  }
}
