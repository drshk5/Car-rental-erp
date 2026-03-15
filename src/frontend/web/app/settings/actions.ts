"use server";

import { revalidatePath } from "next/cache";
import { apiRequest } from "@/lib/api";
import type { ActionResult } from "@/lib/action-result";
import { toActionFailure } from "@/lib/action-result";

export type BranchFormPayload = {
  name: string;
  city: string;
  address: string;
  phone: string;
};

export type RoleFormPayload = {
  name: string;
  roleType: string;
  permissions: string[];
};

export type UserFormPayload = {
  roleId: string;
  branchId: string;
  fullName: string;
  email: string;
  password: string;
};

function trimBranch(payload: BranchFormPayload) {
  return {
    name: payload.name.trim(),
    city: payload.city.trim(),
    address: payload.address.trim(),
    phone: payload.phone.trim(),
  };
}

function trimRole(payload: RoleFormPayload) {
  return {
    name: payload.name.trim(),
    roleType: Number(payload.roleType),
    permissions: payload.permissions,
  };
}

function trimUser(payload: UserFormPayload, includePassword: boolean) {
  return {
    roleId: payload.roleId,
    branchId: payload.branchId,
    fullName: payload.fullName.trim(),
    email: payload.email.trim(),
    ...(includePassword ? { password: payload.password } : {}),
  };
}

export async function createBranchAction(payload: BranchFormPayload): Promise<ActionResult> {
  try {
    await apiRequest("/branches", { method: "POST", body: JSON.stringify(trimBranch(payload)) });
    revalidatePath("/settings");
    return { ok: true, message: "Branch created." };
  } catch (error) {
    return toActionFailure(error, "Branch request failed.");
  }
}

export async function updateBranchAction(id: string, payload: BranchFormPayload): Promise<ActionResult> {
  try {
    await apiRequest(`/branches/${id}`, { method: "PUT", body: JSON.stringify(trimBranch(payload)) });
    revalidatePath("/settings");
    return { ok: true, message: "Branch updated." };
  } catch (error) {
    return toActionFailure(error, "Branch request failed.");
  }
}

export async function setBranchStatusAction(id: string, isActive: boolean): Promise<ActionResult> {
  try {
    await apiRequest(`/branches/${id}/status`, { method: "PATCH", body: JSON.stringify({ isActive }) });
    revalidatePath("/settings");
    return { ok: true, message: isActive ? "Branch activated." : "Branch deactivated." };
  } catch (error) {
    return toActionFailure(error, "Branch status update failed.");
  }
}

export async function createRoleAction(payload: RoleFormPayload): Promise<ActionResult> {
  try {
    await apiRequest("/roles", { method: "POST", body: JSON.stringify(trimRole(payload)) });
    revalidatePath("/settings");
    return { ok: true, message: "Role created." };
  } catch (error) {
    return toActionFailure(error, "Role request failed.");
  }
}

export async function updateRoleAction(id: string, payload: RoleFormPayload): Promise<ActionResult> {
  try {
    await apiRequest(`/roles/${id}`, { method: "PUT", body: JSON.stringify(trimRole(payload)) });
    revalidatePath("/settings");
    return { ok: true, message: "Role updated." };
  } catch (error) {
    return toActionFailure(error, "Role request failed.");
  }
}

export async function createUserAction(payload: UserFormPayload): Promise<ActionResult> {
  try {
    await apiRequest("/users", { method: "POST", body: JSON.stringify(trimUser(payload, true)) });
    revalidatePath("/settings");
    return { ok: true, message: "User created." };
  } catch (error) {
    return toActionFailure(error, "User request failed.");
  }
}

export async function updateUserAction(id: string, payload: UserFormPayload): Promise<ActionResult> {
  try {
    await apiRequest(`/users/${id}`, { method: "PUT", body: JSON.stringify(trimUser(payload, false)) });
    revalidatePath("/settings");
    return { ok: true, message: "User updated." };
  } catch (error) {
    return toActionFailure(error, "User request failed.");
  }
}

export async function setUserStatusAction(id: string, isActive: boolean): Promise<ActionResult> {
  try {
    await apiRequest(`/users/${id}/status`, { method: "PATCH", body: JSON.stringify({ isActive }) });
    revalidatePath("/settings");
    return { ok: true, message: isActive ? "User activated." : "User deactivated." };
  } catch (error) {
    return toActionFailure(error, "User status update failed.");
  }
}

export async function resetUserPasswordAction(id: string, password: string): Promise<ActionResult> {
  try {
    await apiRequest(`/users/${id}/reset-password`, { method: "POST", body: JSON.stringify({ password }) });
    revalidatePath("/settings");
    return { ok: true, message: "Password reset." };
  } catch (error) {
    return toActionFailure(error, "Password reset failed.");
  }
}
