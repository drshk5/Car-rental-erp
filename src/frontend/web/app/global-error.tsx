"use client";

import { useEffect } from "react";
import { Button } from "@/components/ui/button";
import "./globals.css";

export default function GlobalError({
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
    <html lang="en">
      <body>
        <main className="login-shell">
          <section className="login-shell__hero">
            <div className="login-shell__eyebrow">System Error</div>
            <div className="login-shell__content">
              <h1 className="login-shell__title">The app could not load.</h1>
              <p className="login-shell__description">
                A root-level error interrupted rendering. Retry once, then inspect the server log if it happens again.
              </p>
            </div>
          </section>

          <section className="login-shell__panel-wrap">
            <div className="login-shell__panel">
              <div>
                <div className="login-shell__panel-label">Global Error</div>
                <h2 className="login-shell__panel-title">Startup failure</h2>
                <p className="login-shell__panel-description">
                  {error.message || "Unknown startup error."}
                </p>
              </div>

              <div className="grid gap-3">
                <Button onClick={reset}>Try again</Button>
                <Button variant="outline" onClick={() => window.location.assign("/login")}>
                  Go to login
                </Button>
              </div>
            </div>
          </section>
        </main>
      </body>
    </html>
  );
}
