import { notFound } from "next/navigation";
import { AdminPage } from "@/components/administration/admin-page";
import { isLocale } from "@/i18n/config";
export default async function Page({ params }: { params: Promise<{ locale: string }> }) { const { locale } = await params; if (!isLocale(locale)) notFound(); return <AdminPage locale={locale} kind="types" />; }
