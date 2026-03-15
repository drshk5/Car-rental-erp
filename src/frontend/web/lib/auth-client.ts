"use client";

import { decodeJwtPayload, isJwtExpired } from "@/lib/jwt";
import type { UserSession } from "@/types/auth";

const ACCESS_COOKIE = "car_rental_access";

function getAccessTokenFromCookie(): string | null {
  if (typeof document === "undefined") return null;
  const match = document.cookie.match(new RegExp("(^| )" + ACCESS_COOKIE + "=([^;]+)"));
  if (match) return match[2];
  return null;
}

export async function getSession(): Promise<UserSession | null> {
  const token = getAccessTokenFromCookie();
  if (!token || isJwtExpired(token)) {
    return null;
  }

  const payload = decodeJwtPayload(token);
  if (!payload || typeof payload.sub !== "string" || typeof payload.email !== "string") {
    return null;
  }

  return {
    accessToken: token,
    userId: payload.sub,
    email: payload.email,
    fullName: typeof payload.name === "string" ? payload.name : typeof payload.unique_name === "string" ? payload.unique_name : "Authenticated User",
    role: typeof payload.role === "string" ? payload.role : "User",
    branchId: typeof payload.branch_id === "string" ? payload.branch_id : "",
    expiresAtUtc: typeof payload.exp === "number" ? new Date(payload.exp * 1000).toISOString() : null,
  };
}
