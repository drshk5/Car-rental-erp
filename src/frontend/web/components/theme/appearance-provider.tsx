"use client";

import {
  createContext,
  type PropsWithChildren,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import {
  appearanceStorageKey,
  defaultAppearance,
  normalizeAppearance,
  resolveMode,
  type AppearanceSettings,
} from "@/lib/appearance";

type AppearanceContextValue = {
  appearance: AppearanceSettings;
  resolvedMode: "light" | "dark";
  updateAppearance: (patch: Partial<AppearanceSettings>) => void;
  resetAppearance: () => void;
};

const AppearanceContext = createContext<AppearanceContextValue | null>(null);

export function AppearanceProvider({ children }: PropsWithChildren) {
  const [appearance, setAppearance] = useState(defaultAppearance);
  const [systemPrefersDark, setSystemPrefersDark] = useState(false);

  useEffect(() => {
    const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");
    const handleChange = () => {
      setSystemPrefersDark(mediaQuery.matches);
    };

    handleChange();

    try {
      const stored = window.localStorage.getItem(appearanceStorageKey);
      if (stored) {
        setAppearance(normalizeAppearance(JSON.parse(stored)));
      }
    } catch {
      setAppearance(defaultAppearance);
    }

    mediaQuery.addEventListener("change", handleChange);
    return () => mediaQuery.removeEventListener("change", handleChange);
  }, []);

  useEffect(() => {
    const root = document.documentElement;
    const nextResolvedMode = resolveMode(appearance.mode, systemPrefersDark);

    root.dataset.theme = appearance.theme;
    root.dataset.radius = appearance.radius;
    root.dataset.density = appearance.density;
    root.dataset.surface = appearance.surface;
    root.dataset.motion = appearance.motion;
    root.style.colorScheme = nextResolvedMode;
    root.classList.toggle("dark", nextResolvedMode === "dark");

    try {
      window.localStorage.setItem(appearanceStorageKey, JSON.stringify(appearance));
    } catch {}
  }, [appearance, systemPrefersDark]);

  const value = useMemo<AppearanceContextValue>(() => {
    const resolvedMode = resolveMode(appearance.mode, systemPrefersDark);

    return {
      appearance,
      resolvedMode,
      updateAppearance: (patch) => {
        setAppearance((current) => normalizeAppearance({ ...current, ...patch }));
      },
      resetAppearance: () => {
        setAppearance(defaultAppearance);
      },
    };
  }, [appearance, systemPrefersDark]);

  return <AppearanceContext.Provider value={value}>{children}</AppearanceContext.Provider>;
}

export function useAppearance() {
  const context = useContext(AppearanceContext);

  if (!context) {
    throw new Error("useAppearance must be used within AppearanceProvider");
  }

  return context;
}
