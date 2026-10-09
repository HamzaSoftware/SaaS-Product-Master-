import {describe,it,expect} from "vitest";
import {validPart,validQuantity,weightedAverage} from "./inventory-model";
describe("inventory model",()=>{
  it("validates catalog fields and quantities",()=>{expect(validPart({sku:"FLT-1",nameEn:"Filter",minimumStock:0,reorderPoint:2})).toBe(true);expect(validPart({sku:"",nameEn:"",minimumStock:-1,reorderPoint:0})).toBe(false);expect(validQuantity(0)).toBe(false);expect(validQuantity(1.25)).toBe(true)});
  it("calculates moving weighted average",()=>{expect(weightedAverage(10,20,10,30)).toBe(25);expect(weightedAverage(10,20,0,30)).toBeNull()});
});
