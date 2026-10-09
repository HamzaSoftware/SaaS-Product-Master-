import { describe, expect, it } from "vitest";
import { directionFor, isLocale } from "./config";
import { getDictionary, sectionKeys } from "./dictionaries";

describe("localization foundation", () => {
  it("maps Arabic to RTL and English to LTR", () => {
    expect(directionFor("ar")).toBe("rtl");
    expect(directionFor("en")).toBe("ltr");
  });

  it("has every navigation label in both supported languages", () => {
    for (const locale of ["en", "ar"] as const) {
      expect(isLocale(locale)).toBe(true);
      for (const section of sectionKeys) {
        expect(getDictionary(locale).nav[section]).not.toHaveLength(0);
      }
    }
  });
});
