"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Badge, Button, Card, Input, Select, Textarea } from "@/components/ui/primitives";
import { getAdminDictionary } from "@/i18n/admin-dictionaries";
import type { Locale } from "@/i18n/config";
import { apiRequest } from "@/lib/platform-api";
import { buildLocationTree, displayLocationName, type LocationItem, type LocationTreeNode } from "@/components/locations/tree";

type Kind = "company" | "sites" | "types" | "locations";
type RecordItem = LocationItem & { version?: string; description?: string; timeZone?: string; locationTypeId?: string; siteId?: string; nameAr?: string | null };
type PageResult = { items: RecordItem[]; total: number };
const emptyVersion = "00000000-0000-0000-0000-000000000000";

export function AdminManager({ locale, kind }: { locale: Locale; kind: Kind }) {
  const t = getAdminDictionary(locale);
  const [items, setItems] = useState<RecordItem[]>([]);
  const [sites, setSites] = useState<RecordItem[]>([]);
  const [types, setTypes] = useState<RecordItem[]>([]);
  const [company, setCompany] = useState<Record<string, string>>({});
  const [siteId, setSiteId] = useState("");
  const [editing, setEditing] = useState<RecordItem | null | undefined>(undefined);
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(true);
  const [query, setQuery] = useState("");

  const load = useCallback(async () => {
    setBusy(true); setMessage("");
    try {
      if (kind === "company") setCompany(await apiRequest<Record<string, string>>("/api/v1/company"));
      if (kind === "sites") {
        const result = await apiRequest<PageResult>(`/api/v1/sites?page=1&pageSize=100&search=${encodeURIComponent(query)}`); setItems(result.items);
      }
      if (kind === "types") setItems(await apiRequest<RecordItem[]>("/api/v1/location-types"));
      if (kind === "locations") {
        const [sitePage, typeList] = await Promise.all([apiRequest<PageResult>("/api/v1/sites?page=1&pageSize=100"), apiRequest<RecordItem[]>("/api/v1/location-types")]);
        setSites(sitePage.items); setTypes(typeList);
        const selected = siteId || sitePage.items[0]?.id || ""; setSiteId(selected);
        setItems(selected ? await apiRequest<RecordItem[]>(`/api/v1/locations?siteId=${selected}`) : []);
      }
    } catch (error) { setMessage(error instanceof Error ? error.message : t.loadError); }
    finally { setBusy(false); }
  }, [kind, query, siteId, t.loadError]);

  useEffect(() => {
    // The effect starts the external API synchronization for this selected module/site.
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  async function saveCompany(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget);
    try {
      const payload = { ...company, nameEn: data.get("nameEn"), nameAr: data.get("nameAr"), legalNameEn: data.get("legalNameEn"),
        legalNameAr: data.get("legalNameAr"), email: data.get("email"), phone: data.get("phone"), website: data.get("website"),
        country: data.get("country"), city: data.get("city"), address: data.get("address"), timeZone: data.get("timeZone"),
        defaultLanguage: data.get("defaultLanguage") };
      setCompany(await apiRequest<Record<string, string>>("/api/v1/company", { method: "PUT", body: JSON.stringify(payload) })); setMessage(t.saved);
    } catch (error) { setMessage(error instanceof Error ? error.message : t.loadError); }
  }

  async function saveRecord(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault(); const data = new FormData(event.currentTarget); const current = editing ?? null;
    const path = kind === "types" ? "/api/v1/location-types" : kind === "locations" ? "/api/v1/locations" : "/api/v1/sites";
    const payload = { code: data.get("code"), nameEn: data.get("nameEn"), nameAr: data.get("nameAr"), description: data.get("description"),
      timeZone: data.get("timeZone") || "UTC", sortOrder: Number(data.get("sortOrder") || 0), siteId: data.get("siteId") || siteId,
      parentLocationId: data.get("parentLocationId") || null, locationTypeId: data.get("locationTypeId") || undefined,
      address: data.get("address"), city: data.get("city"), country: data.get("country"), latitude: null, longitude: null,
      version: current?.version ?? emptyVersion };
    try {
      await apiRequest(`${path}${current ? `/${current.id}` : ""}`, { method: current ? "PUT" : "POST", body: JSON.stringify(payload) });
      setEditing(undefined); setMessage(t.saved); await load();
    } catch (error) { setMessage(error instanceof Error ? error.message : t.loadError); }
  }

  async function toggle(item: RecordItem) {
    const path = kind === "types" ? "/api/v1/location-types" : kind === "locations" ? "/api/v1/locations" : "/api/v1/sites";
    try { await apiRequest(`${path}/${item.id}/status`, { method: "PATCH", body: JSON.stringify({ isActive: !item.isActive, version: item.version ?? emptyVersion }) }); await load(); }
    catch (error) { setMessage(error instanceof Error ? error.message : t.loadError); }
  }

  if (kind === "company") return <Card><form key={company.version || "loading"} className="admin-form" onSubmit={saveCompany}>
    <Field label={t.nameEn}><Input name="nameEn" required defaultValue={company.nameEn} /></Field>
    <Field label={t.nameAr}><Input name="nameAr" dir="rtl" defaultValue={company.nameAr} /></Field>
    <Field label={t.legalNameEn}><Input name="legalNameEn" defaultValue={company.legalNameEn} /></Field>
    <Field label={t.legalNameAr}><Input name="legalNameAr" dir="rtl" defaultValue={company.legalNameAr} /></Field>
    <Field label={t.email}><Input name="email" type="email" defaultValue={company.email} /></Field>
    <Field label={t.phone}><Input name="phone" defaultValue={company.phone} /></Field>
    <Field label={t.website}><Input name="website" type="url" defaultValue={company.website} /></Field>
    <Field label={t.country}><Input name="country" defaultValue={company.country} /></Field>
    <Field label={t.city}><Input name="city" defaultValue={company.city} /></Field>
    <Field label={t.address} wide><Textarea name="address" defaultValue={company.address} /></Field>
    <Field label={t.timeZone}><Input name="timeZone" required defaultValue={company.timeZone || "UTC"} /></Field>
    <Field label={t.language}><Select name="defaultLanguage" defaultValue={company.defaultLanguage || "en"}><option value="en">English</option><option value="ar">العربية</option></Select></Field>
    <div className="form-actions"><Button type="submit">{t.save}</Button><Status busy={busy} message={message} t={t} /></div>
  </form></Card>;

  return <div className="admin-workspace">
    <Card className="admin-toolbar">
      {kind === "sites" && <Input aria-label={t.search} placeholder={t.search} value={query} onChange={(event) => setQuery(event.target.value)} />}
      {kind === "locations" && <Select aria-label={t.site} value={siteId} onChange={(event) => setSiteId(event.target.value)}>{sites.map((site) => <option key={site.id} value={site.id}>{site.code} · {displayLocationName(site, locale)}</option>)}</Select>}
      <Button onClick={() => setEditing(null)}>＋ {t.add}</Button>
    </Card>
    <Status busy={busy} message={message} t={t} />
    {kind === "locations" ? <LocationTree nodes={buildLocationTree(items)} locale={locale} onEdit={setEditing} onToggle={toggle} t={t} /> :
      <ResourceTable items={items} locale={locale} onEdit={setEditing} onToggle={toggle} t={t} />}
    {editing !== undefined && <Card><h2>{editing ? t.edit : t.add}</h2><form className="admin-form" onSubmit={saveRecord}>
      <Field label={t.code}><Input name="code" required defaultValue={editing?.code} /></Field>
      <Field label={t.nameEn}><Input name="nameEn" required defaultValue={editing?.nameEn} /></Field>
      <Field label={t.nameAr}><Input name="nameAr" dir="rtl" defaultValue={editing?.nameAr ?? ""} /></Field>
      <Field label={t.description} wide><Textarea name="description" defaultValue={editing?.description ?? ""} /></Field>
      {kind === "sites" && <><Field label={t.timeZone}><Input name="timeZone" required defaultValue={editing?.timeZone ?? "UTC"} /></Field><Field label={t.city}><Input name="city" /></Field><Field label={t.country}><Input name="country" /></Field><Field label={t.address} wide><Textarea name="address" /></Field></>}
      {kind !== "sites" && <Field label={t.sortOrder}><Input name="sortOrder" type="number" defaultValue={editing?.sortOrder ?? 0} /></Field>}
      {kind === "locations" && <><input type="hidden" name="siteId" value={siteId} /><Field label={t.type}><Select name="locationTypeId" required defaultValue={editing?.locationTypeId}>{types.filter((x) => x.isActive).map((type) => <option key={type.id} value={type.id}>{displayLocationName(type, locale)}</option>)}</Select></Field><Field label={t.parent}><Select name="parentLocationId" defaultValue={editing?.parentLocationId ?? ""}><option value="">{t.root}</option>{items.filter((x) => x.id !== editing?.id).map((item) => <option key={item.id} value={item.id}>{item.code} · {displayLocationName(item, locale)}</option>)}</Select></Field></>}
      <div className="form-actions"><Button type="submit">{t.save}</Button><Button className="button-secondary" onClick={() => setEditing(undefined)}>{t.cancel}</Button></div>
    </form></Card>}
  </div>;
}

function Field({ label, wide, children }: { label: string; wide?: boolean; children: React.ReactNode }) { return <label className={wide ? "form-field form-wide" : "form-field"}><span>{label}</span>{children}</label>; }
function Status({ busy, message, t }: { busy: boolean; message: string; t: ReturnType<typeof getAdminDictionary> }) { return <p className="admin-message" role="status">{busy ? t.loading : message}</p>; }
function ResourceTable({ items, locale, onEdit, onToggle, t }: { items: RecordItem[]; locale: Locale; onEdit: (x: RecordItem) => void; onToggle: (x: RecordItem) => void; t: ReturnType<typeof getAdminDictionary> }) {
  return <div className="table-scroll"><table className="table"><thead><tr><th>{t.code}</th><th>{locale === "ar" ? t.nameAr : t.nameEn}</th><th>{t.status}</th><th>{t.actions}</th></tr></thead><tbody>{items.length === 0 ? <tr><td colSpan={4} className="table-empty">{t.empty}</td></tr> : items.map((item) => <tr key={item.id}><td><strong>{item.code}</strong></td><td>{displayLocationName(item, locale)}</td><td><Badge tone={item.isActive ? "success" : "warning"}>{item.isActive ? t.active : t.inactive}</Badge></td><td className="row-actions"><button onClick={() => onEdit(item)}>{t.edit}</button><button onClick={() => void onToggle(item)}>{item.isActive ? t.inactive : t.active}</button></td></tr>)}</tbody></table></div>;
}
function LocationTree({ nodes, locale, onEdit, onToggle, t }: { nodes: LocationTreeNode[]; locale: Locale; onEdit: (x: RecordItem) => void; onToggle: (x: RecordItem) => void; t: ReturnType<typeof getAdminDictionary> }) {
  const [collapsed, setCollapsed] = useState<Set<string>>(new Set());
  const rows = useMemo(() => flatten(nodes, 0, collapsed), [nodes, collapsed]);
  function toggleExpanded(id: string) { setCollapsed((current) => { const next = new Set(current); if (next.has(id)) next.delete(id); else next.add(id); return next; }); }
  return <Card className="tree-card">{rows.length === 0 ? <p className="table-empty">{t.empty}</p> : rows.map(({ node, depth }) => <div className="tree-row" key={node.id}><div style={{ paddingInlineStart: `${depth * 28}px` }}><button className="tree-toggle" aria-label={`${displayLocationName(node, locale)} ${t.children}`} aria-expanded={!collapsed.has(node.id)} disabled={node.children.length === 0} onClick={() => toggleExpanded(node.id)}>{node.children.length ? (collapsed.has(node.id) ? "▸" : "▾") : "·"}</button><strong>{displayLocationName(node, locale)}</strong><small>{node.code}</small></div><Badge tone={node.isActive ? "success" : "warning"}>{node.isActive ? t.active : t.inactive}</Badge><div className="row-actions"><button onClick={() => onEdit(node as RecordItem)}>{t.edit}</button><button onClick={() => void onToggle(node as RecordItem)}>{node.isActive ? t.inactive : t.active}</button></div></div>)}</Card>;
}
function flatten(nodes: LocationTreeNode[], depth = 0, collapsed = new Set<string>()): { node: LocationTreeNode; depth: number }[] { return nodes.flatMap((node) => [{ node, depth }, ...(collapsed.has(node.id) ? [] : flatten(node.children, depth + 1, collapsed))]); }
