import {describe,expect,it} from "vitest";
import {availableActions,validRequest} from "./service-request-model";
describe("service request rules",()=>{
  it("enforces category requirements",()=>{const input={siteId:"s",categoryId:"c",title:"Leak",description:"Ceiling leak"};expect(validRequest(input,{assetRequired:true,locationRequired:true} as never,"","")).toBe(false);expect(validRequest(input,{assetRequired:true,locationRequired:true} as never,"l","a")).toBe(true)});
  it("only exposes valid lifecycle actions",()=>{const a=availableActions(2,["service_requests.assign","service_requests.convert_to_maintenance"],false,false);expect(a.assign).toBe(true);expect(a.convert).toBe(true);expect(a.resolve).toBe(false)});
  it("allows requester cancellation only before triage",()=>{expect(availableActions(1,[],false,true).cancel).toBe(true);expect(availableActions(2,[],false,true).cancel).toBe(false)});
});
