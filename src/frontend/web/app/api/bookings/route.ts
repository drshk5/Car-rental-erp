import { NextRequest } from "next/server";
import { proxyBookingRequest } from "@/app/api/bookings/_proxy";

export async function GET(request: NextRequest) {
  return proxyBookingRequest(request);
}
