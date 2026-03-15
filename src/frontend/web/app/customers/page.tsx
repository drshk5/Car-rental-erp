import { AppShell } from "@/components/layout/app-shell";
import { ErrorState, SectionIntro, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { CustomerWorkspace } from "@/app/customers/customer-workspace";
import { getCustomerDetail, getCustomers } from "@/lib/customers";
import { formatCurrency } from "@/lib/format";
import { handleProtectedPageError } from "@/lib/page-guards";

export default async function CustomersPage() {
  try {
    const customers = await getCustomers();
    const details = await Promise.all(customers.data.map((customer) => getCustomerDetail(customer.id)));
    const verified = customers.data.filter((customer) => String(customer.verificationStatus).toLowerCase() === "2" || String(customer.verificationStatus).toLowerCase() === "verified").length;
    const activeRentals = customers.data.filter((customer) => customer.hasActiveRental).length;
    const exposure = customers.data.reduce((sum, customer) => sum + customer.outstandingBalance, 0);

    return (
      <AppShell title="Customers" currentPath="/customers">
        <SectionIntro
          eyebrow="Customer Operations"
          title="Customer Intelligence Center"
          description="Profiles, verification, receivables, and rental activity now sit inside a more premium customer operations frame."
        />
        <StatGrid>
          <StatCard label="Profiles" value={String(customers.data.length)} note="Customer master data" />
          <StatCard label="Verified" value={String(verified)} note="KYC complete" tone="accent" />
          <StatCard label="Active rentals" value={String(activeRentals)} note="Current usage" />
          <StatCard label="Receivables" value={formatCurrency(exposure)} note="Outstanding balance" />
        </StatGrid>
        <Surface
          eyebrow="Workspace intent"
          title="Customer operations with deeper UI density"
          description="The customer workspace already carries richer interactions. This page layer now aligns it with the same dashboard-grade framing used across the rest of the product."
        />
        <CustomerWorkspace customers={customers.data} details={details} />
      </AppShell>
    );
  } catch (error) {
    handleProtectedPageError(error, "/customers");

    return (
      <AppShell title="Customers" currentPath="/customers">
        <ErrorState message="Customer data could not be loaded from the backend." error={error} />
      </AppShell>
    );
  }
}
