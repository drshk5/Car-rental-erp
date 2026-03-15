import { fetchApi } from "@/lib/api";
import type { SystemHealth } from "@/types/system";

export function getSystemHealth() {
  return fetchApi<SystemHealth>("/system/health");
}
