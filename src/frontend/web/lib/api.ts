import { appConfig } from "@/lib/config";
import { getRefreshToken, getSession } from "@/lib/auth";
import type { ApiResponse } from "@/types/api";

export class UnauthorizedApiError extends Error {
  constructor(
    message: string,
    readonly shouldRefresh: boolean,
  ) {
    super(message);
  }
}

export class ForbiddenApiError extends Error {}

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const session = await getSession();
  const refreshToken = await getRefreshToken();

  const response = await fetch(`${appConfig.apiBaseUrl}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...(session?.accessToken ? { Authorization: `Bearer ${session.accessToken}` } : {}),
      ...(init?.headers ?? {}),
    },
    cache: "no-store",
  });

  if (response.status === 401) {
    throw new UnauthorizedApiError("Authentication is required.", Boolean(refreshToken));
  }

  let payload: ApiResponse<T> | null = null;
  if (response.headers.get("content-type")?.includes("application/json")) {
    payload = (await response.json()) as ApiResponse<T>;
  }

  if (response.status === 403) {
    throw new ForbiddenApiError(payload?.message || "You do not have permission to access this resource.");
  }

  if (!response.ok) {
    throw new Error(payload?.message || `API request failed with status ${response.status}`);
  }

  if (!payload || !payload.success || payload.data === null) {
    throw new Error(payload?.message || "API request returned no data");
  }

  return payload.data;
}

export async function fetchApi<T>(path: string, init?: RequestInit): Promise<T> {
  return apiRequest<T>(path, init);
}
