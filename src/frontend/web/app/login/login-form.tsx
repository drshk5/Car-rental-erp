"use client";

import { useActionState } from "react";
import { loginAction } from "@/app/login/actions";
import { loginInitialState } from "@/app/login/state";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export function LoginForm({ next }: { next: string }) {
  const [state, formAction, pending] = useActionState(loginAction, loginInitialState);

  return (
    <form action={formAction} className="grid gap-[18px]">
      <input type="hidden" name="next" value={next} />
      <label className="grid gap-2">
        <span className="text-[13px] font-bold tracking-[0.03em] text-white">Work Email</span>
        <Input
          name="email"
          type="email"
          autoComplete="email"
          defaultValue="admin@carrental.local"
          placeholder="admin@carrental.local"
          className="border-white/10 bg-white/[0.03] text-white shadow-none placeholder:text-slate-400 focus-visible:border-primary/50 focus-visible:bg-white/[0.05] focus-visible:ring-primary/15"
          required
        />
      </label>
      <label className="grid gap-2">
        <span className="text-[13px] font-bold tracking-[0.03em] text-white">Password</span>
        <Input
          name="password"
          type="password"
          autoComplete="current-password"
          defaultValue="change-me"
          placeholder="Enter password"
          className="border-white/10 bg-white/[0.03] text-white shadow-none placeholder:text-slate-400 focus-visible:border-primary/50 focus-visible:bg-white/[0.05] focus-visible:ring-primary/15"
          required
        />
      </label>
      {state.error ? (
        <div className="rounded-[var(--radius-md)] border border-[hsl(var(--destructive)/0.24)] bg-[hsl(var(--error-soft))] px-4 py-3 text-sm text-[hsl(var(--error-soft-foreground))]">
          {state.error}
        </div>
      ) : null}
      <Button
        type="submit"
        disabled={pending}
        size="lg"
        className="w-full rounded-[18px]"
      >
        {pending ? "Signing in..." : "Sign in to console"}
      </Button>
    </form>
  );
}
