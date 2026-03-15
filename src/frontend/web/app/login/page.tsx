import { redirect } from "next/navigation";
import { LoginForm } from "@/app/login/login-form";
import { getSession } from "@/lib/auth";

export default async function LoginPage({
  searchParams,
}: {
  searchParams?: Promise<Record<string, string | string[] | undefined>>;
}) {
  const session = await getSession();
  if (session) {
    redirect("/dashboard");
  }

  const resolvedSearchParams = searchParams ? await searchParams : undefined;
  const nextValue = resolvedSearchParams?.next;
  const next = typeof nextValue === "string" && nextValue.startsWith("/") ? nextValue : "/dashboard";

  return (
    <main className="login-shell">
      <section className="login-shell__hero">
        <div className="login-shell__eyebrow">Fleet Operations Control</div>
        <div className="login-shell__content">
          <h1 className="login-shell__title">Run bookings, fleet, collections, and partner operations from one premium console.</h1>
          <p className="login-shell__description">
            The frontend now leans into a richer dashboard style: stronger hierarchy, better module framing, cleaner
            cards, and a more executive operations feel without losing the existing backend workflows.
          </p>
        </div>
        <div className="login-shell__spotlight">
          <div className="login-shell__spotlight-label">What changed</div>
          <div className="login-shell__spotlight-title">A dashboard-first interface with faster wayfinding and cleaner operational context.</div>
        </div>
        <div className="login-shell__stats">
          {[
            { label: "Protected routes", value: "JWT + refresh" },
            { label: "Module coverage", value: "9 core workspaces" },
            { label: "Session model", value: "HTTP-only cookies" },
          ].map((item) => (
            <article key={item.label} className="login-shell__stat">
              <div className="login-shell__stat-label">{item.label}</div>
              <div className="login-shell__stat-value">{item.value}</div>
            </article>
          ))}
        </div>
      </section>

      <section className="login-shell__panel-wrap">
        <div className="login-shell__panel">
          <div>
            <div className="login-shell__panel-label">Secure Sign-In</div>
            <h2 className="login-shell__panel-title">Operations Console</h2>
            <p className="login-shell__panel-description">
              Sign in with the seeded administrator account below or replace it with your existing user records.
            </p>
          </div>

          <LoginForm next={next} />

          <div className="login-shell__support">
            <div>
              Demo email: <strong>admin@carrental.local</strong>
            </div>
            <div>
              Demo password: <strong>change-me</strong>
            </div>
          </div>
        </div>
      </section>
    </main>
  );
}
