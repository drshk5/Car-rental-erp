"use server";

import { revalidatePath } from "next/cache";
import { apiRequest } from "@/lib/api";
import type { ActionResult } from "@/lib/action-result";
import { toActionFailure } from "@/lib/action-result";

export type BookingFormPayload = {
  customerId: string;
  vehicleId: string;
  pickupBranchId: string;
  returnBranchId: string;
  startAtUtc: string;
  endAtUtc: string;
  pricingPlan: string;
  discountAmount: string;
  depositAmount: string;
};

function toBody(payload: BookingFormPayload) {
  return {
    customerId: payload.customerId,
    vehicleId: payload.vehicleId,
    pickupBranchId: payload.pickupBranchId,
    returnBranchId: payload.returnBranchId,
    startAtUtc: payload.startAtUtc,
    endAtUtc: payload.endAtUtc,
    pricingPlan: Number(payload.pricingPlan),
    discountAmount: Number(payload.discountAmount || 0),
    depositAmount: Number(payload.depositAmount || 0),
  };
}

export async function createBookingAction(payload: BookingFormPayload): Promise<ActionResult> {
  try {
    await apiRequest("/bookings", { method: "POST", body: JSON.stringify(toBody(payload)) });
    revalidatePath("/bookings");
    revalidatePath("/rentals");
    return { ok: true, message: "Booking created." };
  } catch (error) {
    return toActionFailure(error, "Booking request failed.");
  }
}

export async function confirmBookingAction(id: string): Promise<ActionResult> {
  try {
    await apiRequest(`/bookings/${id}/confirm`, { method: "POST" });
    revalidatePath("/bookings");
    return { ok: true, message: "Booking confirmed." };
  } catch (error) {
    return toActionFailure(error, "Booking confirmation failed.");
  }
}

export async function cancelBookingAction(id: string): Promise<ActionResult> {
  try {
    await apiRequest(`/bookings/${id}/cancel`, { method: "POST" });
    revalidatePath("/bookings");
    revalidatePath("/rentals");
    return { ok: true, message: "Booking cancelled." };
  } catch (error) {
    return toActionFailure(error, "Booking cancellation failed.");
  }
}
