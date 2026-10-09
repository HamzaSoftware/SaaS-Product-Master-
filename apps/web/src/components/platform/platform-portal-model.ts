import { temporaryPasswordIsValid } from "../administration/access-model";
import { ApiError } from "../../lib/platform-api";

export type TenantDraft = {
  name: string;
  slug: string;
  timeZone: string;
  defaultLanguage: "en" | "ar";
  adminEmail: string;
  adminDisplayName: string;
  adminPassword: string;
};

export function normalizeTenantSlug(value: string) {
  return value.toLowerCase().trim().replace(/\s+/g, "-").replace(/-+/g, "-");
}

export function validateTenantDraft(draft: TenantDraft): "required" | "slug" | "password" | null {
  if (![draft.name, draft.slug, draft.timeZone, draft.adminEmail, draft.adminDisplayName, draft.adminPassword].every((value) => value.trim())) return "required";
  if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(draft.slug)) return "slug";
  if (!temporaryPasswordIsValid(draft.adminPassword)) return "password";
  return null;
}

export function tenantCounts(tenants: { isActive: boolean }[]) {
  const active = tenants.filter((tenant) => tenant.isActive).length;
  return { total: tenants.length, active, inactive: tenants.length - active };
}

export function platformLoginFailure(error: unknown): "credentials" | "service" {
  return error instanceof ApiError && error.status === 401 ? "credentials" : "service";
}
