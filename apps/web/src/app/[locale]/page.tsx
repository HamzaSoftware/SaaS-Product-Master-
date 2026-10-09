import type { Metadata } from "next";
import { notFound } from "next/navigation";
import Link from "next/link";
import { Badge, Card } from "@/components/ui/primitives";
import { Breadcrumbs, PageHeader } from "@/components/navigation/page-header";
import { getDictionary } from "@/i18n/dictionaries";
import { isLocale } from "@/i18n/config";

export const metadata: Metadata = { title: "Dashboard" };
export default async function DashboardPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params; if (!isLocale(locale)) notFound(); const dictionary = getDictionary(locale); const ar = locale === "ar";
  const actions = [
    { href: `/${locale}/assets`, icon: "▣", title: ar ? "سجل الأصول" : "Asset registry", text: ar ? "الأصول وحالتها وموقعها وسجلها." : "Assets, operating state, placement, and history." },
    { href: `/${locale}/administration/company`, icon: "▤", title: ar ? "ملف الشركة" : "Company profile", text: ar ? "الهوية ثنائية اللغة والإعدادات الإقليمية." : "Bilingual identity and regional defaults." },
    { href: `/${locale}/administration/sites`, icon: "⌖", title: ar ? "المواقع" : "Sites", text: ar ? "المواقع التشغيلية والعناوين والحالة." : "Operational sites, addresses, and lifecycle." },
    { href: `/${locale}/locations`, icon: "⌘", title: ar ? "هيكل الأماكن" : "Location structure", text: ar ? "شجرة مرنة للمباني والطوابق والمساحات." : "Flexible hierarchy for buildings, floors, and spaces." },
    { href: `/${locale}/administration/users`, icon: "◎", title: ar ? "المستخدمون والوصول" : "Users & access", text: ar ? "العضويات والأدوار ونطاق المواقع." : "Memberships, roles, and site scope." },
    { href: `/${locale}/administration/roles`, icon: "◇", title: ar ? "الأدوار والصلاحيات" : "Roles & permissions", text: ar ? "أدوار دقيقة من كتالوج الصلاحيات." : "Precise roles from the permission catalog." },
    { href: `/${locale}/settings/security`, icon: "◈", title: ar ? "الأمان والجلسات" : "Security & sessions", text: ar ? "كلمة المرور والجلسات النشطة." : "Password and active session controls." },
  ];
  return <div className="page-stack"><Breadcrumbs items={[{ label: dictionary.overview }]} /><PageHeader eyebrow={dictionary.foundation} title={ar ? "نظرة عامة على مساحة العمل" : "Workspace overview"} description={dictionary.foundationDescription} action={<Badge tone="success">{ar ? "متصل" : "Connected"}</Badge>} /><Card className="welcome-panel"><div><p className="eyebrow">{ar ? "مساحة موحدة · وصول حسب الصلاحية" : "One workspace · permission-aware access"}</p><h2>{ar ? "كل ما تحتاجه لإعداد عملياتك." : "Everything you need to configure operations."}</h2><p>{ar ? "ابدأ ببيانات المنشأة، ثم نظّم المواقع والأماكن ووصول الفريق." : "Start with organization data, then structure locations and team access."}</p></div><div className="security-note"><span aria-hidden="true">◆</span><div><strong>{ar ? "سياق مستأجر محمي" : "Protected tenant context"}</strong><p>{ar ? "تُتحقق الجلسة والصلاحيات والنطاق على الخادم." : "Session, permission, and scope are validated by the server."}</p></div></div></Card><section><div className="section-heading"><div><p className="eyebrow">{ar ? "إجراءات سريعة" : "Quick actions"}</p><h2>{ar ? "إدارة مساحة العمل" : "Manage your workspace"}</h2></div><Link className="text-link" href={`/${locale}/administration`}>{ar ? "عرض كل إعدادات الإدارة" : "View all administration"} →</Link></div><div className="action-grid">{actions.map((item) => <Link key={item.href} href={item.href}><Card className="action-card"><span className="action-icon" aria-hidden="true">{item.icon}</span><div><h3>{item.title}</h3><p>{item.text}</p></div><span className="action-arrow" aria-hidden="true">→</span></Card></Link>)}</div></section></div>;
}
