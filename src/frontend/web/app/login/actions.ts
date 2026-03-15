"use server";

import { redirect } from "next/navigation";
import { setAuthCookies } from "@/lib/auth";
import { appConfig } from "@/lib/config";
import type { ApiResponse } from "@/types/api";
import type { AuthResponse } from "@/types/auth";
import type { LoginFormState } from "@/app/login/state";

export async function loginAction(_previousState: LoginFormState, formData: FormData): Promise<LoginFormState> {
  const email = String(formData.get("email") ?? "").trim();
  const password = String(formData.get("password") ?? "");
  const next = String(formData.get("next") ?? "/dashboard");

  if (!email || !password) {
    return { error: "Email and password are required." };
  }

  const response = await fetch(`${appConfig.apiBaseUrl}/auth/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
    },
    body: JSON.stringify({ email, password }),
    cache: "no-store",
  });

  const payload = (await response.json()) as ApiResponse<AuthResponse>;
  if (!response.ok || !payload.success || !payload.data) {
    return { error: payload.message || "Sign-in failed." };
  }

  await setAuthCookies(payload.data);
  redirect(next.startsWith("/") ? next : "/dashboard");
}
