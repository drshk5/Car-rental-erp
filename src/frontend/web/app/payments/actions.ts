"use server";

import { revalidatePath } from "next/cache";
import { apiRequest } from "@/lib/api";
import type { ActionResult } from "@/lib/action-result";
import { toActionFailure } from "@/lib/action-result";

export type PaymentFormPayload = {
  bookingId: string;
  amount: string;
  paymentMethod: string;
  referenceNumber: string;
  paidAtUtc: string;
  notes: string;
};

function toBody(payload: PaymentFormPayload) {
  return {
    bookingId: payload.bookingId,
    amount: Number(payload.amount || 0),
    paymentMethod: Number(payload.paymentMethod),
    referenceNumber: payload.referenceNumber.trim(),
    paidAtUtc: payload.paidAtUtc || null,
    notes: payload.notes.trim(),
  };
}

export async function recordPaymentAction(payload: PaymentFormPayload): Promise<ActionResult> {
  try {
    await apiRequest("/payments", { method: "POST", body: JSON.stringify(toBody(payload)) });
    revalidatePath("/payments");
    revalidatePath("/bookings");
    return { ok: true, message: "Payment recorded." };
  } catch (error) {
    return toActionFailure(error, "Payment request failed.");
  }
}

export async function refundPaymentAction(id: string): Promise<ActionResult> {
  try {
    await apiRequest(`/payments/${id}/refund`, { method: "POST" });
    revalidatePath("/payments");
    revalidatePath("/bookings");
    return { ok: true, message: "Payment refunded." };
  } catch (error) {
    return toActionFailure(error, "Payment refund failed.");
  }
}
