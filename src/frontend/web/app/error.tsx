"use client";

import { useEffect } from "react";
import { Button } from "@/components/ui/button";

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <main className="login-shell">
      <section className="login-shell__hero">
        <div className="login-shell__eyebrow">Application Error</div>
        <div className="login-shell__content">
          <h1 className="login-shell__title">Something went wrong.</h1>
          <p className="login-shell__description">
            The page failed to render. You can retry the current route or return to the main workspace.
          </p>
        </div>
      </section>

      <section className="login-shell__panel-wrap">
        <div className="login-shell__panel">
          <div>
            <div className="login-shell__panel-label">Error Details</div>
            <h2 className="login-shell__panel-title">Runtime failure</h2>
            <p className="login-shell__panel-description">
              {error.message || "An unexpected error occurred."}
            </p>
          </div>

          <div className="grid gap-3">
            <Button onClick={reset}>Try again</Button>
            <Button variant="outline" onClick={() => window.location.assign("/dashboard")}>
              Go to dashboard
            </Button>
          </div>
        </div>
      </section>
    </main>
  );
}
