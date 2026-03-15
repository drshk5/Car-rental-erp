import { NextRequest, NextResponse } from "next/server";
import { getAccessToken } from "@/lib/auth";
import { appConfig } from "@/lib/config";

export async function proxyRentalRequest(request: NextRequest, suffix = "") {
  const accessToken = await getAccessToken();
  if (!accessToken) {
    return NextResponse.json(
      { success: false, message: "Authentication is required.", data: null },
      { status: 401 },
    );
  }

  const target = new URL(`${appConfig.apiBaseUrl}/rentals${suffix}`);
  target.search = request.nextUrl.search;

  const contentType = request.headers.get("content-type");
  const response = await fetch(target, {
    method: request.method,
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${accessToken}`,
      ...(contentType ? { "Content-Type": contentType } : {}),
    },
    body: request.method === "GET" || request.method === "HEAD" ? undefined : await request.text(),
    cache: "no-store",
  });

  const responseContentType = response.headers.get("content-type");
  return new NextResponse(await response.text(), {
    status: response.status,
    headers: responseContentType ? { "content-type": responseContentType } : undefined,
  });
}
