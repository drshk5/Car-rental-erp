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
        <span className="text-[13px] font-bold tracking-[0.03em]">Work Email</span>
        <Input
          name="email"
          type="email"
          autoComplete="email"
          defaultValue="admin@carrental.local"
          placeholder="admin@carrental.local"
          required
        />
      </label>
      <label className="grid gap-2">
        <span className="text-[13px] font-bold tracking-[0.03em]">Password</span>
        <Input
          name="password"
          type="password"
          autoComplete="current-password"
          defaultValue="change-me"
          placeholder="Enter password"
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
        className="w-full btn-glow"
      >
        {pending ? "Signing in..." : "Sign in to console"}
      </Button>
    </form>
  );
}
