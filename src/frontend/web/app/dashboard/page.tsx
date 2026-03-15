import { AppShell } from "@/components/layout/app-shell";
import { DetailList, EmptyState, ErrorState, SectionIntro, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { getDashboardSummary } from "@/lib/dashboard";
import { formatCurrency } from "@/lib/format";
import { handleProtectedPageError } from "@/lib/page-guards";

export default async function DashboardPage() {
  try {
    const summary = await getDashboardSummary();
    const cards = [
      { label: "Available Cars", value: summary.availableVehicles },
      { label: "Active Rentals", value: summary.activeRentals },
      { label: "Today Pickups", value: summary.todayPickups },
      { label: "Today Returns", value: summary.todayReturns },
      { label: "Overdue Rentals", value: summary.overdueRentals },
      { label: "Unpaid Bookings", value: summary.unpaidBookings },
    ];

    return (
      <AppShell title="Dashboard" currentPath="/dashboard">
        <SectionIntro
          eyebrow="Command Center"
          title="Operations overview"
          description="Track live fleet availability, booking demand, overdue exposure, and owner revenue in one place."
        />
        <StatGrid>
          {cards.map((card, index) => (
            <StatCard
              key={card.label}
              label={card.label}
              value={String(card.value)}
              tone={index < 2 ? "accent" : "default"}
            />
          ))}
        </StatGrid>
        <Surface
          eyebrow="Owner Revenue"
          title="Partner-wise fleet earnings"
          description="Settlement snapshots are calculated from booking and payment activity already stored in the backend."
        >
          <StatGrid>
            <StatCard label="Revenue today" value={formatCurrency(summary.revenueToday)} tone="warm" />
            <StatCard label="Revenue this month" value={formatCurrency(summary.revenueThisMonth)} tone="accent" />
            <StatCard label="Vehicles in maintenance" value={String(summary.vehiclesInMaintenance)} />
          </StatGrid>
          {summary.ownerRevenue.length === 0 ? (
            <EmptyState message="Owner revenue will appear once bookings and settlements start flowing." />
          ) : (
            <div className="record-grid">
              {summary.ownerRevenue.map((item) => (
                <article key={item.ownerId} className="record-card">
                  <h4 className="record-card__title">{item.ownerName}</h4>
                  <DetailList
                    items={[
                      { label: "Vehicles", value: String(item.vehicleCount) },
                      { label: "Gross", value: formatCurrency(item.grossRevenue) },
                      { label: "Partner share", value: formatCurrency(item.partnerShareAmount) },
                      { label: "Company share", value: formatCurrency(item.companyShareAmount) },
                    ]}
                  />
                </article>
              ))}
            </div>
          )}
        </Surface>
      </AppShell>
    );
  } catch (error) {
    handleProtectedPageError(error, "/dashboard");

    return (
      <AppShell title="Dashboard" currentPath="/dashboard">
        <ErrorState message="Dashboard data could not be loaded from the backend." error={error} />
      </AppShell>
    );
  }
}
