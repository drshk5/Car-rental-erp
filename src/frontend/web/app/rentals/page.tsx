import { AppShell } from "@/components/layout/app-shell";
import { SectionIntro } from "@/components/ui/console";
import { RentalWorkspace } from "@/app/rentals/rental-workspace";

export default function RentalsPage() {
  return (
    <AppShell title="Rentals" currentPath="/rentals">
      <SectionIntro
        eyebrow="Rental Operations"
        title="Rental control workspace"
        description="Operate the rental lifecycle with a real queue, checkout candidates, detailed timelines, and supported check-in actions."
      />
      <RentalWorkspace />
    </AppShell>
  );
}
