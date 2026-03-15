"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { cancelBookingAction, confirmBookingAction, createBookingAction, type BookingFormPayload } from "@/app/bookings/actions";
import { DetailList, EmptyState, RecordCard, RecordGrid, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { formatCurrency, formatDateTime } from "@/lib/format";
import type { ActionResult } from "@/lib/action-result";
import type { Booking } from "@/types/bookings";
import type { Branch } from "@/types/settings";
import type { Customer } from "@/types/customers";
import type { Vehicle } from "@/types/vehicles";

const pricingOptions = [
  { value: "1", label: "Daily" },
  { value: "2", label: "Hourly" },
];

export function BookingsWorkspace({
  bookings,
  customers,
  vehicles,
  branches,
}: {
  bookings: Booking[];
  customers: Customer[];
  vehicles: Vehicle[];
  branches: Branch[];
}) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [notice, setNotice] = useState<ActionResult | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState<BookingFormPayload>({
    customerId: customers[0]?.id ?? "",
    vehicleId: vehicles[0]?.id ?? "",
    pickupBranchId: branches[0]?.id ?? "",
    returnBranchId: branches[0]?.id ?? "",
    startAtUtc: "",
    endAtUtc: "",
    pricingPlan: "1",
    discountAmount: "0",
    depositAmount: "0",
  });
  const confirmed = bookings.filter((booking) => String(booking.status).toLowerCase() === "confirmed" || String(booking.status) === "2").length;

  function updateNotice(result: ActionResult) {
    setNotice(result);
    if (result.ok) {
      router.refresh();
    }
  }

  function handleCreate() {
    startTransition(async () => {
      const result = await createBookingAction(form);
      updateNotice(result);
      if (result.ok) {
        setCreateOpen(false);
      }
    });
  }

  function handleConfirm(id: string) {
    startTransition(async () => {
      updateNotice(await confirmBookingAction(id));
    });
  }

  function handleCancel(id: string) {
    startTransition(async () => {
      updateNotice(await cancelBookingAction(id));
    });
  }

  return (
    <div className="space-y-6">
      {notice ? <div className={`alert-banner ${notice.ok ? "alert-banner--success" : "alert-banner--error"}`}>{notice.message}</div> : null}
      <StatGrid>
        <StatCard label="Bookings" value={String(bookings.length)} />
        <StatCard label="Confirmed" value={String(confirmed)} tone="accent" />
      </StatGrid>
      <Surface title="Reservation list" description="Create bookings and run the backend confirm/cancel actions from the same workspace.">
        <div className="flex flex-wrap gap-3">
          <Button onClick={() => setCreateOpen(true)}>Create booking</Button>
        </div>
        {bookings.length === 0 ? (
          <EmptyState message="No bookings are available from the backend yet." />
        ) : (
          <RecordGrid>
            {bookings.map((booking) => (
              <RecordCard key={booking.id} title={booking.bookingNumber} subtitle={`${booking.customerName} · ${booking.vehicleLabel}`}>
                <DetailList
                  items={[
                    { label: "Pickup", value: `${booking.pickupBranchName} · ${formatDateTime(booking.startAtUtc)}` },
                    { label: "Return", value: `${booking.returnBranchName} · ${formatDateTime(booking.endAtUtc)}` },
                    { label: "Status", value: String(booking.status) },
                    { label: "Pricing plan", value: String(booking.pricingPlan) },
                    { label: "Quoted total", value: formatCurrency(booking.quotedTotal) },
                    { label: "Deposit", value: formatCurrency(booking.depositAmount) },
                  ]}
                />
                <div className="mt-4 flex flex-wrap gap-3">
                  <Button variant="outline" onClick={() => handleConfirm(booking.id)} disabled={pending}>
                    Confirm
                  </Button>
                  <Button variant="outline" onClick={() => handleCancel(booking.id)} disabled={pending}>
                    Cancel
                  </Button>
                </div>
              </RecordCard>
            ))}
          </RecordGrid>
        )}
      </Surface>
      <Dialog open={createOpen} onOpenChange={setCreateOpen}>
        <DialogContent className="max-w-2xl">
          <DialogHeader>
            <DialogTitle>Create booking</DialogTitle>
          </DialogHeader>
          <div className="grid gap-4 md:grid-cols-2">
            <SelectField label="Customer" value={form.customerId} onValueChange={(value) => setForm({ ...form, customerId: value })} options={customers.map((customer) => ({ value: customer.id, label: `${customer.fullName} · ${customer.customerCode}` }))} />
            <SelectField label="Vehicle" value={form.vehicleId} onValueChange={(value) => setForm({ ...form, vehicleId: value })} options={vehicles.map((vehicle) => ({ value: vehicle.id, label: `${vehicle.plateNumber} · ${vehicle.brand} ${vehicle.model}` }))} />
            <SelectField label="Pickup branch" value={form.pickupBranchId} onValueChange={(value) => setForm({ ...form, pickupBranchId: value })} options={branches.map((branch) => ({ value: branch.id, label: branch.name }))} />
            <SelectField label="Return branch" value={form.returnBranchId} onValueChange={(value) => setForm({ ...form, returnBranchId: value })} options={branches.map((branch) => ({ value: branch.id, label: branch.name }))} />
            <Field label="Start" type="datetime-local" value={form.startAtUtc} onChange={(value) => setForm({ ...form, startAtUtc: value })} />
            <Field label="End" type="datetime-local" value={form.endAtUtc} onChange={(value) => setForm({ ...form, endAtUtc: value })} />
            <SelectField label="Pricing plan" value={form.pricingPlan} onValueChange={(value) => setForm({ ...form, pricingPlan: value })} options={pricingOptions} />
            <Field label="Discount amount" type="number" value={form.discountAmount} onChange={(value) => setForm({ ...form, discountAmount: value })} />
            <Field label="Deposit amount" type="number" value={form.depositAmount} onChange={(value) => setForm({ ...form, depositAmount: value })} />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleCreate} disabled={pending}>
              Save
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function Field({ label, value, onChange, type = "text" }: { label: string; value: string; onChange: (value: string) => void; type?: string }) {
  return (
    <div className="grid gap-2">
      <Label>{label}</Label>
      <Input type={type} value={value} onChange={(event) => onChange(event.target.value)} />
    </div>
  );
}

function SelectField({
  label,
  value,
  onValueChange,
  options,
}: {
  label: string;
  value: string;
  onValueChange: (value: string) => void;
  options: Array<{ value: string; label: string }>;
}) {
  return (
    <div className="grid gap-2">
      <Label>{label}</Label>
      <Select value={value} onValueChange={onValueChange}>
        <SelectTrigger>
          <SelectValue placeholder={`Select ${label.toLowerCase()}`} />
        </SelectTrigger>
        <SelectContent>
          {options.map((option) => (
            <SelectItem key={option.value} value={option.value}>
              {option.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}
