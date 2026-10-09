"use client";
import type { FormEvent } from "react";
import { useEffect, useState } from "react";
import { Button, Card, Select } from "@/components/ui/primitives";
import { useAuth } from "@/components/auth/auth-provider";
import type { Locale } from "@/i18n/config";
import { getAssetDictionary } from "@/i18n/asset-dictionaries";
import { ApiError, apiRequest } from "@/lib/platform-api";
import type { Asset, Location, Page, Site } from "./asset-model";

export function AssetRelocation({ locale, assetId }: { locale: Locale; assetId: string }) {
  const auth = useAuth();
  const t = getAssetDictionary(locale); const [asset, setAsset] = useState<Asset>(); const [sites, setSites] = useState<Site[]>([]); const [locations, setLocations] = useState<Location[]>([]); const [siteId, setSiteId] = useState(""); const [notice, setNotice] = useState(""); const [busy, setBusy] = useState(false);
  useEffect(() => { void Promise.all([apiRequest<Asset>(`/api/v1/assets/${assetId}`), apiRequest<Page<Site>>("/api/v1/sites?pageSize=100")]).then(([a, s]) => { setAsset(a); setSiteId(a.siteId); setSites(s.items); }).catch(() => undefined); }, [assetId]);
  useEffect(() => { if (siteId) void apiRequest<Location[]>(`/api/v1/locations?siteId=${siteId}`).then(setLocations).catch(() => setLocations([])); }, [siteId]);
  async function submit(event: FormEvent<HTMLFormElement>) { event.preventDefault(); if (!asset) return; const form = new FormData(event.currentTarget); try { setBusy(true); setNotice(""); const moved = await apiRequest<Asset>(`/api/v1/assets/${asset.id}/relocation`, { method: "PATCH", body: JSON.stringify({ siteId, locationId: form.get("locationId") || null, version: asset.version }) }); setAsset(moved); setNotice(locale === "ar" ? "تم نقل الأصل وتسجيل العملية في السجل." : "Asset relocated and recorded in history."); } catch (error) { setNotice(error instanceof ApiError && error.status === 409 ? t.conflict : error instanceof Error ? error.message : t.loadError); } finally { setBusy(false); } }
  if (!asset || asset.lifecycleState === 1 || !auth.user?.permissions.includes("assets.relocate")) return null;
  return <Card><h2>{t.relocate}</h2><form className="form-actions asset-relocation-form" onSubmit={submit}><Select aria-label={t.site} value={siteId} onChange={(event) => setSiteId(event.target.value)}>{sites.filter((x) => x.isActive).map((x) => <option key={x.id} value={x.id}>{locale === "ar" && x.nameAr ? x.nameAr : x.nameEn}</option>)}</Select><Select name="locationId" aria-label={t.location} defaultValue={asset.locationId ?? ""}><option value="">—</option>{locations.filter((x) => x.isActive).map((x) => <option key={x.id} value={x.id}>{locale === "ar" ? x.pathAr ?? x.nameAr ?? x.nameEn : x.pathEn ?? x.nameEn}</option>)}</Select><Button type="submit" loading={busy}>{t.relocate}</Button></form>{notice && <p className="admin-message" role="status">{notice}</p>}</Card>;
}
