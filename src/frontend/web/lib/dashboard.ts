import { fetchApi } from "@/lib/api";
import type { DashboardSummary } from "@/types/dashboard";

export function getDashboardSummary() {
  return fetchApi<DashboardSummary>("/dashboard/summary");
}
