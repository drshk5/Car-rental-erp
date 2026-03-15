"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { recordPaymentAction, refundPaymentAction, type PaymentFormPayload } from "@/app/payments/actions";
import { DetailList, EmptyState, RecordCard, RecordGrid, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { formatCurrency, formatDateTime } from "@/lib/format";
import type { ActionResult } from "@/lib/action-result";
import type { Booking } from "@/types/bookings";
import type { Payment } from "@/types/payments";

const methodOptions = [
  { value: "1", label: "Cash" },
  { value: "2", label: "UPI" },
  { value: "3", label: "Card" },
  { value: "4", label: "Bank transfer" },
];

export function PaymentsWorkspace({ payments, bookings }: { payments: Payment[]; bookings: Booking[] }) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [notice, setNotice] = useState<ActionResult | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState<PaymentFormPayload>({
    bookingId: bookings[0]?.id ?? "",
    amount: "0",
    paymentMethod: "1",
    referenceNumber: "",
    paidAtUtc: "",
    notes: "",
  });
  const refunded = payments.filter((payment) => String(payment.paymentStatus).toLowerCase() === "refunded" || String(payment.paymentStatus) === "3").length;
  const total = payments.reduce((sum, payment) => sum + payment.amount, 0);

  function updateNotice(result: ActionResult) {
    setNotice(result);
    if (result.ok) {
      router.refresh();
    }
  }

  function handleRecord() {
    startTransition(async () => {
      const result = await recordPaymentAction(form);
      updateNotice(result);
      if (result.ok) {
        setCreateOpen(false);
      }
    });
  }

  function handleRefund(id: string) {
    startTransition(async () => {
      updateNotice(await refundPaymentAction(id));
    });
  }

  return (
    <div className="space-y-6">
      {notice ? <div className={`alert-banner ${notice.ok ? "alert-banner--success" : "alert-banner--error"}`}>{notice.message}</div> : null}
      <StatGrid>
        <StatCard label="Payments" value={String(payments.length)} />
        <StatCard label="Collected amount" value={formatCurrency(total)} tone="accent" />
        <StatCard label="Refunded" value={String(refunded)} tone="warm" />
      </StatGrid>
      <Surface title="Payment ledger" description="Record new payments and trigger refunds without leaving the ledger.">
        <div className="flex flex-wrap gap-3">
          <Button onClick={() => setCreateOpen(true)}>Record payment</Button>
        </div>
        {payments.length === 0 ? (
          <EmptyState message="No payments are available from the backend yet." />
        ) : (
          <RecordGrid>
            {payments.map((payment) => (
              <RecordCard key={payment.id} title={payment.bookingNumber} subtitle={payment.referenceNumber || "No reference"}>
                <DetailList
                  items={[
                    { label: "Amount", value: formatCurrency(payment.amount) },
                    { label: "Method", value: String(payment.paymentMethod) },
                    { label: "Status", value: String(payment.paymentStatus) },
                    { label: "Paid at", value: formatDateTime(payment.paidAtUtc) },
                    { label: "Notes", value: payment.notes || "No notes" },
                  ]}
                />
                <div className="mt-4 flex flex-wrap gap-3">
                  <Button variant="outline" onClick={() => handleRefund(payment.id)} disabled={pending}>
                    Refund
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
            <DialogTitle>Record payment</DialogTitle>
          </DialogHeader>
          <div className="dialog-form-grid">
            <SelectField label="Booking" value={form.bookingId} onValueChange={(value) => setForm({ ...form, bookingId: value })} options={bookings.map((booking) => ({ value: booking.id, label: `${booking.bookingNumber} · ${booking.customerName}` }))} />
            <SelectField label="Method" value={form.paymentMethod} onValueChange={(value) => setForm({ ...form, paymentMethod: value })} options={methodOptions} />
            <Field label="Amount" type="number" value={form.amount} onChange={(value) => setForm({ ...form, amount: value })} />
            <Field label="Paid at" type="datetime-local" value={form.paidAtUtc} onChange={(value) => setForm({ ...form, paidAtUtc: value })} />
            <Field label="Reference number" value={form.referenceNumber} onChange={(value) => setForm({ ...form, referenceNumber: value })} />
            <div className="form-field form-field--full">
              <Label>Notes</Label>
              <Textarea value={form.notes} onChange={(event) => setForm({ ...form, notes: event.target.value })} />
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCreateOpen(false)}>
              Cancel
            </Button>
            <Button onClick={handleRecord} disabled={pending}>
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
    <div className="form-field">
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
    <div className="form-field">
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
