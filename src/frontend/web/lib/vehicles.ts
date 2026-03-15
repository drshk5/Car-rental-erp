import { fetchApi } from "@/lib/api";
import type { PagedResult } from "@/types/api";
import type { Vehicle } from "@/types/vehicles";

export function getVehicles() {
  return fetchApi<PagedResult<Vehicle>>("/vehicles?page=1&pageSize=200");
}
