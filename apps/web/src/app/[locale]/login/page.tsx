import { notFound } from "next/navigation";
import { LoginForm } from "@/components/auth/login-form";
import { isLocale } from "@/i18n/config";
export default async function Page({ params }: { params: Promise<{ locale: string }> }) { const { locale } = await params; if (!isLocale(locale)) notFound(); return <LoginForm locale={locale} />; }
