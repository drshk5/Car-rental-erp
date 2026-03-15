"use server";

import { revalidatePath } from "next/cache";
import { apiRequest } from "@/lib/api";
import type { CustomerFormPayload } from "@/types/customers";

export type CustomerActionResult =
  | { ok: true; message: string }
  | { ok: false; message: string };

function toCustomerBody(payload: CustomerFormPayload) {
  return {
    ...payload,
    dateOfBirth: payload.dateOfBirth || null,
    licenseExpiry: payload.licenseExpiry || null,
  };
}

function toFailure(error: unknown): CustomerActionResult {
  return {
    ok: false,
    message: error instanceof Error ? error.message : "Customer request failed.",
  };
}

function toVerificationValue(verificationStatus: string) {
  switch (verificationStatus) {
    case "Pending":
      return 1;
    case "Verified":
      return 2;
    case "Rejected":
      return 3;
    default:
      return verificationStatus;
  }
}

export async function createCustomerAction(payload: CustomerFormPayload): Promise<CustomerActionResult> {
  try {
    await apiRequest("/customers", {
      method: "POST",
      body: JSON.stringify(toCustomerBody(payload)),
    });
    revalidatePath("/customers");
    return { ok: true, message: "Customer created." };
  } catch (error) {
    return toFailure(error);
  }
}

export async function updateCustomerAction(id: string, payload: CustomerFormPayload): Promise<CustomerActionResult> {
  try {
    await apiRequest(`/customers/${id}`, {
      method: "PUT",
      body: JSON.stringify(toCustomerBody(payload)),
    });
    revalidatePath("/customers");
    return { ok: true, message: "Customer updated." };
  } catch (error) {
    return toFailure(error);
  }
}

export async function setCustomerVerificationAction(id: string, verificationStatus: string): Promise<CustomerActionResult> {
  try {
    await apiRequest(`/customers/${id}/verification`, {
      method: "PATCH",
      body: JSON.stringify({ verificationStatus: toVerificationValue(verificationStatus) }),
    });
    revalidatePath("/customers");
    return { ok: true, message: "Verification updated." };
  } catch (error) {
    return toFailure(error);
  }
}

export async function setCustomerStatusAction(id: string, isActive: boolean): Promise<CustomerActionResult> {
  try {
    await apiRequest(`/customers/${id}/status`, {
      method: "PATCH",
      body: JSON.stringify({ isActive }),
    });
    revalidatePath("/customers");
    return { ok: true, message: isActive ? "Customer reactivated." : "Customer archived." };
  } catch (error) {
    return toFailure(error);
  }
}
