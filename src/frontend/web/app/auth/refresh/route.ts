import { NextResponse } from "next/server";
import { clearAuthCookies, getRefreshToken, setAuthCookies } from "@/lib/auth";
import { appConfig } from "@/lib/config";
import type { ApiResponse } from "@/types/api";
import type { AuthResponse } from "@/types/auth";

export async function GET(request: Request) {
  const url = new URL(request.url);
  const returnTo = sanitizeReturnTo(url.searchParams.get("returnTo"));
  const refreshToken = await getRefreshToken();

  if (!refreshToken) {
    const response = NextResponse.redirect(buildRedirectUrl(request, `/login?next=${encodeURIComponent(returnTo)}`));
    await clearAuthCookies();
    return response;
  }

  const refreshResponse = await fetch(`${appConfig.apiBaseUrl}/auth/refresh`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
    },
    body: JSON.stringify({ refreshToken }),
    cache: "no-store",
  });

  const payload = (await refreshResponse.json()) as ApiResponse<AuthResponse>;
  if (!refreshResponse.ok || !payload.success || !payload.data) {
    const response = NextResponse.redirect(buildRedirectUrl(request, `/login?next=${encodeURIComponent(returnTo)}`));
    await clearAuthCookies();
    return response;
  }

  await setAuthCookies(payload.data);
  return NextResponse.redirect(buildRedirectUrl(request, returnTo));
}

function sanitizeReturnTo(value: string | null) {
  return value && value.startsWith("/") ? value : "/dashboard";
}

function buildRedirectUrl(request: Request, path: string) {
  const requestUrl = new URL(request.url);
  const protocol = request.headers.get("x-forwarded-proto") ?? requestUrl.protocol.replace(":", "");
  const host = request.headers.get("x-forwarded-host") ?? request.headers.get("host") ?? requestUrl.host;

  return new URL(path, `${protocol}://${host}`);
}
