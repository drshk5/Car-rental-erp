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
          description="Manage customer profiles, verify identities, track rentals, and monitor receivables in one unified workspace."
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
