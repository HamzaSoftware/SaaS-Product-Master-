"use client";

import Link from "next/link";
import { useRef, useState } from "react";
import { Badge, Button, Card, Input, PasswordInput, Select } from "@/components/ui/primitives";
import { Table, TableEmpty } from "@/components/ui/table";
import type { Locale } from "@/i18n/config";
import { getPlatformDictionary } from "@/i18n/platform-dictionaries";
import { normalizeTenantSlug, tenantCounts, validateTenantDraft, type TenantDraft } from "./platform-portal-model";
import { usePlatformAuth, type PlatformTenant } from "./platform-auth-provider";

export function PlatformDashboard({ locale }: { locale: Locale }) {
  const t = getPlatformDictionary(locale); const auth = usePlatformAuth(); const counts = tenantCounts(auth.tenants);
  const form = useRef<HTMLFormElement>(null); const [busy, setBusy] = useState(false); const [error, setError] = useState("");
  const [created, setCreated] = useState<PlatformTenant | null>(null);

  async function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError(""); setCreated(null); const data = new FormData(event.currentTarget);
    const draft: TenantDraft = { name: String(data.get("name")), slug: normalizeTenantSlug(String(data.get("slug"))),
      timeZone: String(data.get("timeZone")), defaultLanguage: String(data.get("defaultLanguage")) as "en" | "ar",
      adminEmail: String(data.get("adminEmail")), adminDisplayName: String(data.get("adminDisplayName")), adminPassword: String(data.get("adminPassword")) };
    const validation = validateTenantDraft(draft);
    if (validation) { setError(validation === "slug" ? t.invalidSlug : validation === "password" ? t.invalidPassword : t.required); return; }
    setBusy(true);
    try {
      const result = await auth.createTenant({ ...draft, nameAr: String(data.get("nameAr")).trim() || null });
      setCreated(result); form.current?.reset();
    } catch (failure) { setError(failure instanceof Error ? failure.message : t.loadError); }
    finally { setBusy(false); }
  }

  return <div className="page-stack platform-dashboard">
    <div className="platform-hero"><div><p className="eyebrow">{t.portal}</p><h1>{t.dashboard}</h1><p>{t.securityHelp}</p></div><span className="platform-hero-mark">N</span></div>
    <div className="platform-metrics">
      <Metric label={t.totalTenants} value={counts.total} icon="◇" />
      <Metric label={t.activeTenants} value={counts.active} icon="✓" />
      <Metric label={t.inactiveTenants} value={counts.inactive} icon="○" />
    </div>
    <Card className="platform-section" ><div className="section-heading"><div><p className="eyebrow">{t.tenants}</p><h2 id="tenants">{t.directoryTitle}</h2><p>{t.directoryHelp}</p></div><a className="button" href="#create-tenant">＋ {t.createTenant}</a></div>
      <Table><thead><tr><th>{t.companyName}</th><th>{t.slug}</th><th>{t.status}</th><th>{t.createdAt}</th><th>{t.actions}</th></tr></thead><tbody>
        {auth.tenants.length === 0 ? <tr><TableEmpty><strong>{t.emptyTitle}</strong><br />{t.emptyHelp}</TableEmpty></tr> : auth.tenants.map((tenant) => <tr key={tenant.id}>
          <td><strong>{tenant.name}</strong><small className="cell-subtitle">{tenant.id}</small></td><td><code>{tenant.slug}</code></td>
          <td><Badge tone={tenant.isActive ? "success" : "warning"}>{tenant.isActive ? t.active : t.inactive}</Badge></td>
          <td>{new Intl.DateTimeFormat(locale, { dateStyle: "medium" }).format(new Date(tenant.createdAtUtc))}</td>
          <td><Link className="table-link" href={`/${locale}/platform/tenants/${tenant.id}`}>{t.view} →</Link></td>
        </tr>)}</tbody></Table>
    </Card>
    <Card className="platform-section" ><div className="section-heading"><div><p className="eyebrow">{t.portal}</p><h2 id="create-tenant">{t.createTenant}</h2><p>{t.passwordHelp}</p></div></div>
      {created && <div className="feedback feedback-success" role="status"><strong>{t.created}</strong><span>{created.name} · <code>{created.id}</code></span><Link href={`/${locale}/platform/tenants/${created.id}`}>{t.view} →</Link></div>}
      {error && <div className="feedback feedback-error" role="alert">{error}</div>}
      <form ref={form} className="admin-form platform-create-form" onSubmit={submit}>
        <Field label={t.companyName}><Input name="name" required maxLength={200} /></Field>
        <Field label={t.companyNameAr}><Input name="nameAr" dir="rtl" maxLength={200} /></Field>
        <Field label={t.slug} help={t.slugHelp}><Input name="slug" required maxLength={100} pattern="[a-z0-9]+(?:-[a-z0-9]+)*" placeholder="acme-operations" /></Field>
        <Field label={t.timeZone}><Input name="timeZone" required defaultValue="Asia/Riyadh" list="platform-time-zones" /><datalist id="platform-time-zones"><option value="Asia/Riyadh" /><option value="UTC" /><option value="Europe/London" /><option value="America/New_York" /></datalist></Field>
        <Field label={t.language}><Select name="defaultLanguage" defaultValue="en"><option value="en">English</option><option value="ar">العربية</option></Select></Field>
        <div aria-hidden="true" />
        <Field label={t.adminEmail}><Input name="adminEmail" type="email" autoComplete="off" required maxLength={256} /></Field>
        <Field label={t.adminName}><Input name="adminDisplayName" autoComplete="off" required maxLength={200} /></Field>
        <Field label={t.adminPassword} help={t.passwordHelp} wide><PasswordInput name="adminPassword" autoComplete="new-password" required minLength={12} showLabel={locale === "ar" ? "إظهار كلمة المرور" : "Show password"} hideLabel={locale === "ar" ? "إخفاء كلمة المرور" : "Hide password"} /></Field>
        <div className="form-actions"><Button type="submit" loading={busy}>{busy ? t.creating : t.create}</Button></div>
      </form>
    </Card>
  </div>;
}

function Metric({ label, value, icon }: { label: string; value: number; icon: string }) { return <Card className="platform-metric"><span>{icon}</span><div><strong>{value}</strong><small>{label}</small></div></Card>; }
function Field({ label, help, wide, children }: { label: string; help?: string; wide?: boolean; children: React.ReactNode }) { return <label className={wide ? "form-field form-wide" : "form-field"}><span>{label}</span>{children}{help && <small>{help}</small>}</label>; }
