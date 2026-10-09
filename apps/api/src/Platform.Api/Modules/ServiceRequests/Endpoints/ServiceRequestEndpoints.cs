using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.ServiceRequests.Application;
using SaaSPlatform.Api.Modules.ServiceRequests.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.ServiceRequests.Endpoints;

public static class ServiceRequestEndpoints
{
    public static IEndpointRouteBuilder MapServiceRequestEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/service-requests").WithTags("Service requests").RequireAuthorization("PlatformAccess");
        group.MapGet("/categories", async (bool? activeOnly, IServiceRequestService service, CancellationToken ct) => Results.Ok(await service.Categories(activeOnly ?? false, ct)));
        group.MapPost("/categories", async ([FromBody] ServiceRequestCategoryInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => { var x = await service.SaveCategory(user, null, input, ct); return Results.Created($"/api/v1/service-requests/categories/{x.Id}", x); });
        group.MapPut("/categories/{id:guid}", async (Guid id, [FromBody] ServiceRequestCategoryInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(await service.SaveCategory(user, id, input, ct)));
        group.MapGet("/dashboard", async (IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(await service.Dashboard(user, ct)));
        group.MapGet("/assignees", async (Guid siteId, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(await service.Assignees(user, siteId, ct)));
        group.MapGet("/", async (string? search, ServiceRequestStatus? status, ServiceRequestPriority? priority, Guid? siteId, Guid? categoryId,
            Guid? assetId, bool? mine, bool? assignedToMe, bool? unassigned, DateOnly? createdFrom, DateOnly? createdTo,
            int? page, int? pageSize, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var own = mine ?? false; var result = await service.List(user, new(search, status, priority, siteId, categoryId, assetId, own, assignedToMe ?? false, page ?? 1, pageSize ?? 25, unassigned, createdFrom, createdTo), ct);
            return Results.Ok(new { Items = result.Items.Select(x => Map(x, own)), result.Page, result.PageSize, result.Total });
        });
        group.MapGet("/{id:guid}", async (Guid id, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) =>
        { var x = await service.GetDetails(user, id, ct); return Results.Ok(Map(x, !user.HasClaim(TenantClaims.Permission, TenantPermissions.ServiceRequestsView))); });
        group.MapGet("/{id:guid}/similar", async (Guid id, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok((await service.Similar(user, id, ct)).Select(x => Map(x, true))));
        group.MapPost("/", async ([FromBody] ServiceRequestInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => { var x = await service.Create(user, input, ct); return Results.Created($"/api/v1/service-requests/{x.Id}", Map(x, true)); });
        group.MapPost("/{id:guid}/submit", async (Guid id, [FromBody] ServiceRequestVersionInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Submit(user, id, input, ct), true)));
        group.MapPatch("/{id:guid}/triage", async (Guid id, [FromBody] ServiceRequestTriageInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Triage(user, id, input, ct), false)));
        group.MapPatch("/{id:guid}/assignment", async (Guid id, [FromBody] ServiceRequestAssignmentInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Assign(user, id, input, ct), false)));
        group.MapPost("/{id:guid}/start", async (Guid id, [FromBody] ServiceRequestVersionInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Start(user, id, input, ct), false)));
        group.MapPost("/{id:guid}/notes", async (Guid id, [FromBody] ServiceRequestNoteInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Note(user, id, input, ct), false)));
        group.MapPost("/{id:guid}/resolve", async (Guid id, [FromBody] ServiceRequestResolutionInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Resolve(user, id, input, ct), false)));
        group.MapPost("/{id:guid}/close", async (Guid id, [FromBody] ServiceRequestVersionInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Close(user, id, input, ct), false)));
        group.MapPost("/{id:guid}/reopen", async (Guid id, [FromBody] ServiceRequestReasonInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Reopen(user, id, input, ct), false)));
        group.MapPost("/{id:guid}/reject", async (Guid id, [FromBody] ServiceRequestReasonInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Reject(user, id, input, ct), false)));
        group.MapPost("/{id:guid}/cancel", async (Guid id, [FromBody] ServiceRequestReasonInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Cancel(user, id, input, ct), false)));
        group.MapPost("/{id:guid}/convert-to-maintenance", async (Guid id, [FromBody] ServiceRequestVersionInput input, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.ConvertToMaintenance(user, id, input, ct), false)));

        endpoints.MapGet("/api/v1/assets/{assetId:guid}/service-requests", async (Guid assetId, IServiceRequestService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok((await service.AssetHistory(user, assetId, ct)).Select(x => Map(x, false))))
            .RequireAuthorization("PlatformAccess").WithTags("Service requests");
        return endpoints;
    }

    private static object Map(ServiceRequest x, bool requesterView)
    {
        var notes = x.Notes.Where(n => !requesterView || n.Visibility == ServiceRequestNoteVisibility.RequesterVisible);
        return new { x.Id, x.Number, x.SiteId, SiteNameEn = x.Site?.NameEn, SiteNameAr = x.Site?.NameAr, x.LocationId,
            LocationNameEn = x.Location?.NameEn, LocationNameAr = x.Location?.NameAr, x.CategoryId, CategoryCode = x.Category?.Code,
            CategoryNameEn = x.Category?.NameEn, CategoryNameAr = x.Category?.NameAr, x.RequesterId, RequesterName = x.Requester?.DisplayName,
            x.RequesterContact, x.AssetId, AssetCode = x.Asset?.Code, AssetNameEn = x.Asset?.NameEn, AssetNameAr = x.Asset?.NameAr,
            x.Title, x.Description, x.Priority, x.Status, x.AssignedToId, AssignedToName = x.AssignedTo?.DisplayName,
            x.SubmittedAtUtc, x.FirstResponseAtUtc, x.AssignedAtUtc, x.WorkStartedAtUtc, x.ResolvedAtUtc, x.ClosedAtUtc,
            x.ResolutionSummary, x.TerminalReason, x.LinkedBreakdownReportId, LinkedBreakdownNumber = x.LinkedBreakdownReport?.Number,
            LinkedWorkOrderId = x.LinkedBreakdownReport?.WorkOrder?.Id, LinkedWorkOrderNumber = x.LinkedBreakdownReport?.WorkOrder?.Number, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc,
            Notes = notes.OrderByDescending(n => n.CreatedAtUtc).Select(n => new { n.Id, n.AuthorId, AuthorName = n.Author?.DisplayName, n.Content, n.Visibility, n.CreatedAtUtc }),
            Activities = x.Activities.Where(_ => !requesterView).OrderByDescending(a => a.OccurredAtUtc).Select(a => new { a.Id, a.ActorUserId, ActorName = a.ActorUser?.DisplayName, a.Action, a.FromStatus, a.ToStatus, a.Details, a.OccurredAtUtc }),
            Assignments = x.Assignments.Where(_ => !requesterView).OrderByDescending(a => a.AssignedAtUtc).Select(a => new { a.Id, a.AssigneeId, AssigneeName = a.Assignee?.DisplayName, a.AssignedById, AssignedByName = a.AssignedBy?.DisplayName, a.AssignedAtUtc, a.Note }) };
    }
}
