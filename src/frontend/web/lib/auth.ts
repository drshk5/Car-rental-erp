import { cookies } from "next/headers";
import { appConfig } from "@/lib/config";
import { decodeJwtPayload, isJwtExpired } from "@/lib/jwt";
import type { AuthResponse, UserSession } from "@/types/auth";

const ACCESS_COOKIE = "car_rental_access";
const REFRESH_COOKIE = "car_rental_refresh";
const REFRESH_TTL_DAYS = 14;

export async function getSession(): Promise<UserSession | null> {
  const token = await getAccessToken();
  if (!token) {
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

export async function getAccessToken() {
  const cookieStore = await cookies();
  const token = cookieStore.get(ACCESS_COOKIE)?.value;

  if (!token || isJwtExpired(token)) {
    return null;
  }

  return token;
}

export async function getRefreshToken() {
  const cookieStore = await cookies();
  return cookieStore.get(REFRESH_COOKIE)?.value ?? null;
}

export async function setAuthCookies(auth: AuthResponse) {
  const cookieStore = await cookies();
  const secure = appConfig.appUrl.startsWith("https://");

  cookieStore.set(ACCESS_COOKIE, auth.accessToken, {
    httpOnly: true,
    sameSite: "lax",
    secure,
    path: "/",
    expires: new Date(auth.expiresAtUtc),
  });

  cookieStore.set(REFRESH_COOKIE, auth.refreshToken, {
    httpOnly: true,
    sameSite: "lax",
    secure,
    path: "/",
    expires: new Date(Date.now() + REFRESH_TTL_DAYS * 24 * 60 * 60 * 1000),
  });
}

export async function clearAuthCookies() {
  const cookieStore = await cookies();
  cookieStore.delete(ACCESS_COOKIE);
  cookieStore.delete(REFRESH_COOKIE);
}

export const authCookieNames = {
  access: ACCESS_COOKIE,
  refresh: REFRESH_COOKIE,
};
