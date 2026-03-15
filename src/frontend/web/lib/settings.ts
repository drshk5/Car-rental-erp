import { fetchApi } from "@/lib/api";
import type { PagedResult } from "@/types/api";
import type { AuthUser } from "@/types/auth";
import type { Branch, PermissionCatalog, Role, User } from "@/types/settings";

export function getCurrentUserProfile() {
  return fetchApi<AuthUser>("/auth/me");
}

export function getUsers() {
  return fetchApi<PagedResult<User>>("/users?page=1&pageSize=200");
}

export function getRoles() {
  return fetchApi<Role[]>("/roles");
}

export function getPermissions() {
  return fetchApi<PermissionCatalog[]>("/roles/permissions");
}

export function getBranches() {
  return fetchApi<PagedResult<Branch>>("/branches?page=1&pageSize=200");
}
