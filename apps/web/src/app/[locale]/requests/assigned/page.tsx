import {RequestsList} from "@/components/service-requests/service-request-pages"; import {isLocale} from "@/i18n/config"; import {notFound} from "next/navigation";
export default async function Page({params}:{params:Promise<{locale:string}>}){const{locale}=await params;if(!isLocale(locale))notFound();return <RequestsList locale={locale} mode="assigned"/>}
