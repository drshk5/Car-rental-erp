import { fetchApi } from "@/lib/api";
import type { PagedResult } from "@/types/api";
import type { MaintenanceRecord } from "@/types/maintenance";

export function getMaintenanceRecords() {
  return fetchApi<PagedResult<MaintenanceRecord>>("/maintenance?page=1&pageSize=200");
}
