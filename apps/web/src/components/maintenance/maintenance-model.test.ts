import {describe,expect,it} from "vitest";
import {actionsFor,validBreakdown,validMaintenanceReport,validPreventivePlan} from "./maintenance-model";
describe("corrective maintenance workflow",()=>{
  it("validates complete breakdowns",()=>{expect(validBreakdown({assetId:"a",title:"Failure",description:"Stopped",priority:3})).toBe(true);expect(validBreakdown({assetId:"",title:"",description:"",priority:7})).toBe(false)});
  it("requires a chronological complete maintenance report",()=>{expect(validMaintenanceReport({diagnosis:"Bearing",workPerformed:"Replaced",repairOutcome:"0",actualStartUtc:"2026-01-01T10:00",actualEndUtc:"2026-01-01T11:00"})).toBe(true);expect(validMaintenanceReport({diagnosis:"",workPerformed:"",repairOutcome:"",actualStartUtc:"2026-01-01T12:00",actualEndUtc:"2026-01-01T11:00"})).toBe(false)});
  it("derives actions from workflow state and permissions",()=>{const p=["maintenance.workorders.assign","maintenance.workorders.execute","maintenance.workorders.verify","maintenance.workorders.cancel"];expect(actionsFor(1,p,true).start).toBe(true);expect(actionsFor(3,p,false).verify).toBe(true);expect(actionsFor(5,p,true).cancel).toBe(false)});
});
describe("preventive maintenance workflow",()=>{
  it("validates recurrence and ordered checklist input",()=>{const item={sequence:1,instructionEn:"Inspect filter",isRequired:true,isActive:true};expect(validPreventivePlan({code:"PM-1",nameEn:"Monthly",assetId:"asset",startLocalDate:"2026-01-31",scheduledLocalTime:"09:00",recurrenceInterval:1,checklist:[item]})).toBe(true);expect(validPreventivePlan({code:"",nameEn:"",assetId:"",startLocalDate:"",scheduledLocalTime:"",recurrenceInterval:0,checklist:[{...item,sequence:2}]})).toBe(false)});
  it("uses PM permissions for preventive work orders",()=>{const permissions=["maintenance.pm.manage","maintenance.pm.execute","maintenance.pm.verify"];expect(actionsFor(1,permissions,true,1).start).toBe(true);expect(actionsFor(3,permissions,false,1).verify).toBe(true);expect(actionsFor(1,["maintenance.workorders.execute"],true,1).start).toBe(false)});
});
