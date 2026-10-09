import { notFound } from "next/navigation";
import { PlatformLoginForm } from "@/components/platform/platform-login-form";
import { isLocale } from "@/i18n/config";
export default async function Page({ params }: { params: Promise<{ locale: string }> }) { const { locale } = await params; if (!isLocale(locale)) notFound(); return <PlatformLoginForm locale={locale} />; }
