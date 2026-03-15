import Link from "next/link";

export default function NotFound() {
  return (
    <main
      style={{
        minHeight: "100vh",
        display: "grid",
        placeItems: "center",
        padding: "24px",
      }}
    >
      <section
        style={{
          width: "min(100%, 560px)",
          padding: "32px",
          borderRadius: "28px",
          border: "1px solid rgba(96, 72, 53, 0.14)",
          background: "rgba(255, 251, 245, 0.88)",
          boxShadow: "0 24px 60px rgba(82, 61, 42, 0.12)",
          textAlign: "center",
        }}
      >
        <div
          style={{
            fontSize: 12,
            letterSpacing: 2.4,
            textTransform: "uppercase",
            color: "var(--accent-2)",
          }}
        >
          Route Not Found
        </div>
        <h1 style={{ margin: "12px 0 10px", fontSize: "clamp(2.2rem, 5vw, 3.4rem)" }}>This page does not exist.</h1>
        <p style={{ margin: 0, color: "var(--muted)", lineHeight: 1.7 }}>
          The requested route is not part of the current Car Rental ERP console.
        </p>
        <div style={{ marginTop: 24, display: "flex", justifyContent: "center" }}>
          <Link
            href="/login"
            style={{
              display: "inline-flex",
              alignItems: "center",
              justifyContent: "center",
              minHeight: 48,
              padding: "0 18px",
              borderRadius: 16,
              background: "linear-gradient(135deg, #0f766e 0%, #155e75 100%)",
              color: "#fff",
            }}
          >
            Go to login
          </Link>
        </div>
      </section>
    </main>
  );
}
