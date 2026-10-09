import {notFound} from "next/navigation";import {WorkOrderDetails} from "@/components/maintenance/maintenance-pages";import {isLocale} from "@/i18n/config";
export default async function Page({params}:{params:Promise<{locale:string;id:string}>}){const {locale,id}=await params;if(!isLocale(locale))notFound();return <WorkOrderDetails locale={locale} id={id}/>}
