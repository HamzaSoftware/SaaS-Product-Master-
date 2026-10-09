import Link from "next/link";
import { notFound } from "next/navigation";
import { Breadcrumbs, PageHeader } from "@/components/navigation/page-header";
import { Card } from "@/components/ui/primitives";
import { getAdminDictionary } from "@/i18n/admin-dictionaries";
import { isLocale } from "@/i18n/config";

export default async function AdministrationPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params; if (!isLocale(locale)) notFound(); const t = getAdminDictionary(locale);
  const modules = [[`/${locale}/administration/company`, t.company, t.companyHelp], [`/${locale}/administration/sites`, t.sites, t.sitesHelp], [`/${locale}/administration/location-types`, t.locationTypes, t.typesHelp], [`/${locale}/locations`, t.locations, t.locationsHelp], [`/${locale}/administration/users`, locale === "ar" ? "المستخدمون" : "Users", locale === "ar" ? "إدارة العضويات والأدوار ونطاق المواقع." : "Manage memberships, roles, and site access."], [`/${locale}/administration/roles`, locale === "ar" ? "الأدوار والصلاحيات" : "Roles & permissions", locale === "ar" ? "إنشاء أدوار مخصصة من كتالوج الصلاحيات." : "Create custom roles from the permission catalog."], [`/${locale}/settings/security`, locale === "ar" ? "الأمان والجلسات" : "Security & sessions", locale === "ar" ? "إدارة كلمة المرور والجلسات النشطة." : "Manage password and active sessions."]];
  return <div className="page-stack"><Breadcrumbs items={[{ label: t.administration }]} /><PageHeader eyebrow={t.administration} title={t.administration} description={locale === "ar" ? "تهيئة بيانات المنشأة وبنيتها التشغيلية." : "Configure your organization and operational structure."} /><div className="foundation-grid">{modules.map(([href, title, description]) => <Link key={href} href={href}><Card className="admin-module-card"><span className="foundation-icon">◇</span><div><h2>{title}</h2><p>{description}</p></div><strong>{t.open} →</strong></Card></Link>)}</div></div>;
}
