import { NextRequest } from "next/server";
import { proxyRentalRequest } from "@/app/api/rentals/_proxy";

function getSuffix(path: string[] | undefined) {
  return path && path.length > 0 ? `/${path.join("/")}` : "";
}

export async function GET(
  request: NextRequest,
  context: { params: Promise<{ path?: string[] }> },
) {
  const { path } = await context.params;
  return proxyRentalRequest(request, getSuffix(path));
}

export async function POST(
  request: NextRequest,
  context: { params: Promise<{ path?: string[] }> },
) {
  const { path } = await context.params;
  return proxyRentalRequest(request, getSuffix(path));
}

export async function PATCH(
  request: NextRequest,
  context: { params: Promise<{ path?: string[] }> },
) {
  const { path } = await context.params;
  return proxyRentalRequest(request, getSuffix(path));
}
