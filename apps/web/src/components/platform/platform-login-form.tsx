"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Button, Card, Input, PasswordInput } from "@/components/ui/primitives";
import { ThemeToggle } from "@/components/theme/theme-toggle";
import type { Locale } from "@/i18n/config";
import { getPlatformDictionary } from "@/i18n/platform-dictionaries";
import { usePlatformAuth } from "./platform-auth-provider";
import { platformLoginFailure } from "./platform-portal-model";

export function PlatformLoginForm({ locale }: { locale: Locale }) {
  const auth = usePlatformAuth(); const router = useRouter(); const t = getPlatformDictionary(locale);
  const [error, setError] = useState(""); const [busy, setBusy] = useState(false);
  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError(""); setBusy(true); const data = new FormData(event.currentTarget);
    try { await auth.signIn(String(data.get("email")), String(data.get("password"))); router.replace(`/${locale}/platform`); }
    catch (error) { setError(platformLoginFailure(error) === "credentials" ? t.invalidCredentials : t.serviceUnavailable); setBusy(false); }
  }
  return <main className="auth-layout platform-auth-layout"><section className="auth-story platform-auth-story"><Link className="auth-brand" href={`/${locale}/platform/login`}><span className="brand-mark">N</span><span><strong>Nexa</strong><small>Platform</small></span></Link><div className="auth-story-copy"><p className="eyebrow">{t.portal}</p><h1>{locale === "ar" ? "إدارة المنصة بثقة ووضوح." : "Run the platform with confidence and control."}</h1><p>{locale === "ar" ? "مساحة تشغيل مخصصة لإدارة المستأجرين وحالة الخدمة." : "A dedicated operator workspace for tenant provisioning and lifecycle control."}</p></div><div className="auth-trust"><span aria-hidden="true">◆</span><div><strong>{t.securityLabel}</strong><p>{t.securityHelp}</p></div></div></section><section className="auth-form-panel"><div className="auth-panel-tools"><ThemeToggle locale={locale} /><Link href={`/${locale === "ar" ? "en" : "ar"}/platform/login`}>{locale === "ar" ? "English" : "العربية"}</Link></div><Card className="login-card platform-login-card">
    <p className="eyebrow">{t.portal}</p><h2>{t.signIn}</h2><p>{t.signInHelp}</p>
    <form className="login-form" onSubmit={submit}>
      <label>{t.email}<Input name="email" type="email" autoComplete="username" required autoFocus /></label>
      <label>{t.password}<PasswordInput name="password" autoComplete="current-password" required showLabel={locale === "ar" ? "إظهار كلمة المرور" : "Show password"} hideLabel={locale === "ar" ? "إخفاء كلمة المرور" : "Hide password"} /></label>
      {error && <p className="form-error" role="alert">{error}</p>}
      <Button type="submit" loading={busy}>{busy ? t.loading : t.signIn}</Button>
    </form>
    <div className="platform-security-note"><span aria-hidden="true">◆</span><p><strong>{t.securityLabel}</strong><br />{t.securityHelp}</p></div>
    <Link className="platform-tenant-login-link" href={`/${locale}/login`}>{locale === "ar" ? "دخول المستأجر" : "Tenant sign in"}</Link>
  </Card></section></main>;
}
