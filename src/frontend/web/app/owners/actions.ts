"use server";

import { revalidatePath } from "next/cache";
import { apiRequest } from "@/lib/api";
import type { ActionResult } from "@/lib/action-result";
import { toActionFailure } from "@/lib/action-result";

export type OwnerFormPayload = {
  displayName: string;
  contactName: string;
  email: string;
  phone: string;
  revenueSharePercentage: string;
};

function toBody(payload: OwnerFormPayload) {
  return {
    displayName: payload.displayName.trim(),
    contactName: payload.contactName.trim(),
    email: payload.email.trim(),
    phone: payload.phone.trim(),
    revenueSharePercentage: Number(payload.revenueSharePercentage || 0),
  };
}

export async function createOwnerAction(payload: OwnerFormPayload): Promise<ActionResult> {
  try {
    await apiRequest("/owners", { method: "POST", body: JSON.stringify(toBody(payload)) });
    revalidatePath("/owners");
    return { ok: true, message: "Owner created." };
  } catch (error) {
    return toActionFailure(error, "Owner request failed.");
  }
}

export async function updateOwnerAction(id: string, payload: OwnerFormPayload): Promise<ActionResult> {
  try {
    await apiRequest(`/owners/${id}`, { method: "PUT", body: JSON.stringify(toBody(payload)) });
    revalidatePath("/owners");
    return { ok: true, message: "Owner updated." };
  } catch (error) {
    return toActionFailure(error, "Owner request failed.");
  }
}

export async function setOwnerStatusAction(id: string, isActive: boolean): Promise<ActionResult> {
  try {
    await apiRequest(`/owners/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ isActive }),
    });
    revalidatePath("/owners");
    return { ok: true, message: isActive ? "Owner activated." : "Owner deactivated." };
  } catch (error) {
    return toActionFailure(error, "Owner status request failed.");
  }
}
