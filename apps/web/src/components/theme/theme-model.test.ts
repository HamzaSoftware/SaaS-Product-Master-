import { describe, expect, it } from "vitest";
import { parseThemePreference, resolveThemePreference } from "./theme-model";
describe("theme model", () => {
  it("accepts supported persisted preferences", () => expect(parseThemePreference("dark")).toBe("dark"));
  it("falls back safely for unknown persisted values", () => expect(parseThemePreference("sepia")).toBe("system"));
  it("resolves system preference without changing the stored choice", () => expect(resolveThemePreference("system", true)).toBe("dark"));
});
