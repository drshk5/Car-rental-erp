"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { completeMaintenanceAction, createMaintenanceAction, type MaintenanceFormPayload } from "@/app/maintenance/actions";
import { DetailList, EmptyState, RecordCard, RecordGrid, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import { formatCurrency, formatDateTime } from "@/lib/format";
import type { ActionResult } from "@/lib/action-result";
import type { MaintenanceRecord } from "@/types/maintenance";
import type { Vehicle } from "@/types/vehicles";

export function MaintenanceWorkspace({ records, vehicles }: { records: MaintenanceRecord[]; vehicles: Vehicle[] }) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [notice, setNotice] = useState<ActionResult | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [form, setForm] = useState<MaintenanceFormPayload>({
    vehicleId: vehicles[0]?.id ?? "",
    serviceType: "",
    scheduledAtUtc: "",
    vendorName: "",
    cost: "0",
    notes: "",
  });
  const openCount = records.filter((record) => String(record.status).toLowerCase() !== "completed" && String(record.status) !== "3").length;
  const totalCost = records.reduce((sum, record) => sum + record.cost, 0);

  function updateNotice(result: ActionResult) {
    setNotice(result);
    if (result.ok) {
      router.refresh();
    }
  }

  function handleCreate() {
    startTransition(async () => {
      const result = await createMaintenanceAction(form);
      updateNotice(result);
      if (result.ok) {
        setCreateOpen(false);
      }
    });
  }

  function handleComplete(id: string) {
    startTransition(async () => {
      updateNotice(await completeMaintenanceAction(id));
    });
  }

  return (
    <div className="space-y-6">
      {notice ? <div className={`alert-banner ${notice.ok ? "alert-banner--success" : "alert-banner--error"}`}>{notice.message}</div> : null}
      <StatGrid>
        <StatCard label="Work orders" value={String(records.length)} />
        <StatCard label="Open items" value={String(openCount)} tone="warm" />
        <StatCard label="Recorded cost" value={formatCurrency(totalCost)} tone="accent" />
      </StatGrid>
      <Surface title="Maintenance records" description="Schedule work orders and complete them from the same fleet-care view.">
        <div className="flex flex-wrap gap-3">
          <Button onClick={() => setCreateOpen(true)}>Schedule maintenance</Button>
        </div>
        {records.length === 0 ? (
          <EmptyState message="No maintenance records are available from the backend yet." />
        ) : (
          <RecordGrid>
            {records.map((record) => (
              <RecordCard key={record.id} title={record.vehicleLabel} subtitle={record.serviceType}>
                <DetailList
                  items={[
                    { label: "Scheduled", value: formatDateTime(record.scheduledAtUtc) },
                    { label: "Completed", value: record.completedAtUtc ? formatDateTime(record.completedAtUtc) : "Pending" },
                    { label: "Vendor", value: record.vendorName },
                    { label: "Cost", value: formatCurrency(record.cost) },
                    { label: "Status", value: String(record.status) },
                    { label: "Notes", value: record.notes || "No notes" },
                  ]}
                />
                <div className="mt-4 flex flex-wrap gap-3">
                  <Button variant="outline" onClick={() => handleComplete(record.id)} disabled={pending}>
                    Complete
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
            <DialogTitle>Schedule maintenance</DialogTitle>
          </DialogHeader>
          <div className="grid gap-4 md:grid-cols-2">
            <SelectField label="Vehicle" value={form.vehicleId} onValueChange={(value) => setForm({ ...form, vehicleId: value })} options={vehicles.map((vehicle) => ({ value: vehicle.id, label: `${vehicle.plateNumber} · ${vehicle.brand} ${vehicle.model}` }))} />
            <Field label="Service type" value={form.serviceType} onChange={(value) => setForm({ ...form, serviceType: value })} />
            <Field label="Scheduled at" type="datetime-local" value={form.scheduledAtUtc} onChange={(value) => setForm({ ...form, scheduledAtUtc: value })} />
            <Field label="Vendor name" value={form.vendorName} onChange={(value) => setForm({ ...form, vendorName: value })} />
            <Field label="Cost" type="number" value={form.cost} onChange={(value) => setForm({ ...form, cost: value })} />
            <div className="grid gap-2 md:col-span-2">
              <Label>Notes</Label>
              <Textarea value={form.notes} onChange={(event) => setForm({ ...form, notes: event.target.value })} />
            </div>
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
