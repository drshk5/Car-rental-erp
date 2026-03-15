import { NextRequest } from "next/server";
import { proxyRentalRequest } from "@/app/api/rentals/_proxy";

export async function GET(request: NextRequest) {
  return proxyRentalRequest(request);
}

export async function POST(request: NextRequest) {
  return proxyRentalRequest(request);
}
