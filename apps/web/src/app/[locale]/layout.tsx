import { notFound } from "next/navigation";
import { AppShell } from "@/components/navigation/app-shell";
import { directionFor, isLocale, locales } from "@/i18n/config";

export function generateStaticParams() {
  return locales.map((locale) => ({ locale }));
}

export default async function LocaleLayout({
  children,
  params,
}: {
  children: React.ReactNode;
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (!isLocale(locale)) notFound();

  return <div lang={locale} dir={directionFor(locale)}><AppShell locale={locale}>{children}</AppShell></div>;
}
