import {RequestDetails} from "@/components/service-requests/service-request-pages"; import {isLocale} from "@/i18n/config"; import {notFound} from "next/navigation";
export default async function Page({params}:{params:Promise<{locale:string;id:string}>}){const{locale,id}=await params;if(!isLocale(locale))notFound();return <RequestDetails locale={locale} id={id}/>}
