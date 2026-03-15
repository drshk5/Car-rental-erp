"use client";

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

export async function fetchApi<T>(path: string, init?: RequestInit): Promise<T> {
  const url = path.startsWith("http")
    ? path
    : path.startsWith("/api/")
      ? path
      : `/api${path}`;

  const response = await fetch(url, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...(init?.headers ?? {}),
    },
    cache: "no-store",
  });

  if (response.status === 401) {
    if (typeof window !== "undefined") {
      const next = `${window.location.pathname}${window.location.search}`;
      window.location.href = `/auth/refresh?returnTo=${encodeURIComponent(next)}`;
    }
    throw new UnauthorizedApiError("Authentication is required.", false);
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

export async function apiRequestClient<T>(path: string, init?: RequestInit): Promise<T> {
  return fetchApi<T>(path, init);
}
