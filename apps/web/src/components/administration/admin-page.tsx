import { Breadcrumbs, PageHeader } from "@/components/navigation/page-header";
import { AdminManager } from "./admin-manager";
import { getAdminDictionary } from "@/i18n/admin-dictionaries";
import type { Locale } from "@/i18n/config";

const content = { company: ["company", "companyHelp"], sites: ["sites", "sitesHelp"], types: ["locationTypes", "typesHelp"], locations: ["locations", "locationsHelp"] } as const;
export function AdminPage({ locale, kind }: { locale: Locale; kind: keyof typeof content }) {
  const t = getAdminDictionary(locale); const [titleKey, helpKey] = content[kind];
  return <div className="page-stack"><Breadcrumbs items={[{ label: t.administration, href: `/${locale}/administration` }, { label: t[titleKey] }]} /><PageHeader eyebrow={t.administration} title={t[titleKey]} description={t[helpKey]} /><AdminManager locale={locale} kind={kind} /></div>;
}
