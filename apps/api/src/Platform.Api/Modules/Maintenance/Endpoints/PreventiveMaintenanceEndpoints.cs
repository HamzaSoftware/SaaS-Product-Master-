using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Maintenance.Application;
using SaaSPlatform.Api.Modules.Maintenance.Domain;

namespace SaaSPlatform.Api.Modules.Maintenance.Endpoints;

public static class PreventiveMaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapPreventiveMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/preventive-maintenance").WithTags("Preventive maintenance");
        group.MapGet("/assets", async (string? search, IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(await service.Assets(user, search, ct))).RequireAuthorization(TenantPermissions.MaintenancePmCreate);
        group.MapGet("/plans", async (string? search, PreventivePlanState? state, Guid? siteId, Guid? assetId, int? page, int? pageSize,
            IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var result = await service.List(user, new(search, state, siteId, assetId, page ?? 1, pageSize ?? 25), ct);
            return Results.Ok(new { Items = result.Items.Select(MapPlan), result.Page, result.PageSize, result.Total });
        }).RequireAuthorization(TenantPermissions.MaintenancePmView);
        group.MapGet("/plans/{id:guid}", async (Guid id, IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapPlan(await service.GetPlan(user, id, ct)))).RequireAuthorization(TenantPermissions.MaintenancePmView);
        group.MapPost("/plans", async ([FromBody] PreventivePlanInput input, IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var plan = await service.Create(user, input, ct); return Results.Created($"/api/v1/preventive-maintenance/plans/{plan.Id}", MapPlan(plan));
        }).RequireAuthorization(TenantPermissions.MaintenancePmCreate);
        group.MapPut("/plans/{id:guid}", async (Guid id, [FromBody] PreventivePlanInput input, IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapPlan(await service.Update(user, id, input, ct)))).RequireAuthorization(TenantPermissions.MaintenancePmUpdate);
        group.MapPatch("/plans/{id:guid}/status", async (Guid id, [FromBody] PreventivePlanStatusInput input, IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapPlan(await service.ChangeStatus(user, id, input, ct)))).RequireAuthorization(TenantPermissions.MaintenancePmManage);
        group.MapGet("/plans/{id:guid}/occurrences", async (Guid id, DateTimeOffset fromUtc, DateTimeOffset toUtc, IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok((await service.Occurrences(user, id, fromUtc, toUtc, ct)).Select(MapOccurrence))).RequireAuthorization(TenantPermissions.MaintenancePmView);
        group.MapPost("/generate", async (DateTimeOffset? throughUtc, IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(await service.Generate(user, throughUtc, ct))).RequireAuthorization(TenantPermissions.MaintenancePmGenerate);
        group.MapGet("/calendar", async (DateTimeOffset fromUtc, DateTimeOffset toUtc, Guid? siteId, Guid? assetId,
            IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok((await service.Calendar(user, new(fromUtc, toUtc, siteId, assetId), ct)).Select(MapOccurrence))).RequireAuthorization(TenantPermissions.MaintenancePmView);
        group.MapGet("/dashboard", async (IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(await service.Dashboard(user, ct))).RequireAuthorization(TenantPermissions.MaintenancePmView);
        group.MapGet("/work-orders", async (string? search, MaintenancePriority? priority, CorrectiveWorkOrderStatus? status, Guid? siteId, Guid? assetId,
            Guid? assignedTechnicianId, bool? mine, int? page, int? pageSize, IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var result = await service.ListWorkOrders(user, new(search, priority, status, siteId, assetId, assignedTechnicianId, mine ?? false, page ?? 1, pageSize ?? 25), ct);
            return Results.Ok(new { Items = result.Items.Select(CorrectiveMaintenanceEndpoints.MapWorkOrder), result.Page, result.PageSize, result.Total });
        }).RequireAuthorization(TenantPermissions.MaintenancePmView);
        group.MapGet("/technicians", async (Guid siteId, IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(await service.Technicians(user, siteId, ct))).RequireAuthorization(TenantPermissions.MaintenancePmManage);
        endpoints.MapPut("/api/v1/work-orders/{id:guid}/preventive-checklist", async (Guid id, [FromBody] PreventiveChecklistResponseInput input,
            IPreventiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(CorrectiveMaintenanceEndpoints.MapWorkOrder(await service.RecordChecklist(user, id, input, ct))))
            .RequireAuthorization(TenantPermissions.MaintenancePmExecute).WithTags("Preventive maintenance");
        return endpoints;
    }

    private static object MapPlan(PreventiveMaintenancePlan x) => new { x.Id, x.Code, x.NameEn, x.NameAr, x.Description, x.AssetId,
        AssetCode = x.Asset?.Code, AssetNameEn = x.Asset?.NameEn, AssetNameAr = x.Asset?.NameAr, x.SiteId, x.Priority,
        x.StartLocalDate, x.ScheduledLocalTime, x.TimeZoneId, x.RecurrenceType, x.RecurrenceInterval, x.NextDueAtUtc,
        x.LastGeneratedAtUtc, x.State, x.EstimatedDurationMinutes, x.DefaultTechnicianId,
        DefaultTechnicianName = x.DefaultTechnician?.DisplayName, x.CreatedById, CreatedByName = x.CreatedBy?.DisplayName,
        x.Version, x.CreatedAtUtc, x.UpdatedAtUtc,
        Checklist = x.ChecklistItems.OrderBy(i => i.Sequence).Select(i => new { i.Id, i.Sequence, i.InstructionEn, i.InstructionAr, i.IsRequired, i.Guidance, i.IsActive }),
        GeneratedWorkOrders = x.Occurrences.Where(o => o.WorkOrder != null).OrderByDescending(o => o.ScheduledDueAtUtc).Take(20).Select(o => new { o.Id, o.ScheduledDueAtUtc, o.Status, WorkOrderId = o.WorkOrder!.Id, WorkOrderNumber = o.WorkOrder.Number, WorkOrderStatus = o.WorkOrder.Status }) };

    private static object MapOccurrence(PreventiveMaintenanceOccurrence x) => new { x.Id, x.PlanId,
        PlanCode = x.Plan?.Code, PlanNameEn = x.Plan?.NameEn, PlanNameAr = x.Plan?.NameAr, AssetId = x.Plan?.AssetId,
        AssetCode = x.Plan?.Asset?.Code, AssetNameEn = x.Plan?.Asset?.NameEn, AssetNameAr = x.Plan?.Asset?.NameAr,
        SiteId = x.Plan?.SiteId, x.ScheduledDueAtUtc, x.Status, x.GeneratedAtUtc, x.WorkOrderId,
        WorkOrderNumber = x.WorkOrder?.Number, WorkOrderStatus = x.WorkOrder?.Status };
}
