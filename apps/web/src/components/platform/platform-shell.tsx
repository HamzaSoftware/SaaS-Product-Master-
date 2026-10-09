"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/primitives";
import { ThemeToggle } from "@/components/theme/theme-toggle";
import type { Locale } from "@/i18n/config";
import { getPlatformDictionary } from "@/i18n/platform-dictionaries";
import { usePlatformAuth } from "./platform-auth-provider";

export function PlatformShell({ locale, children }: { locale: Locale; children: React.ReactNode }) {
  const pathname = usePathname(); const router = useRouter(); const auth = usePlatformAuth(); const t = getPlatformDictionary(locale);
  const [navigationOpen, setNavigationOpen] = useState(false); const isLogin = pathname === `/${locale}/platform/login`;
  const opposite = locale === "en" ? "ar" : "en"; const localePath = pathname.replace(/^\/(en|ar)(?=\/|$)/, `/${opposite}`);
  useEffect(() => {
    if (auth.loading) return;
    if (!auth.authenticated && !isLogin) router.replace(`/${locale}/platform/login`);
    if (auth.authenticated && isLogin) router.replace(`/${locale}/platform`);
  }, [auth.loading, auth.authenticated, isLogin, locale, router]);
  if (isLogin) return children;
  if (auth.loading || !auth.authenticated) return <main className="login-page"><div className="spinner" aria-label={t.loading} /></main>;
  return <div className="app-frame platform-frame">
    <button className={`nav-scrim ${navigationOpen ? "is-open" : ""}`} aria-label={locale === "ar" ? "إغلاق التنقل" : "Close navigation"} onClick={() => setNavigationOpen(false)} />
    <aside className={`sidebar platform-sidebar ${navigationOpen ? "is-open" : ""}`}>
      <Link className="brand" href={`/${locale}/platform`}><span className="brand-mark">N</span><span><strong>Nexa</strong><small>Platform</small></span></Link>
      <p className="nav-section-label">{t.portal}</p>
      <nav className="primary-nav" aria-label={t.portal}>
        <Link className={pathname === `/${locale}/platform` ? "active" : undefined} href={`/${locale}/platform`} onClick={() => setNavigationOpen(false)}><span>▦</span><span>{t.dashboard}</span></Link>
        <Link className={pathname.startsWith(`/${locale}/platform/tenants/`) ? "active" : undefined} href={`/${locale}/platform#tenants`} onClick={() => setNavigationOpen(false)}><span>◇</span><span>{t.tenants}</span></Link>
        <Link href={`/${locale}/platform#create-tenant`} onClick={() => setNavigationOpen(false)}><span>＋</span><span>{t.createTenant}</span></Link>
      </nav>
      <div className="sidebar-footer"><span className="environment-dot" />{t.securityLabel}</div>
    </aside>
    <div className="content-frame"><header className="topbar platform-topbar">
      <Button variant="ghost" size="icon" className="mobile-menu" aria-label={locale === "ar" ? "فتح التنقل" : "Open navigation"} onClick={() => setNavigationOpen(true)}>☰</Button>
      <div className="tenant-context"><span className="tenant-logo">PA</span><span><small>{t.portal}</small><strong>{t.product}</strong></span></div>
      <div className="topbar-actions"><ThemeToggle locale={locale} /><Link className="language-switcher" href={localePath}>{opposite === "ar" ? "العربية" : "English"}</Link>
        <Button className="user-button platform-user-button" onClick={() => void auth.signOut().then(() => router.replace(`/${locale}/platform/login`))}><span className="avatar">PA</span><span className="user-copy"><strong>{t.signOut}</strong><small>{t.securityLabel}</small></span></Button>
      </div>
    </header><main className="main-content">{children}</main></div>
  </div>;
}
