export const appearanceStorageKey = "car-rental-appearance";

export type AppearanceMode = "system" | "light" | "dark";
export type AppearanceTheme = "oasis" | "executive" | "solar" | "graphite";
export type AppearanceRadius = "soft" | "rounded" | "sharp";
export type AppearanceDensity = "comfortable" | "compact";
export type AppearanceSurface = "glass" | "solid";
export type AppearanceMotion = "full" | "reduced";

export type AppearanceSettings = {
  mode: AppearanceMode;
  theme: AppearanceTheme;
  radius: AppearanceRadius;
  density: AppearanceDensity;
  surface: AppearanceSurface;
  motion: AppearanceMotion;
};

export const defaultAppearance: AppearanceSettings = {
  mode: "system",
  theme: "oasis",
  radius: "rounded",
  density: "comfortable",
  surface: "glass",
  motion: "full",
};

export const appearanceThemes: ReadonlyArray<{
  value: AppearanceTheme;
  label: string;
  description: string;
  swatches: readonly [string, string, string];
}> = [
  {
    value: "oasis",
    label: "Oasis",
    description: "Teal and brass with warm console surfaces.",
    swatches: ["#0f766e", "#a16207", "#f7efe3"],
  },
  {
    value: "executive",
    label: "Executive",
    description: "Deep sapphire with platinum accents.",
    swatches: ["#1d4ed8", "#475569", "#eff6ff"],
  },
  {
    value: "solar",
    label: "Solar",
    description: "Copper and ember tones with energetic contrast.",
    swatches: ["#c2410c", "#7c2d12", "#fff7ed"],
  },
  {
    value: "graphite",
    label: "Graphite",
    description: "Neutral monochrome with electric cyan highlights.",
    swatches: ["#0891b2", "#111827", "#e5eef5"],
  },
];

export const appearanceModes: ReadonlyArray<{ value: AppearanceMode; label: string }> = [
  { value: "system", label: "System" },
  { value: "light", label: "Light" },
  { value: "dark", label: "Dark" },
];

export const appearanceRadiusOptions: ReadonlyArray<{ value: AppearanceRadius; label: string }> = [
  { value: "soft", label: "Soft" },
  { value: "rounded", label: "Rounded" },
  { value: "sharp", label: "Sharp" },
];

export const appearanceDensityOptions: ReadonlyArray<{ value: AppearanceDensity; label: string }> = [
  { value: "comfortable", label: "Comfortable" },
  { value: "compact", label: "Compact" },
];

export const appearanceSurfaceOptions: ReadonlyArray<{ value: AppearanceSurface; label: string }> = [
  { value: "glass", label: "Glass" },
  { value: "solid", label: "Solid" },
];

export const appearanceMotionOptions: ReadonlyArray<{ value: AppearanceMotion; label: string }> = [
  { value: "full", label: "Full" },
  { value: "reduced", label: "Reduced" },
];

export function normalizeAppearance(value: unknown): AppearanceSettings {
  if (!value || typeof value !== "object") {
    return defaultAppearance;
  }

  const candidate = value as Partial<AppearanceSettings>;

  return {
    mode: isAppearanceMode(candidate.mode) ? candidate.mode : defaultAppearance.mode,
    theme: isAppearanceTheme(candidate.theme) ? candidate.theme : defaultAppearance.theme,
    radius: isAppearanceRadius(candidate.radius) ? candidate.radius : defaultAppearance.radius,
    density: isAppearanceDensity(candidate.density) ? candidate.density : defaultAppearance.density,
    surface: isAppearanceSurface(candidate.surface) ? candidate.surface : defaultAppearance.surface,
    motion: isAppearanceMotion(candidate.motion) ? candidate.motion : defaultAppearance.motion,
  };
}

export function resolveMode(mode: AppearanceMode, systemPrefersDark: boolean): Exclude<AppearanceMode, "system"> {
  if (mode === "system") {
    return systemPrefersDark ? "dark" : "light";
  }

  return mode;
}

export function getAppearanceBootScript() {
  const defaults = JSON.stringify(defaultAppearance);
  const storageKey = JSON.stringify(appearanceStorageKey);

  return `
    (() => {
      const defaults = ${defaults};
      const storageKey = ${storageKey};
      const root = document.documentElement;
      const systemPrefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;

      let stored = null;
      try {
        stored = JSON.parse(window.localStorage.getItem(storageKey) || "null");
      } catch {
        stored = null;
      }

      const appearance = { ...defaults, ...(stored || {}) };
      const resolvedMode = appearance.mode === "system" ? (systemPrefersDark ? "dark" : "light") : appearance.mode;

      root.dataset.theme = appearance.theme;
      root.dataset.radius = appearance.radius;
      root.dataset.density = appearance.density;
      root.dataset.surface = appearance.surface;
      root.dataset.motion = appearance.motion;
      root.style.colorScheme = resolvedMode;
      root.classList.toggle("dark", resolvedMode === "dark");
    })();
  `;
}

function isAppearanceMode(value: unknown): value is AppearanceMode {
  return value === "system" || value === "light" || value === "dark";
}

function isAppearanceTheme(value: unknown): value is AppearanceTheme {
  return value === "oasis" || value === "executive" || value === "solar" || value === "graphite";
}

function isAppearanceRadius(value: unknown): value is AppearanceRadius {
  return value === "soft" || value === "rounded" || value === "sharp";
}

function isAppearanceDensity(value: unknown): value is AppearanceDensity {
  return value === "comfortable" || value === "compact";
}

function isAppearanceSurface(value: unknown): value is AppearanceSurface {
  return value === "glass" || value === "solid";
}

function isAppearanceMotion(value: unknown): value is AppearanceMotion {
  return value === "full" || value === "reduced";
}
