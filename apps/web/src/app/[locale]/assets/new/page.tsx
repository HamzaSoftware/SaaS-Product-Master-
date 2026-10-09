import { notFound } from "next/navigation";
import { AssetEditor } from "@/components/assets/asset-pages";
import { Breadcrumbs, PageHeader } from "@/components/navigation/page-header";
import { getAssetDictionary } from "@/i18n/asset-dictionaries";
import { isLocale } from "@/i18n/config";
export default async function Page({params}:{params:Promise<{locale:string}>}){const {locale}=await params;if(!isLocale(locale))notFound();const t=getAssetDictionary(locale);return <div className="page-stack"><Breadcrumbs items={[{label:t.assets,href:`/${locale}/assets`},{label:t.add}]}/><PageHeader eyebrow={t.registry} title={t.add} description={locale==="ar"?"أنشئ سجل أصل مرتبطًا بالموقع والمكان والتصنيف الصحيح.":"Create a governed asset record with its classification and physical placement."}/><AssetEditor locale={locale}/></div>}
