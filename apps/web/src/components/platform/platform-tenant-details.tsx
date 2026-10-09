"use client";

import Link from "next/link";
import { useState } from "react";
import { Badge, Button, Card } from "@/components/ui/primitives";
import type { Locale } from "@/i18n/config";
import { getPlatformDictionary } from "@/i18n/platform-dictionaries";
import { usePlatformAuth } from "./platform-auth-provider";

export function PlatformTenantDetails({ locale, tenantId }: { locale: Locale; tenantId: string }) {
  const t = getPlatformDictionary(locale); const auth = usePlatformAuth(); const tenant = auth.tenants.find((item) => item.id === tenantId);
  const [busy, setBusy] = useState(false); const [error, setError] = useState("");
  if (!tenant) return <div className="page-stack"><Link className="table-link" href={`/${locale}/platform`}>← {t.back}</Link><Card className="state"><h1>{t.notFound}</h1></Card></div>;
  async function toggle() { setBusy(true); setError(""); try { await auth.setTenantStatus(tenant!, !tenant!.isActive); } catch (failure) { setError(failure instanceof Error ? failure.message : t.loadError); } finally { setBusy(false); } }
  return <div className="page-stack platform-details"><Link className="table-link" href={`/${locale}/platform`}>← {t.back}</Link>
    <div className="page-header"><div><p className="eyebrow">{t.details}</p><h1>{tenant.name}</h1><p><code>{tenant.slug}</code></p></div><Badge tone={tenant.isActive ? "success" : "warning"}>{tenant.isActive ? t.active : t.inactive}</Badge></div>
    {error && <div className="feedback feedback-error" role="alert">{error}</div>}
    <Card className="platform-detail-card"><Detail label={t.tenantId} value={tenant.id} mono /><Detail label={t.companyName} value={tenant.name} /><Detail label={t.slug} value={tenant.slug} mono /><Detail label={t.createdAt} value={new Date(tenant.createdAtUtc).toLocaleString(locale)} /><Detail label={t.updatedAt} value={new Date(tenant.updatedAtUtc).toLocaleString(locale)} />
      <div className="platform-danger-zone"><div><strong>{tenant.isActive ? t.deactivate : t.activate}</strong><p>{t.securityHelp}</p></div><Button className={tenant.isActive ? "button-danger" : ""} disabled={busy} onClick={() => void toggle()}>{busy ? t.saving : tenant.isActive ? t.deactivate : t.activate}</Button></div>
    </Card>
  </div>;
}
function Detail({ label, value, mono }: { label: string; value: string; mono?: boolean }) { return <div className="platform-detail-row"><span>{label}</span><strong className={mono ? "mono" : ""}>{value}</strong></div>; }
