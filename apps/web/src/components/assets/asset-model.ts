export type AssetStatus = 0 | 1 | 2 | 3;
export type AssetLifecycle = 0 | 1;
export type Ref = { id: string; code?: string; name?: string; nameEn?: string; nameAr?: string; isActive: boolean; version: string; manufacturerId?: string; manufacturerName?: string; categoryId?: string; categoryNameEn?: string; country?: string; technicalDescription?: string };
export type Site = { id: string; code: string; nameEn: string; nameAr?: string; isActive: boolean };
export type Location = { id: string; siteId: string; nameEn: string; nameAr?: string; pathEn?: string; pathAr?: string; isActive: boolean };
export type Asset = { id: string; code: string; nameEn: string; nameAr?: string; categoryId: string; categoryNameEn: string; categoryNameAr?: string; siteId: string; siteNameEn: string; siteNameAr?: string; locationId?: string; locationNameEn?: string; locationNameAr?: string; manufacturerId?: string; manufacturerName?: string; modelId?: string; modelName?: string; serialNumber?: string; barcode?: string; description?: string; purchaseDate?: string; purchaseCost?: number; currency?: string; installationDate?: string; warrantyStartDate?: string; warrantyExpiryDate?: string; supplierName?: string; supplierReference?: string; responsibleUserId?: string; responsibleUserName?: string; notes?: string; operationalStatus: AssetStatus; lifecycleState: AssetLifecycle; retiredAtUtc?: string; version: string; createdAtUtc: string; updatedAtUtc: string };
export type Activity = { id: string; assetId: string; actorUserId?: string; action: string; changesJson?: string; correlationId?: string; occurredAtUtc: string };
export type Page<T> = { items: T[]; page: number; pageSize: number; total: number };

export const statusKeys = ["operational", "underMaintenance", "outOfService", "standby"] as const;
export function validateAssetDates(values: { purchaseDate?: string; installationDate?: string; warrantyStartDate?: string; warrantyExpiryDate?: string; purchaseCost?: number }) {
  if (values.purchaseCost !== undefined && values.purchaseCost < 0) return "cost";
  if (values.purchaseDate && values.installationDate && values.installationDate < values.purchaseDate) return "installation";
  if (values.warrantyStartDate && values.warrantyExpiryDate && values.warrantyExpiryDate < values.warrantyStartDate) return "warranty";
  return null;
}
export function totalPages(total: number, pageSize: number) { return Math.max(1, Math.ceil(total / pageSize)); }
