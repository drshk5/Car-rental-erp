import { fetchApi } from "@/lib/api";
import type { PagedResult } from "@/types/api";
import type { Owner, OwnerRevenue } from "@/types/owners";

export function getOwners() {
  return fetchApi<PagedResult<Owner>>("/owners?page=1&pageSize=200");
}

export function getOwnerRevenue() {
  return fetchApi<OwnerRevenue[]>("/owners/revenue");
}
