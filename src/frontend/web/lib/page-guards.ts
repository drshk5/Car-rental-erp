import { redirect } from "next/navigation";
import { ForbiddenApiError, UnauthorizedApiError } from "@/lib/api";

export function handleProtectedPageError(error: unknown, returnTo: string) {
  if (error instanceof UnauthorizedApiError) {
    redirect(error.shouldRefresh ? `/auth/refresh?returnTo=${encodeURIComponent(returnTo)}` : `/login?next=${encodeURIComponent(returnTo)}`);
  }

  if (error instanceof ForbiddenApiError) {
    return;
  }
}
