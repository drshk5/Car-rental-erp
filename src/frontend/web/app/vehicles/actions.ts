"use server";

import { revalidatePath } from "next/cache";
import { apiRequest } from "@/lib/api";
import type { ActionResult } from "@/lib/action-result";
import { toActionFailure } from "@/lib/action-result";

export type VehicleFormPayload = {
  branchId: string;
  ownerId: string;
  plateNumber: string;
  vin: string;
  brand: string;
  model: string;
  year: string;
  dailyRate: string;
  hourlyRate: string;
  kmRate: string;
  status: string;
};

function toBody(payload: VehicleFormPayload) {
  return {
    branchId: payload.branchId,
    ownerId: payload.ownerId,
    plateNumber: payload.plateNumber.trim(),
    vin: payload.vin.trim(),
    brand: payload.brand.trim(),
    model: payload.model.trim(),
    year: Number(payload.year || 0),
    dailyRate: Number(payload.dailyRate || 0),
    hourlyRate: Number(payload.hourlyRate || 0),
    kmRate: Number(payload.kmRate || 0),
    status: Number(payload.status),
  };
}

export async function createVehicleAction(payload: VehicleFormPayload): Promise<ActionResult> {
  try {
    await apiRequest("/vehicles", { method: "POST", body: JSON.stringify(toBody(payload)) });
    revalidatePath("/vehicles");
    return { ok: true, message: "Vehicle created." };
  } catch (error) {
    return toActionFailure(error, "Vehicle request failed.");
  }
}

export async function updateVehicleAction(id: string, payload: VehicleFormPayload): Promise<ActionResult> {
  try {
    await apiRequest(`/vehicles/${id}`, { method: "PUT", body: JSON.stringify(toBody(payload)) });
    revalidatePath("/vehicles");
    return { ok: true, message: "Vehicle updated." };
  } catch (error) {
    return toActionFailure(error, "Vehicle request failed.");
  }
}

export async function setVehicleStatusAction(id: string, status: string): Promise<ActionResult> {
  try {
    await apiRequest(`/vehicles/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ status: Number(status) }),
    });
    revalidatePath("/vehicles");
    return { ok: true, message: "Vehicle status updated." };
  } catch (error) {
    return toActionFailure(error, "Vehicle status request failed.");
  }
}
