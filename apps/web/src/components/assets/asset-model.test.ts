import { describe, expect, it } from "vitest";
import { totalPages, validateAssetDates } from "./asset-model";
describe("asset form rules", () => {
  it("rejects invalid financial and date relationships", () => {
    expect(validateAssetDates({ purchaseCost: -1 })).toBe("cost");
    expect(validateAssetDates({ purchaseDate: "2026-02-01", installationDate: "2026-01-01" })).toBe("installation");
    expect(validateAssetDates({ warrantyStartDate: "2026-02-01", warrantyExpiryDate: "2026-01-01" })).toBe("warranty");
  });
  it("accepts valid dates and calculates pagination", () => { expect(validateAssetDates({ purchaseCost: 0 })).toBeNull(); expect(totalPages(51, 25)).toBe(3); });
});
