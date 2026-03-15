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
          <h1 className="login-shell__title">A calmer, more professional control surface for fleet and revenue operations.</h1>
          <p className="login-shell__description">
            Reservations, rentals, maintenance, settlements, and administration now sit inside a more refined dashboard
            system with tighter alignment, cleaner spacing, and better visual discipline.
          </p>
        </div>
        <div className="login-shell__spotlight">
          <div className="login-shell__spotlight-label">What changed</div>
          <div className="login-shell__spotlight-title">Sharper layout rhythm, cleaner action hierarchy, and richer admin-grade presentation across every workspace.</div>
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
