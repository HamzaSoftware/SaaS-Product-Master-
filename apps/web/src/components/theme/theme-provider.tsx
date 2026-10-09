"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { parseThemePreference, resolveThemePreference, type ThemePreference } from "./theme-model";

type ThemeContextValue = { preference: ThemePreference; resolved: "light" | "dark"; setPreference(value: ThemePreference): void };
const ThemeContext = createContext<ThemeContextValue | null>(null);

function resolveTheme(preference: ThemePreference) { return resolveThemePreference(preference, window.matchMedia("(prefers-color-scheme: dark)").matches); }

export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const [preference, setPreferenceState] = useState<ThemePreference>("system");
  const [resolved, setResolved] = useState<"light" | "dark">("light");
  useEffect(() => {
    const initial = parseThemePreference(window.localStorage.getItem("nexa-theme"));
    // The persisted preference is only available after client hydration.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    setPreferenceState(initial);
    const media = window.matchMedia("(prefers-color-scheme: dark)");
    const apply = () => { const next = resolveTheme(initial); setResolved(next); document.documentElement.dataset.theme = next; };
    apply(); media.addEventListener("change", apply); return () => media.removeEventListener("change", apply);
  }, []);
  function setPreference(value: ThemePreference) {
    window.localStorage.setItem("nexa-theme", value); setPreferenceState(value); const next = resolveTheme(value); setResolved(next); document.documentElement.dataset.theme = next;
  }
  const value = useMemo(() => ({ preference, resolved, setPreference }), [preference, resolved]);
  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() { const value = useContext(ThemeContext); if (!value) throw new Error("ThemeProvider is required"); return value; }
