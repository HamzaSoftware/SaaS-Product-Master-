import { notFound } from "next/navigation";
import { AssetMasterData } from "@/components/assets/asset-pages";
import { isLocale } from "@/i18n/config";
export default async function Page({params}:{params:Promise<{locale:string}>}){const {locale}=await params;if(!isLocale(locale))notFound();return <AssetMasterData locale={locale}/>}
