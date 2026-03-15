import type { ReactNode } from "react";

export function SectionIntro({
  eyebrow,
  title,
  description,
}: {
  eyebrow?: string;
  title: string;
  description?: string;
}) {
  return (
    <div className="section-intro">
      {eyebrow ? <div className="section-intro__eyebrow">{eyebrow}</div> : null}
      <h3 className="section-intro__title">{title}</h3>
      {description ? <p className="section-intro__description">{description}</p> : null}
    </div>
  );
}

export function StatGrid({ children }: { children: ReactNode }) {
  return <div className="stat-grid">{children}</div>;
}

export function StatCard({
  label,
  value,
  note,
  tone = "default",
}: {
  label: string;
  value: string;
  note?: string;
  tone?: "default" | "accent" | "warm";
}) {
  return (
    <article className={`stat-card stat-card--${tone}`}>
      <div className="stat-card__label">{label}</div>
      <div className="stat-card__value">{value}</div>
      {note ? <div className="stat-card__note">{note}</div> : null}
    </article>
  );
}

export function Surface({
  title,
  eyebrow,
  description,
  children,
}: {
  title: string;
  eyebrow?: string;
  description?: string;
  children: ReactNode;
}) {
  return (
    <section className="surface-card">
      <SectionIntro eyebrow={eyebrow} title={title} description={description} />
      {children}
    </section>
  );
}

export function RecordGrid({ children }: { children: ReactNode }) {
  return <div className="record-grid">{children}</div>;
}

export function RecordCard({
  title,
  subtitle,
  children,
}: {
  title: string;
  subtitle?: string;
  children: ReactNode;
}) {
  return (
    <article className="record-card">
      <div className="record-card__header">
        <div>
          <h4 className="record-card__title">{title}</h4>
          {subtitle ? <p className="record-card__subtitle">{subtitle}</p> : null}
        </div>
      </div>
      <div className="record-card__body">{children}</div>
    </article>
  );
}

export function DetailList({
  items,
}: {
  items: ReadonlyArray<{ label: string; value: string }>;
}) {
  return (
    <dl className="detail-list">
      {items.map((item) => (
        <div key={item.label} className="detail-list__item">
          <dt>{item.label}</dt>
          <dd>{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}

export function EmptyState({ message }: { message: string }) {
  return <div className="empty-state">{message}</div>;
}

export function ErrorState({ message, error }: { message: string; error: unknown }) {
  return (
    <div className="error-state" role="alert">
      <div>{message}</div>
      <div className="error-state__detail">{error instanceof Error ? error.message : "Unknown error"}</div>
    </div>
  );
}
