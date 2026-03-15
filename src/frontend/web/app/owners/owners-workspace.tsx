"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { createOwnerAction, setOwnerStatusAction, updateOwnerAction, type OwnerFormPayload } from "@/app/owners/actions";
import { DetailList, EmptyState, RecordCard, RecordGrid, StatCard, StatGrid, Surface } from "@/components/ui/console";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { formatCurrency, formatPercent } from "@/lib/format";
import type { ActionResult } from "@/lib/action-result";
import type { Owner, OwnerRevenue } from "@/types/owners";

const emptyForm: OwnerFormPayload = {
  displayName: "",
  contactName: "",
  email: "",
  phone: "",
  revenueSharePercentage: "0",
};

function toForm(owner?: Owner): OwnerFormPayload {
  if (!owner) {
    return emptyForm;
  }

  return {
    displayName: owner.displayName,
    contactName: owner.contactName,
    email: owner.email,
    phone: owner.phone,
    revenueSharePercentage: String(owner.revenueSharePercentage),
  };
}

export function OwnersWorkspace({ owners, revenue }: { owners: Owner[]; revenue: OwnerRevenue[] }) {
  const router = useRouter();
  const [pending, startTransition] = useTransition();
  const [notice, setNotice] = useState<ActionResult | null>(null);
  const [createOpen, setCreateOpen] = useState(false);
  const [editOpen, setEditOpen] = useState(false);
  const [editingOwner, setEditingOwner] = useState<Owner | null>(null);
  const [createForm, setCreateForm] = useState<OwnerFormPayload>(emptyForm);
  const [editForm, setEditForm] = useState<OwnerFormPayload>(emptyForm);
  const revenueByOwnerId = new Map(revenue.map((entry) => [entry.ownerId, entry]));
  const activeOwners = owners.filter((owner) => owner.isActive).length;
  const grossRevenue = revenue.reduce((sum, entry) => sum + entry.grossRevenue, 0);

  function refreshWithNotice(result: ActionResult) {
    setNotice(result);
    if (result.ok) {
      router.refresh();
    }
  }

  function handleCreate() {
    startTransition(async () => {
      const result = await createOwnerAction(createForm);
      refreshWithNotice(result);
      if (result.ok) {
        setCreateOpen(false);
        setCreateForm(emptyForm);
      }
    });
  }

  function handleEdit() {
    if (!editingOwner) {
      return;
    }

    startTransition(async () => {
      const result = await updateOwnerAction(editingOwner.id, editForm);
      refreshWithNotice(result);
      if (result.ok) {
        setEditOpen(false);
        setEditingOwner(null);
      }
    });
  }

  function handleStatus(owner: Owner) {
    startTransition(async () => {
      refreshWithNotice(await setOwnerStatusAction(owner.id, !owner.isActive));
    });
  }

  return (
    <div className="space-y-6">
      {notice ? (
        <div className={`alert-banner ${notice.ok ? "alert-banner--success" : "alert-banner--error"}`}>{notice.message}</div>
      ) : null}

      <StatGrid>
        <StatCard label="Total owners" value={String(owners.length)} />
        <StatCard label="Active owners" value={String(activeOwners)} tone="accent" />
        <StatCard label="Gross owner revenue" value={formatCurrency(grossRevenue)} tone="warm" />
      </StatGrid>

      <Surface title="Partner directory" description="Create, update, and maintain partner records without leaving the page.">
        <div className="flex flex-wrap gap-3">
          <Button onClick={() => setCreateOpen(true)}>Create owner</Button>
        </div>

        {owners.length === 0 ? (
          <EmptyState message="No owners are available from the backend yet." />
        ) : (
          <RecordGrid>
            {owners.map((owner) => {
              const summary = revenueByOwnerId.get(owner.id);

              return (
                <RecordCard key={owner.id} title={owner.displayName} subtitle={`Contact: ${owner.contactName}`}>
                  <DetailList
                    items={[
                      { label: "Email", value: owner.email },
                      { label: "Phone", value: owner.phone },
                      { label: "Partner share", value: formatPercent(owner.revenueSharePercentage) },
                      { label: "Status", value: owner.isActive ? "Active" : "Inactive" },
                      { label: "Gross revenue", value: formatCurrency(summary?.grossRevenue ?? 0) },
                      { label: "Company share", value: formatCurrency(summary?.companyShareAmount ?? 0) },
                    ]}
                  />
                  <div className="mt-4 flex flex-wrap gap-3">
                    <Button
                      variant="outline"
                      onClick={() => {
                        setEditingOwner(owner);
                        setEditForm(toForm(owner));
                        setEditOpen(true);
                      }}
                    >
                      Edit
                    </Button>
                    <Button variant="outline" onClick={() => handleStatus(owner)} disabled={pending}>
                      {owner.isActive ? "Deactivate" : "Activate"}
                    </Button>
                  </div>
                </RecordCard>
              );
            })}
          </RecordGrid>
        )}
      </Surface>

      <OwnerDialog
        open={createOpen}
        title="Create owner"
        form={createForm}
        onOpenChange={setCreateOpen}
        onChange={setCreateForm}
        onSubmit={handleCreate}
        pending={pending}
      />
      <OwnerDialog
        open={editOpen}
        title="Edit owner"
        form={editForm}
        onOpenChange={setEditOpen}
        onChange={setEditForm}
        onSubmit={handleEdit}
        pending={pending}
      />
    </div>
  );
}

function OwnerDialog({
  open,
  title,
  form,
  onOpenChange,
  onChange,
  onSubmit,
  pending,
}: {
  open: boolean;
  title: string;
  form: OwnerFormPayload;
  onOpenChange: (open: boolean) => void;
  onChange: (form: OwnerFormPayload) => void;
  onSubmit: () => void;
  pending: boolean;
}) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
        </DialogHeader>
        <div className="grid gap-4">
          <Field label="Business name" value={form.displayName} onChange={(value) => onChange({ ...form, displayName: value })} />
          <Field label="Contact name" value={form.contactName} onChange={(value) => onChange({ ...form, contactName: value })} />
          <Field label="Email" type="email" value={form.email} onChange={(value) => onChange({ ...form, email: value })} />
          <Field label="Phone" value={form.phone} onChange={(value) => onChange({ ...form, phone: value })} />
          <Field
            label="Revenue share %"
            type="number"
            value={form.revenueSharePercentage}
            onChange={(value) => onChange({ ...form, revenueSharePercentage: value })}
          />
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

function Field({
  label,
  value,
  onChange,
  type = "text",
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
}) {
  return (
    <div className="grid gap-2">
      <Label>{label}</Label>
      <Input type={type} value={value} onChange={(event) => onChange(event.target.value)} />
    </div>
  );
}
