"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { createVehicleAction, setVehicleStatusAction, updateVehicleAction, type VehicleFormPayload } from "@/app/vehicles/actions";
import { DetailList, EmptyState, RecordCard, RecordGrid, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { formatCurrency } from "@/lib/format";
import type { ActionResult } from "@/lib/action-result";
import type { Branch } from "@/types/settings";
import type { Owner } from "@/types/owners";
import type { Vehicle } from "@/types/vehicles";

const statusOptions = [
  { value: "1", label: "Available" },
  { value: "2", label: "Reserved" },
  { value: "3", label: "Active rental" },
  { value: "4", label: "Maintenance" },
  { value: "5", label: "Out of service" },
];

const emptyForm: VehicleFormPayload = {
  branchId: "",
  ownerId: "",
  plateNumber: "",
  vin: "",
  brand: "",
  model: "",
  year: "",
  dailyRate: "0",
  hourlyRate: "0",
  kmRate: "0",
  status: "1",
};

function toForm(vehicle?: Vehicle): VehicleFormPayload {
  if (!vehicle) {
    return emptyForm;
  }

  return {
    branchId: vehicle.branchId,
    ownerId: vehicle.ownerId,
    plateNumber: vehicle.plateNumber,
    vin: vehicle.vin,
    brand: vehicle.brand,
    model: vehicle.model,
    year: String(vehicle.year),
    dailyRate: String(vehicle.dailyRate),
    hourlyRate: String(vehicle.hourlyRate),
    kmRate: String(vehicle.kmRate),
    status: String(vehicle.status),
  };
}

export function VehiclesWorkspace({
  vehicles,
  owners,
  branches,
}: {
  vehicles: Vehicle[];
  owners: Owner[];
  branches: Branch[];
}) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [notice, setNotice] = useState<ActionResult | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [editingVehicle, setEditingVehicle] = useState<Vehicle | null>(null);
  const [createForm, setCreateForm] = useState<VehicleFormPayload>({
    ...emptyForm,
    branchId: branches[0]?.id ?? "",
    ownerId: owners[0]?.id ?? "",
  });
  const [editForm, setEditForm] = useState<VehicleFormPayload>(emptyForm);
  const available = vehicles.filter((vehicle) => String(vehicle.status) === "Available" || String(vehicle.status) === "1").length;

  function updateNotice(result: ActionResult) {
    setNotice(result);
    if (result.ok) {
      router.refresh();
    }
  }

  function handleCreate() {
    startTransition(async () => {
      const result = await createVehicleAction(createForm);
      updateNotice(result);
      if (result.ok) {
        setCreateOpen(false);
      }
    });
  }

  function handleEdit() {
    if (!editingVehicle) {
      return;
    }

    startTransition(async () => {
      const result = await updateVehicleAction(editingVehicle.id, editForm);
      updateNotice(result);
      if (result.ok) {
        setEditOpen(false);
      }
    });
  }

  function handleStatus(vehicle: Vehicle, status: string) {
    startTransition(async () => {
      updateNotice(await setVehicleStatusAction(vehicle.id, status));
    });
  }

  return (
    <div className="space-y-6">
      {notice ? <div className={`alert-banner ${notice.ok ? "alert-banner--success" : "alert-banner--error"}`}>{notice.message}</div> : null}
      <StatGrid>
        <StatCard label="Fleet size" value={String(vehicles.length)} />
        <StatCard label="Available now" value={String(available)} tone="accent" />
      </StatGrid>
      <Surface title="Fleet catalog" description="Create, edit, and change fleet status directly from the inventory page.">
        <div className="flex flex-wrap gap-3">
          <Button onClick={() => setCreateOpen(true)}>Create vehicle</Button>
        </div>
        {vehicles.length === 0 ? (
          <EmptyState message="No vehicles are available from the backend yet." />
        ) : (
          <RecordGrid>
            {vehicles.map((vehicle) => (
              <RecordCard key={vehicle.id} title={`${vehicle.plateNumber} · ${vehicle.brand} ${vehicle.model}`} subtitle={`VIN ${vehicle.vin}`}>
                <DetailList
                  items={[
                    { label: "Owner", value: vehicle.ownerName },
                    { label: "Branch", value: vehicle.branchName },
                    { label: "Year", value: String(vehicle.year) },
                    { label: "Daily rate", value: formatCurrency(vehicle.dailyRate) },
                    { label: "Hourly rate", value: formatCurrency(vehicle.hourlyRate) },
                    { label: "KM rate", value: formatCurrency(vehicle.kmRate) },
                    { label: "Status", value: String(vehicle.status) },
                  ]}
                />
                <div className="mt-4 flex flex-wrap gap-3">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setEditingVehicle(vehicle);
                      setEditForm(toForm(vehicle));
                      setEditOpen(true);
                    }}
                  >
                    Edit
                  </Button>
                  <Select defaultValue={String(vehicle.status)} onValueChange={(value) => handleStatus(vehicle, value)}>
                    <SelectTrigger className="w-[180px]">
                      <SelectValue placeholder="Change status" />
                    </SelectTrigger>
                    <SelectContent>
                      {statusOptions.map((option) => (
                        <SelectItem key={option.value} value={option.value}>
                          {option.label}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </RecordCard>
            ))}
          </RecordGrid>
        )}
      </Surface>
      <VehicleDialog
        open={createOpen}
        title="Create vehicle"
        form={createForm}
        owners={owners}
        branches={branches}
        onOpenChange={setCreateOpen}
        onChange={setCreateForm}
        onSubmit={handleCreate}
        pending={pending}
      />
      <VehicleDialog
        open={editOpen}
        title="Edit vehicle"
        form={editForm}
        owners={owners}
        branches={branches}
        onOpenChange={setEditOpen}
        onChange={setEditForm}
        onSubmit={handleEdit}
        pending={pending}
      />
    </div>
  );
}

function VehicleDialog({
  open,
  title,
  form,
  owners,
  branches,
  onOpenChange,
  onChange,
  onSubmit,
  pending,
}: {
  open: boolean;
  title: string;
  form: VehicleFormPayload;
  owners: Owner[];
  branches: Branch[];
  onOpenChange: (open: boolean) => void;
  onChange: (form: VehicleFormPayload) => void;
  onSubmit: () => void;
  pending: boolean;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <div className="dialog-form-grid">
          <SelectField label="Owner" value={form.ownerId} onValueChange={(value) => onChange({ ...form, ownerId: value })} options={owners.map((owner) => ({ value: owner.id, label: owner.displayName }))} />
          <SelectField label="Branch" value={form.branchId} onValueChange={(value) => onChange({ ...form, branchId: value })} options={branches.map((branch) => ({ value: branch.id, label: branch.name }))} />
          <Field label="Plate number" value={form.plateNumber} onChange={(value) => onChange({ ...form, plateNumber: value })} />
          <Field label="VIN" value={form.vin} onChange={(value) => onChange({ ...form, vin: value })} />
          <Field label="Brand" value={form.brand} onChange={(value) => onChange({ ...form, brand: value })} />
          <Field label="Model" value={form.model} onChange={(value) => onChange({ ...form, model: value })} />
          <Field label="Year" type="number" value={form.year} onChange={(value) => onChange({ ...form, year: value })} />
          <Field label="Daily rate" type="number" value={form.dailyRate} onChange={(value) => onChange({ ...form, dailyRate: value })} />
          <Field label="Hourly rate" type="number" value={form.hourlyRate} onChange={(value) => onChange({ ...form, hourlyRate: value })} />
          <Field label="KM rate" type="number" value={form.kmRate} onChange={(value) => onChange({ ...form, kmRate: value })} />
          <SelectField label="Status" value={form.status} onValueChange={(value) => onChange({ ...form, status: value })} options={statusOptions} />
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={onSubmit} disabled={pending}>
            Save
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
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
