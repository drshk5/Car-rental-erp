import { fetchApi } from "@/lib/api";
import type { PagedResult } from "@/types/api";
import type { Customer, CustomerDetail, CustomerFilters } from "@/types/customers";

function toQueryString(filters: CustomerFilters = {}) {
  const params = new URLSearchParams();

  if (filters.search) {
    params.set("search", filters.search);
  }

  if (filters.verificationStatus) {
    params.set("verificationStatus", filters.verificationStatus);
  }

  if (typeof filters.isActive === "boolean") {
    params.set("isActive", String(filters.isActive));
  }

  if (typeof filters.hasActiveRental === "boolean") {
    params.set("hasActiveRental", String(filters.hasActiveRental));
  }

  if (typeof filters.hasOutstandingBalance === "boolean") {
    params.set("hasOutstandingBalance", String(filters.hasOutstandingBalance));
  }

  const query = params.toString();
  return query ? `?${query}` : "";
}

export function getCustomers(filters: CustomerFilters = {}) {
  const query = toQueryString(filters);
  const suffix = query ? `${query}&page=1&pageSize=200` : "?page=1&pageSize=200";
  return fetchApi<PagedResult<Customer>>(`/customers${suffix}`);
}

export function getCustomerDetail(id: string) {
  return fetchApi<CustomerDetail>(`/customers/${id}`);
}
