using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Maintenance.Application;
using SaaSPlatform.Api.Modules.Maintenance.Domain;

namespace SaaSPlatform.Api.Modules.Maintenance.Endpoints;

public static class CorrectiveMaintenanceEndpoints
{
    private sealed record VersionInput(Guid Version);

    public static IEndpointRouteBuilder MapCorrectiveMaintenanceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var breakdowns = endpoints.MapGroup("/api/v1/breakdowns").WithTags("Corrective maintenance");
        breakdowns.MapGet("/assets", async (string? search, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(await service.BreakdownAssets(user, search, ct))).RequireAuthorization(TenantPermissions.MaintenanceBreakdownsCreate);
        breakdowns.MapGet("/", async (string? search, MaintenancePriority? priority, BreakdownStatus? status, Guid? siteId, Guid? assetId,
            int? page, int? pageSize, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var result = await service.ListBreakdowns(user, new(search, priority, status, siteId, assetId, page ?? 1, pageSize ?? 25), ct);
            return Results.Ok(new { Items = result.Items.Select(MapBreakdown), result.Page, result.PageSize, result.Total });
        }).RequireAuthorization(TenantPermissions.MaintenanceBreakdownsView);
        breakdowns.MapGet("/{id:guid}", async (Guid id, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapBreakdown(await service.GetBreakdown(user, id, ct)))).RequireAuthorization(TenantPermissions.MaintenanceBreakdownsView);
        breakdowns.MapPost("/", async ([FromBody] BreakdownCreateInput input, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var created = await service.CreateBreakdown(user, input, ct); return Results.Created($"/api/v1/breakdowns/{created.Id}", MapBreakdown(created));
        }).RequireAuthorization(TenantPermissions.MaintenanceBreakdownsCreate);
        breakdowns.MapPost("/{id:guid}/review", async (Guid id, [FromBody] BreakdownReviewInput input, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapBreakdown(await service.ReviewBreakdown(user, id, input, ct)))).RequireAuthorization(TenantPermissions.MaintenanceBreakdownsReview);

        var orders = endpoints.MapGroup("/api/v1/work-orders").WithTags("Corrective maintenance");
        orders.MapGet("/", async (string? search, MaintenancePriority? priority, CorrectiveWorkOrderStatus? status, Guid? siteId, Guid? assetId,
            Guid? assignedTechnicianId, bool? mine, int? page, int? pageSize, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var result = await service.ListWorkOrders(user, new(search, priority, status, siteId, assetId, assignedTechnicianId, mine ?? false, page ?? 1, pageSize ?? 25), ct);
            return Results.Ok(new { Items = result.Items.Select(MapWorkOrder), result.Page, result.PageSize, result.Total });
        }).RequireAuthorization(TenantPermissions.MaintenanceWorkOrdersView);
        orders.MapGet("/dashboard", async (ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(await service.Dashboard(user, ct))).RequireAuthorization(TenantPermissions.MaintenanceWorkOrdersView);
        orders.MapGet("/technicians", async (Guid siteId, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(await service.Technicians(user, siteId, ct))).RequireAuthorization(TenantPermissions.MaintenanceWorkOrdersAssign);
        orders.MapGet("/{id:guid}", async (Guid id, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapWorkOrder(await service.GetWorkOrder(user, id, ct)))).RequireAuthorization("PlatformAccess");
        orders.MapPost("/", async ([FromBody] WorkOrderCreateInput input, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var created = await service.CreateWorkOrder(user, input, ct); return Results.Created($"/api/v1/work-orders/{created.Id}", MapWorkOrder(created));
        }).RequireAuthorization(TenantPermissions.MaintenanceWorkOrdersCreate);
        orders.MapPatch("/{id:guid}/assignment", async (Guid id, [FromBody] WorkOrderAssignmentInput input, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapWorkOrder(await service.Assign(user, id, input, ct)))).RequireAuthorization("PlatformAccess");
        orders.MapPost("/{id:guid}/start", async (Guid id, [FromBody] VersionInput input, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapWorkOrder(await service.Start(user, id, input.Version, ct)))).RequireAuthorization("PlatformAccess");
        orders.MapPost("/{id:guid}/activities", async (Guid id, [FromBody] WorkOrderActivityInput input, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapWorkOrder(await service.AddActivity(user, id, input, ct)))).RequireAuthorization("PlatformAccess");
        orders.MapPut("/{id:guid}/maintenance-report", async (Guid id, [FromBody] MaintenanceReportInput input, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapWorkOrder(await service.SaveReport(user, id, input, ct)))).RequireAuthorization("PlatformAccess");
        orders.MapPost("/{id:guid}/maintenance-report/submit", async (Guid id, [FromBody] VersionInput input, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapWorkOrder(await service.SubmitReport(user, id, input.Version, ct)))).RequireAuthorization("PlatformAccess");
        orders.MapPost("/{id:guid}/verification", async (Guid id, [FromBody] WorkOrderVerificationInput input, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapWorkOrder(await service.Verify(user, id, input, ct)))).RequireAuthorization("PlatformAccess");
        orders.MapPost("/{id:guid}/cancel", async (Guid id, [FromBody] WorkOrderCancelInput input, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapWorkOrder(await service.Cancel(user, id, input, ct)))).RequireAuthorization("PlatformAccess");

        endpoints.MapGet("/api/v1/assets/{assetId:guid}/maintenance-history", async (Guid assetId, ICorrectiveMaintenanceService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok((await service.AssetHistory(user, assetId, ct)).Select(MapWorkOrder)))
            .RequireAuthorization(TenantPermissions.MaintenanceWorkOrdersView).WithTags("Corrective maintenance");
        return endpoints;
    }

    private static object MapBreakdown(BreakdownReport x) => new { x.Id, x.Number, x.AssetId, AssetCode = x.Asset?.Code,
        AssetNameEn = x.Asset?.NameEn, AssetNameAr = x.Asset?.NameAr, x.SiteId, x.LocationSnapshot, x.ReportedById,
        ReportedByName = x.ReportedBy?.DisplayName, x.ReportedAtUtc, x.Title, x.Description, x.ObservedSymptoms,
        x.DowntimeStartedAtUtc, x.Priority, x.Status, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc,
        OriginatingServiceRequestId = x.OriginatingServiceRequest?.Id, OriginatingServiceRequestNumber = x.OriginatingServiceRequest?.Number,
        Reviews = x.Reviews.OrderByDescending(r => r.ReviewedAtUtc).Select(r => new { r.Id, r.ReviewerId, ReviewerName = r.Reviewer?.DisplayName, r.Approved, r.Reason, r.ReviewedAtUtc }) };

    internal static object MapWorkOrder(CorrectiveWorkOrder x) => new { x.Id, x.Number, x.Type, x.BreakdownReportId,
        x.PreventiveMaintenancePlanId, PreventivePlanCode = x.PreventiveMaintenancePlan?.Code,
        PreventivePlanNameEn = x.PreventiveMaintenancePlan?.NameEn, PreventivePlanNameAr = x.PreventiveMaintenancePlan?.NameAr,
        x.PreventiveMaintenanceOccurrenceId, x.ScheduledDueAtUtc,
        BreakdownNumber = x.BreakdownReport?.Number, x.AssetId, AssetCode = x.Asset?.Code, AssetNameEn = x.Asset?.NameEn,
        AssetNameAr = x.Asset?.NameAr, ResponsibleUserId = x.Asset?.ResponsibleUserId,
        ResponsibleUserName = x.Asset?.ResponsibleUser?.DisplayName, x.SiteId, x.LocationSnapshot, x.Priority, x.Description,
        x.Status, x.AssignedTechnicianId, AssignedTechnicianName = x.AssignedTechnician?.DisplayName, x.AssignedById,
        AssignedByName = x.AssignedBy?.DisplayName, x.AssignedAtUtc, x.PlannedStartUtc, x.ActualStartUtc,
        x.ActualCompletionUtc, x.ClosedAtUtc, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc,
        Assignments = x.Assignments.OrderByDescending(a => a.AssignedAtUtc).Select(a => new { a.Id, a.TechnicianId,
            TechnicianName = a.Technician?.DisplayName, a.AssignedById, AssignedByName = a.AssignedBy?.DisplayName, a.AssignedAtUtc, a.Note }),
        Activities = x.Activities.OrderByDescending(a => a.OccurredAtUtc).Select(a => new { a.Id, a.ActorUserId,
            ActorName = a.ActorUser?.DisplayName, a.Action, a.Details, a.MetadataJson, a.OccurredAtUtc }),
        MaintenanceReport = x.MaintenanceReport is null ? null : new { x.MaintenanceReport.Id, x.MaintenanceReport.TechnicianId,
            TechnicianName = x.MaintenanceReport.Technician?.DisplayName, x.MaintenanceReport.Diagnosis, x.MaintenanceReport.WorkPerformed,
            x.MaintenanceReport.RepairOutcome, x.MaintenanceReport.ActualStartUtc, x.MaintenanceReport.ActualEndUtc,
            x.MaintenanceReport.LaborMinutes, x.MaintenanceReport.AdditionalNotes, x.MaintenanceReport.SubmittedAtUtc,
            x.MaintenanceReport.Status, x.MaintenanceReport.Version,
            Reviews = x.MaintenanceReport.Reviews.OrderByDescending(r => r.ReviewedAtUtc).Select(r => new { r.Id, r.ReviewerId,
                ReviewerName = r.Reviewer?.DisplayName, r.Verified, r.Notes, r.ReviewedAtUtc }) },
        ChecklistResponses = x.ChecklistResponses.OrderBy(i => i.Sequence).Select(i => new { i.Id, i.SourceChecklistItemId, i.Sequence,
            i.InstructionEn, i.InstructionAr, i.IsRequired, i.Guidance, i.Result, i.Notes, i.RecordedById,
            RecordedByName = i.RecordedBy == null ? null : i.RecordedBy.DisplayName, i.RecordedAtUtc }) };
}
