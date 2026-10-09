import { describe, expect, it } from "vitest";
import { buildLocationTree, displayLocationName, type LocationItem } from "./tree";

const root: LocationItem = { id: "1", parentLocationId: null, code: "BLD", nameEn: "Building", nameAr: "المبنى", sortOrder: 10, isActive: true };

describe("location tree", () => {
  it("nests children and orders siblings", () => {
    const tree = buildLocationTree([
      { ...root, id: "3", parentLocationId: "1", code: "B", sortOrder: 2 }, root,
      { ...root, id: "2", parentLocationId: "1", code: "A", sortOrder: 1 },
    ]);
    expect(tree).toHaveLength(1); expect(tree[0].children.map((x) => x.code)).toEqual(["A", "B"]);
  });
  it("uses Arabic when available and falls back to English", () => {
    expect(displayLocationName(root, "ar")).toBe("المبنى");
    expect(displayLocationName({ ...root, nameAr: null }, "ar")).toBe("Building");
  });
});
