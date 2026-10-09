export type ThemePreference = "light" | "dark" | "system";
export function parseThemePreference(value: string | null): ThemePreference { return value === "light" || value === "dark" || value === "system" ? value : "system"; }
export function resolveThemePreference(preference: ThemePreference, systemDark: boolean): "light" | "dark" { return preference === "system" ? (systemDark ? "dark" : "light") : preference; }
