import { notFound } from "next/navigation";
import { PlatformTenantDetails } from "@/components/platform/platform-tenant-details";
import { isLocale } from "@/i18n/config";
export default async function Page({ params }: { params: Promise<{ locale: string; id: string }> }) { const { locale, id } = await params; if (!isLocale(locale)) notFound(); return <PlatformTenantDetails locale={locale} tenantId={id} />; }
