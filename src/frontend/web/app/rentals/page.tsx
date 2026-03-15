import { AppShell } from "@/components/layout/app-shell";
import { SectionIntro, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { RentalWorkspace } from "@/app/rentals/rental-workspace";

export default function RentalsPage() {
  return (
    <AppShell title="Rentals" currentPath="/rentals">
      <SectionIntro
        eyebrow="Rental Operations"
        title="Rental control workspace"
        description="Operate the rental lifecycle with stronger route framing, richer module hierarchy, and a more dashboard-native control surface."
      />
      <StatGrid>
        <StatCard label="Lifecycle control" value="Check-out" note="Dispatch and handover" tone="accent" />
        <StatCard label="Queue visibility" value="Live" note="Operational queue" />
        <StatCard label="Check-in support" value="Ready" note="Return workflow" />
        <StatCard label="Workspace model" value="Rich" note="Reference-inspired layout" />
      </StatGrid>
      <Surface
        eyebrow="Workspace intent"
        title="Rental lifecycle in a cleaner command layout"
        description="This route now inherits the same denser dashboard system as the rest of the app while keeping the existing rental queue behavior intact."
      />
      <RentalWorkspace />
    </AppShell>
  );
}
