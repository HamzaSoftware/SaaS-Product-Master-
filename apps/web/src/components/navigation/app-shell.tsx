"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import type { Locale } from "@/i18n/config";
import { getDictionary } from "@/i18n/dictionaries";
import { cn } from "@/lib/cn";
import { Button } from "@/components/ui/primitives";
import { ThemeToggle } from "@/components/theme/theme-toggle";
import { ToastProvider } from "@/components/ui/toast";
import { AuthProvider, useAuth } from "@/components/auth/auth-provider";
import { PlatformAuthProvider } from "@/components/platform/platform-auth-provider";
import { PlatformShell } from "@/components/platform/platform-shell";

export function AppShell({ locale, children }: { locale: Locale; children: React.ReactNode }) {
  const pathname = usePathname();
  if (pathname.startsWith(`/${locale}/platform`)) return <PlatformAuthProvider><PlatformShell locale={locale}>{children}</PlatformShell></PlatformAuthProvider>;
  return <AuthProvider><AuthenticatedShell locale={locale}>{children}</AuthenticatedShell></AuthProvider>;
}

function AuthenticatedShell({ locale, children }: { locale: Locale; children: React.ReactNode }) {
  const dictionary = getDictionary(locale); const pathname = usePathname(); const router = useRouter(); const auth = useAuth(); const [navigationOpen, setNavigationOpen] = useState(false);
  const oppositeLocale: Locale = locale === "en" ? "ar" : "en";
  const localePath = pathname.replace(/^\/(en|ar)(?=\/|$)/, `/${oppositeLocale}`);
  const isLogin = pathname === `/${locale}/login`;
  useEffect(() => { if (!auth.loading && !auth.user && !isLogin) router.replace(`/${locale}/login`); }, [auth.loading, auth.user, isLogin, locale, router]);
  if (isLogin) return children;
  if (auth.loading || !auth.user) return <main className="login-page"><div className="spinner" aria-label="Loading session" /></main>;
  const navigation = [
    { href: `/${locale}`, label: dictionary.overview, icon: "⌂", exact: true },
    { href: `/${locale}/assets`, label: dictionary.nav.assets, icon: "▣" },
    { href: `/${locale}/maintenance`, label: dictionary.nav.maintenance, icon: "◆" },
    { href: `/${locale}/maintenance/pm`, label: locale === "ar" ? "الصيانة الوقائية" : "Preventive maintenance", icon: "↻" },
    { href: `/${locale}/inventory`, label: locale === "ar" ? "المخزون وقطع الغيار" : "Inventory & spare parts", icon: "▦" },
    { href: `/${locale}/procurement`, label: locale === "ar" ? "المشتريات" : "Procurement", icon: "▤" },
    { href: `/${locale}/requests`, label: locale === "ar" ? "طلبات الخدمة" : "Service requests", icon: "◇" },
    { href: `/${locale}/locations`, label: dictionary.nav.locations, icon: "⌖" },
    { href: `/${locale}/administration`, label: dictionary.nav.administration, icon: "⚙" },
    { href: `/${locale}/settings/security`, label: dictionary.settings, icon: "◇" },
  ];
  return <ToastProvider><div className="app-frame">
    <button className={cn("nav-scrim", navigationOpen && "is-open")} aria-label={dictionary.closeNavigation} onClick={() => setNavigationOpen(false)} />
    <aside className={cn("sidebar", navigationOpen && "is-open")}>
      <Link className="brand" href={`/${locale}`}><span className="brand-mark" aria-hidden="true">N</span><span><strong>Nexa</strong><small>Operations</small></span></Link>
      <p className="nav-section-label">{dictionary.navigation}</p>
      <nav className="primary-nav" aria-label={dictionary.navigation}>{navigation.map((item) => { const active = item.exact ? pathname === item.href : pathname === item.href || pathname.startsWith(`${item.href}/`); return <Link key={item.href} href={item.href} className={cn(active && "active")} onClick={() => setNavigationOpen(false)}><span aria-hidden="true">{item.icon}</span><span>{item.label}</span></Link>; })}</nav>
      <div className="sidebar-footer"><span className="environment-dot" /><span>{dictionary.environment}</span></div>
    </aside>
    <div className="content-frame"><header className="topbar">
      <Button className="icon-button mobile-menu" aria-label={dictionary.openNavigation} onClick={() => setNavigationOpen(true)}>☰</Button>
      <div className="tenant-context"><span className="tenant-logo">NX</span><span><small>{dictionary.workspaceLabel}</small><strong>{dictionary.workspace}</strong></span></div>
      <div className="topbar-actions"><ThemeToggle locale={locale} /><Link className="language-switcher" href={localePath} hrefLang={oppositeLocale}>{dictionary.switchLanguage}</Link><details className="user-menu"><summary className="user-button" aria-label={dictionary.userMenu}><span className="avatar">U</span><span className="user-copy"><strong>{dictionary.account}</strong><small>{auth.user.userId.slice(0, 8)}</small></span><span aria-hidden="true">⌄</span></summary><div className="dropdown-menu"><button onClick={() => void auth.signOut().then(() => router.replace(`/${locale}/login`))}>{dictionary.signOut}</button></div></details></div>
    </header><main className="main-content">{children}</main></div>
  </div></ToastProvider>;
}
