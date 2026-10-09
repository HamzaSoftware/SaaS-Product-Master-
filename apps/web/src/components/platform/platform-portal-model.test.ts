import { describe, expect, it } from "vitest";
import { ApiError } from "../../lib/platform-api";
import { normalizeTenantSlug, platformLoginFailure, tenantCounts, validateTenantDraft } from "./platform-portal-model";

const validDraft = { name: "Acme", slug: "acme", timeZone: "Asia/Riyadh", defaultLanguage: "en" as const,
  adminEmail: "admin@example.test", adminDisplayName: "Tenant Admin", adminPassword: "Strong!Pass123" };

describe("platform portal model", () => {
  it("normalizes human-readable slugs", () => expect(normalizeTenantSlug("  Acme   North  ")).toBe("acme-north"));
  it("rejects malformed slugs", () => expect(validateTenantDraft({ ...validDraft, slug: "Acme_1" })).toBe("slug"));
  it("rejects passwords that do not meet Identity guidance", () => expect(validateTenantDraft({ ...validDraft, adminPassword: "weak" })).toBe("password"));
  it("summarizes tenant lifecycle state", () => expect(tenantCounts([{ isActive: true }, { isActive: false }])).toEqual({ total: 2, active: 1, inactive: 1 }));
  it("keeps invalid credentials generic", () => expect(platformLoginFailure(new ApiError("Invalid credentials.", 401))).toBe("credentials"));
  it("distinguishes connectivity and server failures", () => {
    expect(platformLoginFailure(new TypeError("fetch failed"))).toBe("service");
    expect(platformLoginFailure(new ApiError("Unavailable", 503))).toBe("service");
  });
});
