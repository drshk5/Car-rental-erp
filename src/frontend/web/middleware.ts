import type { NextRequest } from "next/server";
import { NextResponse } from "next/server";
import { decodeJwtPayload } from "@/lib/jwt";

const ACCESS_COOKIE = "car_rental_access";
const REFRESH_COOKIE = "car_rental_refresh";

export function middleware(request: NextRequest) {
  const { pathname, search } = request.nextUrl;

  if (pathname.startsWith("/auth/refresh") || pathname.startsWith("/auth/logout")) {
    return NextResponse.next();
  }

  const accessToken = request.cookies.get(ACCESS_COOKIE)?.value;
  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;
  const isAccessValid = isTokenValid(accessToken);

  if (pathname === "/login") {
    if (isAccessValid) {
      return NextResponse.redirect(new URL("/dashboard", request.url));
    }

    return NextResponse.next();
  }

  if (!isAccessValid) {
    if (refreshToken) {
      return NextResponse.redirect(
        new URL(`/auth/refresh?returnTo=${encodeURIComponent(pathname + search)}`, request.url),
      );
    }

    return NextResponse.redirect(new URL(`/login?next=${encodeURIComponent(pathname + search)}`, request.url));
  }

  return NextResponse.next();
}

function isTokenValid(token: string | undefined) {
  if (!token) {
    return false;
  }

  const payload = decodeJwtPayload(token);
  return typeof payload?.exp === "number" && payload.exp * 1000 > Date.now();
}

export const config = {
  matcher: [
    "/((?!api|_next/static|_next/image|favicon.ico).*)",
  ],
};
