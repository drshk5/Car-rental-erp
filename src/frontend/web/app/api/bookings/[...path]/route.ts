import { NextRequest } from "next/server";
import { proxyBookingRequest } from "@/app/api/bookings/_proxy";

function getSuffix(path: string[] | undefined) {
  return path && path.length > 0 ? `/${path.join("/")}` : "";
}

export async function GET(
  request: NextRequest,
  context: { params: Promise<{ path?: string[] }> },
) {
  const { path } = await context.params;
  return proxyBookingRequest(request, getSuffix(path));
}
