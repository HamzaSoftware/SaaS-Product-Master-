"use client";
import { Button } from "@/components/ui/primitives";
import { useTheme } from "./theme-provider";
export function ThemeToggle({ locale }: { locale: "en" | "ar" }) { const theme = useTheme(); const dark = theme.resolved === "dark"; const label = locale === "ar" ? (dark ? "استخدام المظهر الفاتح" : "استخدام المظهر الداكن") : (dark ? "Use light theme" : "Use dark theme"); return <Button variant="ghost" size="icon" aria-label={label} title={label} onClick={() => theme.setPreference(dark ? "light" : "dark")}><span aria-hidden="true">{dark ? "☀" : "◐"}</span></Button>; }
