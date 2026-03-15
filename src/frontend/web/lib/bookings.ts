import { fetchApi } from "@/lib/api";
import type { PagedResult } from "@/types/api";
import type { Booking } from "@/types/bookings";

export function getBookings() {
  return fetchApi<PagedResult<Booking>>("/bookings?page=1&pageSize=200");
}
