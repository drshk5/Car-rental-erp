import { NextResponse } from "next/server";
import { clearAuthCookies } from "@/lib/auth";

export async function GET(request: Request) {
  return NextResponse.redirect(buildRedirectUrl(request, "/login"));
}

export async function POST(request: Request) {
  await clearAuthCookies();
  return NextResponse.redirect(buildRedirectUrl(request, "/login"));
}

function buildRedirectUrl(request: Request, path: string) {
  const requestUrl = new URL(request.url);
  const protocol = request.headers.get("x-forwarded-proto") ?? requestUrl.protocol.replace(":", "");
  const host = request.headers.get("x-forwarded-host") ?? request.headers.get("host") ?? requestUrl.host;

  return new URL(path, `${protocol}://${host}`);
}
