import {describe,expect,it} from "vitest";
import {canReviewRequisition,canSubmitRequisition,moneyTotal,remainingQuantity,validConversion} from "./procurement-model";
describe("procurement workflow model",()=>{
  it("calculates deterministic financial totals",()=>expect(moneyTotal(3,12.34567)).toBe(37.04));
  it("only submits populated drafts",()=>{expect(canSubmitRequisition(0,2)).toBe(true);expect(canSubmitRequisition(1,2)).toBe(false);expect(canSubmitRequisition(0,0)).toBe(false)});
  it("prevents requester self-review",()=>{expect(canReviewRequisition(1,"a","b")).toBe(true);expect(canReviewRequisition(1,"a","a")).toBe(false)});
  it("protects conversion and receipt bounds",()=>{expect(validConversion(100,60,40)).toBe(true);expect(validConversion(100,60,41)).toBe(false);expect(remainingQuantity(100,35)).toBe(65)});
});
