"use server";

import { revalidatePath } from "next/cache";
import { apiRequest } from "@/lib/api";
import type { ActionResult } from "@/lib/action-result";
import { toActionFailure } from "@/lib/action-result";

export type MaintenanceFormPayload = {
  vehicleId: string;
  serviceType: string;
  scheduledAtUtc: string;
  vendorName: string;
  cost: string;
  notes: string;
};

function toBody(payload: MaintenanceFormPayload) {
  return {
    vehicleId: payload.vehicleId,
    serviceType: payload.serviceType.trim(),
    scheduledAtUtc: payload.scheduledAtUtc,
    vendorName: payload.vendorName.trim(),
    cost: Number(payload.cost || 0),
    notes: payload.notes.trim(),
  };
}

export async function createMaintenanceAction(payload: MaintenanceFormPayload): Promise<ActionResult> {
  try {
    await apiRequest("/maintenance", { method: "POST", body: JSON.stringify(toBody(payload)) });
    revalidatePath("/maintenance");
    revalidatePath("/vehicles");
    return { ok: true, message: "Maintenance record created." };
  } catch (error) {
    return toActionFailure(error, "Maintenance request failed.");
  }
}

export async function completeMaintenanceAction(id: string): Promise<ActionResult> {
  try {
    await apiRequest(`/maintenance/${id}/complete`, { method: "PATCH" });
    revalidatePath("/maintenance");
    revalidatePath("/vehicles");
    return { ok: true, message: "Maintenance marked complete." };
  } catch (error) {
    return toActionFailure(error, "Maintenance completion failed.");
  }
}
