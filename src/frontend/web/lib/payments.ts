import { fetchApi } from "@/lib/api";
import type { PagedResult } from "@/types/api";
import type { Payment } from "@/types/payments";

export function getPayments() {
  return fetchApi<PagedResult<Payment>>("/payments?page=1&pageSize=200");
}
