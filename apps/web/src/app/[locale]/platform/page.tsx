import { notFound } from "next/navigation";
import { PlatformDashboard } from "@/components/platform/platform-dashboard";
import { isLocale } from "@/i18n/config";
export default async function Page({ params }: { params: Promise<{ locale: string }> }) { const { locale } = await params; if (!isLocale(locale)) notFound(); return <PlatformDashboard locale={locale} />; }
