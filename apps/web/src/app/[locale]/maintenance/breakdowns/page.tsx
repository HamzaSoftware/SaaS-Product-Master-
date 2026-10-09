import {notFound} from "next/navigation";import {BreakdownList} from "@/components/maintenance/maintenance-pages";import {isLocale} from "@/i18n/config";
export default async function Page({params}:{params:Promise<{locale:string}>}){const {locale}=await params;if(!isLocale(locale))notFound();return <BreakdownList locale={locale}/>}
