import type { NextPageContext } from "next";
import Link from "next/link";

type ErrorPageProps = {
  statusCode?: number;
};

function ErrorPage({ statusCode }: ErrorPageProps) {
  return (
    <main className="login-shell">
      <section className="login-shell__hero">
        <div className="login-shell__eyebrow">Request Error</div>
        <div className="login-shell__content">
          <h1 className="login-shell__title">Something failed to load.</h1>
          <p className="login-shell__description">
            {statusCode ? `The server returned ${statusCode}.` : "An unexpected client error occurred."}
          </p>
        </div>
      </section>

      <section className="login-shell__panel-wrap">
        <div className="login-shell__panel">
          <div>
            <div className="login-shell__panel-label">Fallback Error Page</div>
            <h2 className="login-shell__panel-title">Next.js compatibility</h2>
            <p className="login-shell__panel-description">
              This page exists so development mode always has `_app` and `_error` fallbacks available.
            </p>
          </div>

          <Link href="/login" className="inline-flex min-h-[var(--control-height)] items-center justify-center rounded-[var(--radius-lg)] bg-primary px-5 text-sm font-semibold text-primary-foreground shadow-soft">
            Return to login
          </Link>
        </div>
      </section>
    </main>
  );
}

ErrorPage.getInitialProps = ({ res, err }: NextPageContext) => {
  const statusCode = res?.statusCode ?? err?.statusCode ?? 404;
  return { statusCode };
};

export default ErrorPage;
