import { notFound } from "next/navigation";
import { AssetDetails } from "@/components/assets/asset-pages";
import { AssetRelocation } from "@/components/assets/asset-relocation";
import { AssetMaintenanceHistory } from "@/components/maintenance/maintenance-pages";
import { AssetServiceRequests } from "@/components/service-requests/service-request-pages";
import { isLocale } from "@/i18n/config";
export default async function Page({params}:{params:Promise<{locale:string;id:string}>}){const {locale,id}=await params;if(!isLocale(locale))notFound();return <div className="page-stack"><AssetDetails locale={locale} id={id}/><AssetRelocation locale={locale} assetId={id}/><AssetMaintenanceHistory locale={locale} assetId={id}/><AssetServiceRequests locale={locale} assetId={id}/></div>}
