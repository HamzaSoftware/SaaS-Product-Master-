import { describe, expect, it } from "vitest";
import { buildAccessAssignments, temporaryPasswordIsValid } from "./access-model";

describe("access administration model", () => {
  it("builds tenant-wide assignments without browser-supplied tenant IDs", () => {
    expect(buildAccessAssignments(["role-1"], "tenant", ["ignored"])).toEqual([{ roleId: "role-1", scopeKind: 0, scopeId: null }]);
  });
  it("creates one assignment per unique role and site", () => {
    expect(buildAccessAssignments(["r", "r"], "site", ["a", "b", "a"])).toHaveLength(2);
  });
  it("mirrors the visible minimum password guidance", () => {
    expect(temporaryPasswordIsValid("Strong!Pass123")).toBe(true);
    expect(temporaryPasswordIsValid("weakpassword")).toBe(false);
  });
});
