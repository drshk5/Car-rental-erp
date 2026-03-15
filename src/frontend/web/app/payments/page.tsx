import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { PaymentsWorkspace } from "@/app/payments/payments-workspace";
import { getBookings } from "@/lib/bookings";
import { getPayments } from "@/lib/payments";
import { formatCurrency } from "@/lib/format";
import { handleProtectedPageError } from "@/lib/page-guards";

export default async function PaymentsPage() {
  try {
    const [payments, bookings] = await Promise.all([getPayments(), getBookings()]);
    const collected = payments.data.reduce((sum, payment) => sum + payment.amount, 0);
    const quoted = bookings.data.reduce((sum, booking) => sum + booking.quotedTotal, 0);
    const outstanding = bookings.data.reduce((sum, booking) => sum + (booking.outstandingBalance ?? 0), 0);

    return (
      <AppShell title="Payments" currentPath="/payments">
        <SectionIntro
          eyebrow="Cashflow"
          title="Payment activity"
          description="Collections and outstanding exposure are now framed like a finance dashboard with stronger summaries and cleaner work surfaces."
        />
        <StatGrid>
          <StatCard label="Payments" value={String(payments.data.length)} note="Recorded transactions" />
          <StatCard label="Collected" value={formatCurrency(collected)} note="Captured cashflow" tone="accent" />
          <StatCard label="Quoted bookings" value={formatCurrency(quoted)} note={`${bookings.data.length} reservations`} />
          <StatCard label="Outstanding" value={formatCurrency(outstanding)} note="Balance still open" />
        </StatGrid>
        <Surface
          eyebrow="Workspace intent"
          title="A richer collections console"
          description="The view now mirrors a proper admin billing workspace: top-level money movement first, then the transaction and balance surface."
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
