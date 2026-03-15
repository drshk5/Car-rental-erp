"use client";

import type { ComponentType, ReactNode } from "react";
import { Monitor, MoonStar, Palette, Sparkles, SunMedium } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Sheet, SheetContent, SheetDescription, SheetHeader, SheetTitle, SheetTrigger } from "@/components/ui/sheet";
import { cn } from "@/lib/utils";
import {
  appearanceDensityOptions,
  appearanceModes,
  appearanceMotionOptions,
  appearanceRadiusOptions,
  appearanceSurfaceOptions,
  appearanceThemes,
  type AppearanceDensity,
  type AppearanceMode,
  type AppearanceMotion,
  type AppearanceRadius,
  type AppearanceSurface,
} from "@/lib/appearance";
import { useAppearance } from "@/components/theme/appearance-provider";

const modeIcons = {
  system: Monitor,
  light: SunMedium,
  dark: MoonStar,
} satisfies Record<AppearanceMode, typeof Monitor>;

export function AppearancePanel() {
  const { appearance, resolvedMode, updateAppearance, resetAppearance } = useAppearance();
  const activeTheme = appearanceThemes.find((theme) => theme.value === appearance.theme);

  return (
    <Sheet>
      <SheetTrigger asChild>
        <Button variant="outline" className="min-w-[12rem] justify-between border-border/70 bg-card text-foreground hover:bg-muted">
          <span className="inline-flex items-center gap-2">
            <Palette className="h-4 w-4" />
            Appearance
          </span>
          <span className="rounded-full border border-border/70 bg-muted px-2 py-0.5 text-[11px] uppercase tracking-[0.2em]">
            {activeTheme?.label ?? appearance.theme}
          </span>
        </Button>
      </SheetTrigger>

      <SheetContent
        side="right"
        className="w-full overflow-y-auto border-white/10 bg-[hsl(var(--background)/0.98)] px-0 sm:max-w-[30rem]"
      >
        <div className="flex min-h-full flex-col">
          <SheetHeader className="border-b border-border/60 px-6 pb-5">
            <SheetTitle className="flex items-center gap-2 text-2xl">
              <Sparkles className="h-5 w-5 text-primary" />
              Appearance
            </SheetTitle>
            <SheetDescription>
              Simple controls for color, shape, spacing, and overall style across the app.
            </SheetDescription>
          </SheetHeader>

          <div className="flex-1 space-y-6 px-6 py-6">
            <SimpleSection
              title="Color"
              description="Pick the color style you want everywhere."
            >
              <div className="grid gap-3 sm:grid-cols-2">
                {appearanceThemes.map((theme) => {
                  const selected = appearance.theme === theme.value;

                  return (
                    <button
                      key={theme.value}
                      type="button"
                      onClick={() => updateAppearance({ theme: theme.value })}
                      className={cn(
                        "rounded-[var(--radius-xl)] border p-4 text-left transition-all duration-200",
                        selected
                          ? "border-primary bg-primary/10 shadow-soft"
                          : "border-border/70 bg-card/80 hover:border-primary/35 hover:bg-card",
                      )}
                    >
                      <div className="flex items-center justify-between gap-3">
                        <div>
                          <div className="text-base font-semibold">{theme.label}</div>
                          <p className="mt-1 text-sm text-muted-foreground">{theme.description}</p>
                        </div>
                        <div className="flex gap-2">
                          {theme.swatches.map((swatch) => (
                            <span
                              key={swatch}
                              className="h-6 w-6 rounded-full border border-black/5"
                              style={{ background: swatch }}
                            />
                          ))}
                        </div>
                      </div>
                    </button>
                  );
                })}
              </div>
            </SimpleSection>

            <SimpleSection
              title="Mode"
              description={`Current result: ${resolvedMode} mode.`}
            >
              <ChoiceRow<AppearanceMode>
                value={appearance.mode}
                onChange={(value) => updateAppearance({ mode: value })}
                options={appearanceModes.map((option) => ({
                  ...option,
                  icon: modeIcons[option.value],
                }))}
              />
            </SimpleSection>

            <SimpleSection
              title="Shape"
              description="Choose how round or sharp the interface looks."
            >
              <ChoiceRow<AppearanceRadius>
                value={appearance.radius}
                onChange={(value) => updateAppearance({ radius: value })}
                options={appearanceRadiusOptions}
              />
            </SimpleSection>

            <SimpleSection
              title="Spacing"
              description="Choose relaxed or tighter spacing."
            >
              <ChoiceRow<AppearanceDensity>
                value={appearance.density}
                onChange={(value) => updateAppearance({ density: value })}
                options={appearanceDensityOptions}
              />
            </SimpleSection>

            <SimpleSection
              title="Surface"
              description="Choose soft glass panels or solid panels."
            >
              <ChoiceRow<AppearanceSurface>
                value={appearance.surface}
                onChange={(value) => updateAppearance({ surface: value })}
                options={appearanceSurfaceOptions}
              />
            </SimpleSection>

            <SimpleSection
              title="Motion"
              description="Choose full animation or reduced movement."
            >
              <ChoiceRow<AppearanceMotion>
                value={appearance.motion}
                onChange={(value) => updateAppearance({ motion: value })}
                options={appearanceMotionOptions}
              />
            </SimpleSection>
          </div>

          <div className="border-t border-border/60 px-6 py-4">
            <Button variant="ghost" onClick={resetAppearance} className="w-full">
              Reset appearance
            </Button>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  );
}

function SimpleSection({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <section className="space-y-3">
      <div className="space-y-1">
        <h3 className="text-sm font-semibold uppercase tracking-[0.16em] text-muted-foreground">{title}</h3>
        <p className="text-sm text-muted-foreground">{description}</p>
      </div>
      {children}
    </section>
  );
}

function ChoiceRow<T extends string>({
  value,
  onChange,
  options,
}: {
  value: T;
  onChange: (value: T) => void;
  options: ReadonlyArray<{ value: T; label: string; icon?: ComponentType<{ className?: string }> }>;
}) {
  return (
    <div className="grid gap-2 sm:grid-cols-3">
      {options.map((option) => {
        const selected = option.value === value;
        const Icon = option.icon;

        return (
          <button
            key={option.value}
            type="button"
            onClick={() => onChange(option.value)}
            className={cn(
              "inline-flex min-h-[var(--control-height)] items-center justify-center gap-2 rounded-[var(--radius-lg)] border px-4 text-sm font-semibold transition-all duration-200",
              selected
                ? "border-primary bg-primary text-primary-foreground shadow-soft"
                : "border-border/70 bg-card/75 text-foreground hover:border-primary/35 hover:bg-card",
            )}
          >
            {Icon ? <Icon className="h-4 w-4" /> : null}
            {option.label}
          </button>
        );
      })}
    </div>
  );
}
