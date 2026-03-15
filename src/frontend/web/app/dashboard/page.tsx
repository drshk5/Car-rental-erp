import { AlertTriangle, ArrowUpRight, Banknote, CalendarClock, CarFront, TimerReset, Wrench } from "lucide-react";
import { AppShell } from "@/components/layout/app-shell";
import { DetailList, EmptyState, ErrorState, SectionIntro, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { getDashboardSummary } from "@/lib/dashboard";
import { formatCurrency } from "@/lib/format";
import { handleProtectedPageError } from "@/lib/page-guards";

export default async function DashboardPage() {
  try {
    const summary = await getDashboardSummary();
    const fleetTotal = summary.availableVehicles + summary.activeRentals + summary.vehiclesInMaintenance;
    const utilization = fleetTotal > 0 ? Math.round((summary.activeRentals / fleetTotal) * 100) : 0;
    const readiness = fleetTotal > 0 ? Math.round((summary.availableVehicles / fleetTotal) * 100) : 0;
    const topOwners = [...summary.ownerRevenue]
      .sort((left, right) => right.grossRevenue - left.grossRevenue)
      .slice(0, 4);

    const cards = [
      { label: "Available Cars", value: summary.availableVehicles, note: `${readiness}% fleet ready`, tone: "accent" as const },
      { label: "Active Rentals", value: summary.activeRentals, note: `${utilization}% utilization`, tone: "accent" as const },
      { label: "Today Pickups", value: summary.todayPickups, note: "Dispatch board", tone: "default" as const },
      { label: "Today Returns", value: summary.todayReturns, note: "Handback queue", tone: "default" as const },
      { label: "Overdue Rentals", value: summary.overdueRentals, note: "Requires follow-up", tone: "warm" as const },
      { label: "Unpaid Bookings", value: summary.unpaidBookings, note: "Collection risk", tone: "warm" as const },
    ];

    const focusLanes = [
      {
        label: "Revenue today",
        value: formatCurrency(summary.revenueToday),
        detail: "Daily inflow",
        icon: Banknote,
      },
      {
        label: "Revenue this month",
        value: formatCurrency(summary.revenueThisMonth),
        detail: "Month-to-date",
        icon: ArrowUpRight,
      },
      {
        label: "Maintenance load",
        value: String(summary.vehiclesInMaintenance),
        detail: "Vehicles in workshop",
        icon: Wrench,
      },
    ];

    const actionBoard = [
      {
        title: "Pickup readiness",
        count: summary.todayPickups,
        icon: CalendarClock,
        description: "Prioritize vehicle prep, ID checks, and dispatch staging for today’s reserved pickups.",
      },
      {
        title: "Return throughput",
        count: summary.todayReturns,
        icon: TimerReset,
        description: "Balance return inspections, cleaning turnaround, and availability recovery for the next booking cycle.",
      },
      {
        title: "Risk watch",
        count: summary.overdueRentals + summary.unpaidBookings,
        icon: AlertTriangle,
        description: "Combine overdue movement and payment backlog into one operational escalation queue.",
      },
    ];

    return (
      <AppShell title="Dashboard" currentPath="/dashboard">
        <section className="dashboard-hero">
          <div className="dashboard-hero__copy">
            <SectionIntro
              eyebrow="Command Center"
              title="Operations overview"
              description="Track live fleet availability, booking demand, payment exposure, and partner revenue in a dashboard layout inspired by modern analytics products."
            />
            <div className="dashboard-hero__callout">
              <span className="dashboard-hero__callout-label">Operations health</span>
              <strong className="dashboard-hero__callout-value">{readiness}% ready fleet</strong>
              <p className="dashboard-hero__callout-text">
                Available vehicles, scheduled handovers, service load, and collection pressure are all framed in a single executive view.
              </p>
            </div>
          </div>

          <div className="dashboard-hero__panel">
            <div className="dashboard-hero__panel-header">
              <span>Fleet posture</span>
              <strong>{fleetTotal || 0} total tracked units</strong>
            </div>
            <div className="dashboard-hero__progress-list">
              <MetricBar label="Available" value={summary.availableVehicles} total={fleetTotal} tone="primary" />
              <MetricBar label="On rent" value={summary.activeRentals} total={fleetTotal} tone="secondary" />
              <MetricBar label="In service" value={summary.vehiclesInMaintenance} total={fleetTotal} tone="muted" />
            </div>
          </div>
        </section>

        <StatGrid>
          {cards.map((card) => (
            <StatCard key={card.label} label={card.label} value={String(card.value)} note={card.note} tone={card.tone} />
          ))}
        </StatGrid>

        <div className="dashboard-grid">
          <Surface
            eyebrow="Operating signals"
            title="Daily action board"
            description="The dashboard starter uses varied panel density; this board applies that same idea to your dispatch, returns, and risk workflows."
          >
            <div className="dashboard-action-grid">
              {actionBoard.map((item) => {
                const Icon = item.icon;

                return (
                  <article key={item.title} className="dashboard-action-card">
                    <div className="dashboard-action-card__icon">
                      <Icon className="h-5 w-5" />
                    </div>
                    <div className="dashboard-action-card__count">{item.count}</div>
                    <h4 className="dashboard-action-card__title">{item.title}</h4>
                    <p className="dashboard-action-card__text">{item.description}</p>
                  </article>
                );
              })}
            </div>
          </Surface>

          <Surface
            eyebrow="Revenue pulse"
            title="Financial highlights"
            description="A compact insight rail for money movement and workshop pressure."
          >
            <div className="dashboard-focus-list">
              {focusLanes.map((item) => {
                const Icon = item.icon;

                return (
                  <article key={item.label} className="dashboard-focus-card">
                    <div className="dashboard-focus-card__icon">
                      <Icon className="h-5 w-5" />
                    </div>
                    <div>
                      <div className="dashboard-focus-card__label">{item.label}</div>
                      <div className="dashboard-focus-card__value">{item.value}</div>
                      <div className="dashboard-focus-card__meta">{item.detail}</div>
                    </div>
                  </article>
                );
              })}
            </div>
          </Surface>
        </div>

        <Surface
          eyebrow="Owner revenue"
          title="Partner-wise fleet earnings"
          description="Settlement snapshots are calculated from booking and payment activity already stored in the backend."
        >
          {topOwners.length === 0 ? (
            <EmptyState message="Owner revenue will appear once bookings and settlements start flowing." />
          ) : (
            <div className="dashboard-owner-grid">
              {topOwners.map((item, index) => {
                const sharePercent = item.grossRevenue > 0 ? Math.round((item.partnerShareAmount / item.grossRevenue) * 100) : 0;

                return (
                  <article key={item.ownerId} className="record-card dashboard-owner-card">
                    <div className="dashboard-owner-card__header">
                      <div>
                        <span className="dashboard-owner-card__rank">Top {index + 1}</span>
                        <h4 className="record-card__title">{item.ownerName}</h4>
                      </div>
                      <div className="dashboard-owner-card__gross">{formatCurrency(item.grossRevenue)}</div>
                    </div>
                    <div className="dashboard-owner-card__meter">
                      <span style={{ width: `${Math.max(sharePercent, 8)}%` }} />
                    </div>
                    <DetailList
                      items={[
                        { label: "Vehicles", value: String(item.vehicleCount) },
                        { label: "Partner share", value: formatCurrency(item.partnerShareAmount) },
                        { label: "Company share", value: formatCurrency(item.companyShareAmount) },
                        { label: "Partner ratio", value: `${sharePercent}%` },
                      ]}
                    />
                  </article>
                );
              })}
            </div>
          )}
        </Surface>

        <Surface
          eyebrow="Snapshot"
          title="Operational summary"
          description="A fast executive summary for current booking pressure and fleet utilization."
        >
          <div className="dashboard-summary-grid">
            <article className="dashboard-summary-card">
              <div className="dashboard-summary-card__label">Utilization</div>
              <div className="dashboard-summary-card__value">{utilization}%</div>
              <p className="dashboard-summary-card__text">
                Based on active rentals against available, on-rent, and workshop units currently represented in the API.
              </p>
            </article>
            <article className="dashboard-summary-card dashboard-summary-card--accent">
              <div className="dashboard-summary-card__label">Collections at risk</div>
              <div className="dashboard-summary-card__value">{summary.unpaidBookings + summary.overdueRentals}</div>
              <p className="dashboard-summary-card__text">
                Combined backlog from unpaid bookings and overdue rentals that may need manual intervention today.
              </p>
            </article>
            <article className="dashboard-summary-card">
              <div className="dashboard-summary-card__label">Ready inventory</div>
              <div className="dashboard-summary-card__value">
                <CarFront className="h-5 w-5" />
                {summary.availableVehicles}
              </div>
              <p className="dashboard-summary-card__text">
                Vehicles currently ready for assignment without counting units blocked by maintenance or active rental status.
              </p>
            </article>
          </div>
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

function MetricBar({
  label,
  value,
  total,
  tone,
}: {
  label: string;
  value: number;
  total: number;
  tone: "primary" | "secondary" | "muted";
}) {
  const width = total > 0 ? Math.max(Math.round((value / total) * 100), value > 0 ? 8 : 0) : 0;

  return (
    <div className="dashboard-metric-bar">
      <div className="dashboard-metric-bar__head">
        <span>{label}</span>
        <strong>{value}</strong>
      </div>
      <div className="dashboard-metric-bar__track">
        <span className={`dashboard-metric-bar__fill dashboard-metric-bar__fill--${tone}`} style={{ width: `${width}%` }} />
      </div>
    </div>
  );
}
