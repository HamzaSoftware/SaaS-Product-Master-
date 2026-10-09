using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Assets.Domain;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Maintenance.Application;
using SaaSPlatform.Api.Modules.Maintenance.Domain;
using SaaSPlatform.Api.Modules.Procurement.Application;
using SaaSPlatform.Api.Modules.ServiceRequests.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.ServiceRequests.Application;

public sealed record ServiceRequestPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public sealed record ServiceRequestCategoryInput(string Code, string NameEn, string? NameAr, string? DescriptionEn,
    string? DescriptionAr, ServiceRequestPriority DefaultPriority, bool AssetRequired, bool LocationRequired,
    bool IsActive, int SortOrder, Guid? Version);
public sealed record ServiceRequestInput(Guid SiteId, Guid? LocationId, Guid CategoryId, Guid? AssetId,
    string Title, string Description, string? RequesterContact, ServiceRequestPriority? Priority, bool Submit);
public sealed record ServiceRequestListQuery(string? Search, ServiceRequestStatus? Status, ServiceRequestPriority? Priority,
    Guid? SiteId, Guid? CategoryId, Guid? AssetId, bool Mine, bool AssignedToMe, int Page, int PageSize,
    bool? Unassigned = null, DateOnly? CreatedFrom = null, DateOnly? CreatedTo = null);
public sealed record ServiceRequestTriageInput(Guid CategoryId, Guid? LocationId, Guid? AssetId,
    ServiceRequestPriority Priority, Guid Version);
public sealed record ServiceRequestAssignmentInput(Guid AssigneeId, string? Note, Guid Version);
public sealed record ServiceRequestNoteInput(string Content, ServiceRequestNoteVisibility Visibility, Guid Version);
public sealed record ServiceRequestResolutionInput(string ResolutionSummary, Guid Version);
public sealed record ServiceRequestReasonInput(string Reason, Guid Version);
public sealed record ServiceRequestVersionInput(Guid Version);
public sealed record ServiceRequestAssignee(Guid UserId, string DisplayName, string Email);
public sealed record ServiceRequestDashboard(int Open, int Unassigned, int AssignedToMe, int Urgent, int ResolvedAwaitingClosure);

public interface IServiceRequestService
{
    Task<IReadOnlyList<ServiceRequestCategory>> Categories(bool activeOnly, CancellationToken ct);
    Task<ServiceRequestCategory> SaveCategory(ClaimsPrincipal user, Guid? id, ServiceRequestCategoryInput input, CancellationToken ct);
    Task<ServiceRequestPage<ServiceRequest>> List(ClaimsPrincipal user, ServiceRequestListQuery queryInput, CancellationToken ct);
    Task<ServiceRequest> GetDetails(ClaimsPrincipal user, Guid id, CancellationToken ct);
    Task<ServiceRequest> Create(ClaimsPrincipal user, ServiceRequestInput input, CancellationToken ct);
    Task<ServiceRequest> Submit(ClaimsPrincipal user, Guid id, ServiceRequestVersionInput input, CancellationToken ct);
    Task<ServiceRequest> Triage(ClaimsPrincipal user, Guid id, ServiceRequestTriageInput input, CancellationToken ct);
    Task<ServiceRequest> Assign(ClaimsPrincipal user, Guid id, ServiceRequestAssignmentInput input, CancellationToken ct);
    Task<ServiceRequest> Start(ClaimsPrincipal user, Guid id, ServiceRequestVersionInput input, CancellationToken ct);
    Task<ServiceRequest> Note(ClaimsPrincipal user, Guid id, ServiceRequestNoteInput input, CancellationToken ct);
    Task<ServiceRequest> Resolve(ClaimsPrincipal user, Guid id, ServiceRequestResolutionInput input, CancellationToken ct);
    Task<ServiceRequest> Close(ClaimsPrincipal user, Guid id, ServiceRequestVersionInput input, CancellationToken ct);
    Task<ServiceRequest> Reopen(ClaimsPrincipal user, Guid id, ServiceRequestReasonInput input, CancellationToken ct);
    Task<ServiceRequest> Reject(ClaimsPrincipal user, Guid id, ServiceRequestReasonInput input, CancellationToken ct);
    Task<ServiceRequest> Cancel(ClaimsPrincipal user, Guid id, ServiceRequestReasonInput input, CancellationToken ct);
    Task<ServiceRequest> ConvertToMaintenance(ClaimsPrincipal user, Guid id, ServiceRequestVersionInput input, CancellationToken ct);
    Task<IReadOnlyList<ServiceRequestAssignee>> Assignees(ClaimsPrincipal user, Guid siteId, CancellationToken ct);
    Task<ServiceRequestDashboard> Dashboard(ClaimsPrincipal user, CancellationToken ct);
    Task<IReadOnlyList<ServiceRequest>> AssetHistory(ClaimsPrincipal user, Guid assetId, CancellationToken ct);
    Task<IReadOnlyList<ServiceRequest>> Similar(ClaimsPrincipal user, Guid id, CancellationToken ct);
}

public sealed class ServiceRequestService(PlatformDbContext database, ICurrentTenant currentTenant,
    ISiteScopeAuthorizer scopes, IAuditWriter audit, ICorrectiveMaintenanceService maintenance,
    TimeProvider timeProvider) : IServiceRequestService
{
    private Guid TenantId => currentTenant.Id ?? throw new InvalidOperationException("Tenant context is required.");
    private DateTimeOffset Now => timeProvider.GetUtcNow();

    public async Task<IReadOnlyList<ServiceRequestCategory>> Categories(bool activeOnly, CancellationToken ct)
    {
        var query = database.ServiceRequestCategories.AsNoTracking();
        if (activeOnly) query = query.Where(x => x.IsActive);
        return await query.OrderBy(x => x.SortOrder).ThenBy(x => x.NameEn).ToListAsync(ct);
    }

    public async Task<ServiceRequestCategory> SaveCategory(ClaimsPrincipal user, Guid? id, ServiceRequestCategoryInput input, CancellationToken ct)
    {
        RequireTenant(user, TenantPermissions.ServiceRequestCategoriesManage);
        var code = Required(input.Code, "Code").ToUpperInvariant(); var name = Required(input.NameEn, "English name"); Valid(input.DefaultPriority);
        if (input.SortOrder < 0) throw new ValidationException("Sort order cannot be negative.");
        if (await database.ServiceRequestCategories.AnyAsync(x => x.Code == code && x.Id != id, ct)) throw new ConflictException("Category code already exists.");
        ServiceRequestCategory entity;
        if (id is null)
        {
            entity = new() { Id = Guid.NewGuid(), TenantId = TenantId, Code = code, NameEn = name };
            database.ServiceRequestCategories.Add(entity);
        }
        else
        {
            entity = await database.ServiceRequestCategories.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Service request category was not found.");
            Version(entity.Version, input.Version ?? Guid.Empty); entity.Code = code; entity.NameEn = name; entity.Version = Guid.NewGuid();
        }
        entity.NameAr = Clean(input.NameAr); entity.DescriptionEn = Clean(input.DescriptionEn); entity.DescriptionAr = Clean(input.DescriptionAr);
        entity.DefaultPriority = input.DefaultPriority; entity.AssetRequired = input.AssetRequired; entity.LocationRequired = input.LocationRequired;
        entity.IsActive = input.IsActive; entity.SortOrder = input.SortOrder;
        audit.Add(id is null ? "ServiceRequest.CategoryCreated" : "ServiceRequest.CategoryUpdated", nameof(ServiceRequestCategory), entity.Id, new { entity.Code });
        await Save(ct); return await database.ServiceRequestCategories.AsNoTracking().SingleAsync(x => x.Id == entity.Id, ct);
    }

    public async Task<ServiceRequestPage<ServiceRequest>> List(ClaimsPrincipal user, ServiceRequestListQuery queryInput, CancellationToken ct)
    {
        var actor = Actor(user); IQueryable<ServiceRequest> query;
        if (queryInput.Mine) { RequireAny(user, TenantPermissions.ServiceRequestsViewOwn); query = database.ServiceRequests.Where(x => x.RequesterId == actor); }
        else if (queryInput.AssignedToMe) { RequireAny(user, TenantPermissions.ServiceRequestsExecute); query = Accessible(user, TenantPermissions.ServiceRequestsExecute).Where(x => x.AssignedToId == actor); }
        else query = Accessible(user, TenantPermissions.ServiceRequestsView);
        query = query.AsNoTracking().IncludeRequest();
        if (!string.IsNullOrWhiteSpace(queryInput.Search)) { var term = $"%{queryInput.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Number, term) || EF.Functions.ILike(x.Title, term) || EF.Functions.ILike(x.Requester!.DisplayName, term) || (x.Asset != null && (EF.Functions.ILike(x.Asset.Code, term) || EF.Functions.ILike(x.Asset.NameEn, term))) || (x.Location != null && (EF.Functions.ILike(x.Location.Code, term) || EF.Functions.ILike(x.Location.NameEn, term)))); }
        if (queryInput.Status is { } status) query = query.Where(x => x.Status == status);
        if (queryInput.Priority is { } priority) query = query.Where(x => x.Priority == priority);
        if (queryInput.SiteId is { } site) query = query.Where(x => x.SiteId == site);
        if (queryInput.CategoryId is { } category) query = query.Where(x => x.CategoryId == category);
        if (queryInput.AssetId is { } asset) query = query.Where(x => x.AssetId == asset);
        if (queryInput.Unassigned is true) query = query.Where(x => x.AssignedToId == null);
        if (queryInput.CreatedFrom is { } from) { var start = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); query = query.Where(x => x.CreatedAtUtc >= start); }
        if (queryInput.CreatedTo is { } to) { var end = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero); query = query.Where(x => x.CreatedAtUtc < end); }
        var page = Math.Max(1, queryInput.Page); var size = Math.Clamp(queryInput.PageSize, 1, 100); var total = await query.CountAsync(ct);
        return new(await query.OrderByDescending(x => x.CreatedAtUtc).Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, total);
    }

    public async Task<ServiceRequest> GetDetails(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var entity = await Load(id, ct); var actor = Actor(user);
        if (entity.RequesterId != actor || !HasAny(user, TenantPermissions.ServiceRequestsViewOwn))
            RequireSite(user, TenantPermissions.ServiceRequestsView, entity.SiteId);
        return entity;
    }

    public async Task<ServiceRequest> Create(ClaimsPrincipal user, ServiceRequestInput input, CancellationToken ct)
    {
        RequireSite(user, TenantPermissions.ServiceRequestsCreate, input.SiteId);
        var category = await ValidateReferences(input.SiteId, input.LocationId, input.CategoryId, input.AssetId, true, ct);
        await using var transaction = await Transaction(ct);
        var entity = new ServiceRequest { Id = Guid.NewGuid(), TenantId = TenantId, Number = await NextNumber("SR", ct), SiteId = input.SiteId,
            LocationId = input.LocationId, CategoryId = category.Id, RequesterId = Actor(user), RequesterContact = Clean(input.RequesterContact), AssetId = input.AssetId,
            Title = Required(input.Title, "Title"), Description = Required(input.Description, "Description"), Priority = input.Priority ?? category.DefaultPriority,
            Status = input.Submit ? ServiceRequestStatus.Submitted : ServiceRequestStatus.Draft, SubmittedAtUtc = input.Submit ? Now : null };
        Valid(entity.Priority); database.ServiceRequests.Add(entity); Activity(entity, "ServiceRequest.Created", null, entity.Status, user);
        audit.Add("ServiceRequest.Created", nameof(ServiceRequest), entity.Id, new { entity.Number, entity.SiteId, entity.Status });
        try { await Save(ct); if (transaction is not null) await transaction.CommitAsync(ct); return await Load(entity.Id, ct); }
        catch { if (transaction is not null) await transaction.RollbackAsync(ct); throw; }
    }

    public async Task<ServiceRequest> Submit(ClaimsPrincipal user, Guid id, ServiceRequestVersionInput input, CancellationToken ct)
    {
        var entity = await Owned(id, user, ct); Version(entity.Version, input.Version);
        if (entity.Status != ServiceRequestStatus.Draft) throw new ConflictException("Only a draft request can be submitted.");
        await ValidateReferences(entity.SiteId, entity.LocationId, entity.CategoryId, entity.AssetId, true, ct);
        Change(entity, ServiceRequestStatus.Submitted, user); entity.SubmittedAtUtc = Now; await Save(ct); return await Load(id, ct);
    }

    public async Task<ServiceRequest> Triage(ClaimsPrincipal user, Guid id, ServiceRequestTriageInput input, CancellationToken ct)
    {
        var entity = await Request(id, ct); RequireSite(user, TenantPermissions.ServiceRequestsTriage, entity.SiteId); Version(entity.Version, input.Version);
        if (entity.Status is not (ServiceRequestStatus.Submitted or ServiceRequestStatus.UnderReview)) throw new ConflictException("Only submitted requests can be triaged.");
        await ValidateReferences(entity.SiteId, input.LocationId, input.CategoryId, input.AssetId, true, ct); Valid(input.Priority);
        entity.CategoryId = input.CategoryId; entity.LocationId = input.LocationId; entity.AssetId = input.AssetId; entity.Priority = input.Priority; entity.FirstResponseAtUtc ??= Now;
        Change(entity, ServiceRequestStatus.UnderReview, user); await Save(ct); return await Load(id, ct);
    }

    public async Task<ServiceRequest> Assign(ClaimsPrincipal user, Guid id, ServiceRequestAssignmentInput input, CancellationToken ct)
    {
        var entity = await Request(id, ct); RequireSite(user, TenantPermissions.ServiceRequestsAssign, entity.SiteId); Version(entity.Version, input.Version);
        if (entity.Status is not (ServiceRequestStatus.UnderReview or ServiceRequestStatus.Assigned)) throw new ConflictException("Only a triaged or assigned request can be assigned.");
        if (!await CanExecute(input.AssigneeId, entity.SiteId, ct)) throw new ValidationException("The assignee must be active and authorized to execute requests at this site.");
        var actor = Actor(user); var previous = entity.AssignedToId; entity.AssignedToId = input.AssigneeId; entity.AssignedById = actor; entity.AssignedAtUtc = Now;
        database.ServiceRequestAssignments.Add(new() { Id = Guid.NewGuid(), TenantId = TenantId, ServiceRequestId = id, PreviousAssigneeId = previous,
            AssigneeId = input.AssigneeId, AssignedById = actor, AssignedAtUtc = Now, Note = Clean(input.Note) });
        Change(entity, ServiceRequestStatus.Assigned, user, previous is null ? "Assigned" : "Reassigned"); await Save(ct); return await Load(id, ct);
    }

    public async Task<ServiceRequest> Start(ClaimsPrincipal user, Guid id, ServiceRequestVersionInput input, CancellationToken ct)
    {
        var entity = await Request(id, ct); RequireExecutor(user, entity); Version(entity.Version, input.Version);
        if (entity.Status != ServiceRequestStatus.Assigned) throw new ConflictException("Only an assigned request can be started.");
        Change(entity, ServiceRequestStatus.InProgress, user); entity.WorkStartedAtUtc ??= Now; await Save(ct); return await Load(id, ct);
    }

    public async Task<ServiceRequest> Note(ClaimsPrincipal user, Guid id, ServiceRequestNoteInput input, CancellationToken ct)
    {
        var entity = await Request(id, ct); Version(entity.Version, input.Version);
        if (input.Visibility == ServiceRequestNoteVisibility.Internal) RequireExecutorOr(user, entity, TenantPermissions.ServiceRequestsTriage);
        else if (entity.RequesterId != Actor(user)) RequireExecutorOr(user, entity, TenantPermissions.ServiceRequestsTriage);
        if (entity.Status is ServiceRequestStatus.Closed or ServiceRequestStatus.Rejected or ServiceRequestStatus.Cancelled or ServiceRequestStatus.ConvertedToMaintenance) throw new ConflictException("Notes cannot be added to a terminal request.");
        database.ServiceRequestNotes.Add(new() { Id = Guid.NewGuid(), TenantId = TenantId, ServiceRequestId = id, AuthorId = Actor(user), Content = Required(input.Content, "Note"), Visibility = input.Visibility, CreatedAtUtc = Now });
        entity.Version = Guid.NewGuid(); Activity(entity, "ServiceRequest.NoteAdded", entity.Status, entity.Status, user); await Save(ct); return await Load(id, ct);
    }

    public async Task<ServiceRequest> Resolve(ClaimsPrincipal user, Guid id, ServiceRequestResolutionInput input, CancellationToken ct)
    {
        var entity = await Request(id, ct); RequireExecutorOr(user, entity, TenantPermissions.ServiceRequestsResolve); Version(entity.Version, input.Version);
        if (entity.Status != ServiceRequestStatus.InProgress) throw new ConflictException("Only in-progress requests can be resolved.");
        entity.ResolutionSummary = Required(input.ResolutionSummary, "Resolution summary"); entity.ResolvedAtUtc = Now; entity.ResolvedById = Actor(user);
        Change(entity, ServiceRequestStatus.Resolved, user); await Save(ct); return await Load(id, ct);
    }

    public async Task<ServiceRequest> Close(ClaimsPrincipal user, Guid id, ServiceRequestVersionInput input, CancellationToken ct)
    {
        var entity = await Request(id, ct); RequireSite(user, TenantPermissions.ServiceRequestsClose, entity.SiteId); Version(entity.Version, input.Version);
        if (entity.Status != ServiceRequestStatus.Resolved) throw new ConflictException("Only a resolved request can be closed.");
        entity.ClosedAtUtc = Now; Change(entity, ServiceRequestStatus.Closed, user); await Save(ct); return await Load(id, ct);
    }

    public async Task<ServiceRequest> Reopen(ClaimsPrincipal user, Guid id, ServiceRequestReasonInput input, CancellationToken ct)
    {
        var entity = await Request(id, ct); RequireSite(user, TenantPermissions.ServiceRequestsClose, entity.SiteId); Version(entity.Version, input.Version);
        if (entity.Status != ServiceRequestStatus.Resolved) throw new ConflictException("Only a resolved request can be reopened.");
        var reason = Required(input.Reason, "Reopen reason"); entity.ResolvedAtUtc = null; entity.ResolvedById = null; entity.ResolutionSummary = null;
        Change(entity, ServiceRequestStatus.InProgress, user, reason); await Save(ct); return await Load(id, ct);
    }

    public async Task<ServiceRequest> Reject(ClaimsPrincipal user, Guid id, ServiceRequestReasonInput input, CancellationToken ct)
    {
        var entity = await Request(id, ct); RequireSite(user, TenantPermissions.ServiceRequestsTriage, entity.SiteId); Version(entity.Version, input.Version);
        if (entity.Status is not (ServiceRequestStatus.Submitted or ServiceRequestStatus.UnderReview)) throw new ConflictException("Only a submitted request can be rejected.");
        entity.TerminalReason = Required(input.Reason, "Rejection reason"); entity.ClosedAtUtc = Now; Change(entity, ServiceRequestStatus.Rejected, user, entity.TerminalReason); await Save(ct); return await Load(id, ct);
    }

    public async Task<ServiceRequest> Cancel(ClaimsPrincipal user, Guid id, ServiceRequestReasonInput input, CancellationToken ct)
    {
        var entity = await Request(id, ct); Version(entity.Version, input.Version); var actor = Actor(user);
        var requesterAllowed = entity.RequesterId == actor && entity.Status is ServiceRequestStatus.Draft or ServiceRequestStatus.Submitted;
        if (!requesterAllowed) RequireSite(user, TenantPermissions.ServiceRequestsTriage, entity.SiteId);
        if (entity.Status is not (ServiceRequestStatus.Draft or ServiceRequestStatus.Submitted or ServiceRequestStatus.UnderReview)) throw new ConflictException("This request can no longer be cancelled.");
        entity.TerminalReason = Required(input.Reason, "Cancellation reason"); entity.ClosedAtUtc = Now; Change(entity, ServiceRequestStatus.Cancelled, user, entity.TerminalReason); await Save(ct); return await Load(id, ct);
    }

    public async Task<ServiceRequest> ConvertToMaintenance(ClaimsPrincipal user, Guid id, ServiceRequestVersionInput input, CancellationToken ct)
    {
        var entity = await Request(id, ct); RequireSite(user, TenantPermissions.ServiceRequestsConvertToMaintenance, entity.SiteId); Version(entity.Version, input.Version);
        if (entity.Status != ServiceRequestStatus.UnderReview) throw new ConflictException("Only a triaged request can be converted to maintenance.");
        if (entity.AssetId is null) throw new ValidationException("An asset is required before conversion to corrective maintenance.");
        if (entity.LinkedBreakdownReportId is not null) throw new ConflictException("This request is already linked to corrective maintenance.");
        await using var transaction = await Transaction(ct);
        try
        {
            var breakdown = await maintenance.CreateBreakdownFromServiceRequest(user, new(entity.AssetId.Value, entity.Title, entity.Description, null, null, ToMaintenance(entity.Priority)), ct);
            entity.LinkedBreakdownReportId = breakdown.Id; Change(entity, ServiceRequestStatus.ConvertedToMaintenance, user, breakdown.Number);
            audit.Add("ServiceRequest.ConvertedToMaintenance", nameof(ServiceRequest), id, new { BreakdownReportId = breakdown.Id, breakdown.Number });
            await Save(ct); if (transaction is not null) await transaction.CommitAsync(ct); return await Load(id, ct);
        }
        catch { if (transaction is not null) await transaction.RollbackAsync(ct); throw; }
    }

    public async Task<IReadOnlyList<ServiceRequestAssignee>> Assignees(ClaimsPrincipal user, Guid siteId, CancellationToken ct)
    {
        RequireSite(user, TenantPermissions.ServiceRequestsAssign, siteId);
        return await database.TenantMemberships.AsNoTracking().Where(m => m.IsActive && m.User != null && m.User.IsActive &&
            m.Roles.Any(mr => mr.Role != null && mr.Role.IsActive && mr.Role.Permissions.Any(rp => rp.Permission != null && rp.Permission.Code == TenantPermissions.ServiceRequestsExecute) &&
                (mr.ScopeKind == AccessScopeKind.Tenant || mr.ScopeKind == AccessScopeKind.Site && mr.ScopeId == siteId)))
            .OrderBy(m => m.User!.DisplayName).Select(m => new ServiceRequestAssignee(m.UserId, m.User!.DisplayName, m.User.Email ?? "")).ToListAsync(ct);
    }

    public async Task<ServiceRequestDashboard> Dashboard(ClaimsPrincipal user, CancellationToken ct)
    {
        var actor = Actor(user); var query = Accessible(user, TenantPermissions.ServiceRequestsView).AsNoTracking();
        return new(await query.CountAsync(x => x.Status != ServiceRequestStatus.Closed && x.Status != ServiceRequestStatus.Rejected && x.Status != ServiceRequestStatus.Cancelled && x.Status != ServiceRequestStatus.ConvertedToMaintenance, ct),
            await query.CountAsync(x => x.Status == ServiceRequestStatus.UnderReview && x.AssignedToId == null, ct), await query.CountAsync(x => x.AssignedToId == actor && (x.Status == ServiceRequestStatus.Assigned || x.Status == ServiceRequestStatus.InProgress), ct),
            await query.CountAsync(x => x.Priority == ServiceRequestPriority.Urgent && x.Status != ServiceRequestStatus.Closed, ct), await query.CountAsync(x => x.Status == ServiceRequestStatus.Resolved, ct));
    }

    public async Task<IReadOnlyList<ServiceRequest>> AssetHistory(ClaimsPrincipal user, Guid assetId, CancellationToken ct)
    {
        var asset = await database.Assets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assetId, ct) ?? throw new ResourceNotFoundException("Asset was not found.");
        RequireSite(user, TenantPermissions.ServiceRequestsView, asset.SiteId);
        return await database.ServiceRequests.AsNoTracking().IncludeRequest().Where(x => x.AssetId == assetId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ServiceRequest>> Similar(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var entity = await GetDetails(user, id, ct); return await database.ServiceRequests.AsNoTracking().IncludeRequest()
            .Where(x => x.Id != id && x.SiteId == entity.SiteId && x.CategoryId == entity.CategoryId && x.Status != ServiceRequestStatus.Closed && x.Status != ServiceRequestStatus.Rejected && x.Status != ServiceRequestStatus.Cancelled)
            .OrderByDescending(x => x.CreatedAtUtc).Take(5).ToListAsync(ct);
    }

    private async Task<ServiceRequestCategory> ValidateReferences(Guid siteId, Guid? locationId, Guid categoryId, Guid? assetId, bool activeCategory, CancellationToken ct)
    {
        if (!await database.Sites.AnyAsync(x => x.Id == siteId && x.IsActive, ct)) throw new ValidationException("An active site in this tenant is required.");
        var category = await database.ServiceRequestCategories.SingleOrDefaultAsync(x => x.Id == categoryId, ct) ?? throw new ValidationException("A service request category in this tenant is required.");
        if (activeCategory && !category.IsActive) throw new ValidationException("The service request category is inactive.");
        if (category.LocationRequired && locationId is null) throw new ValidationException("A facility location is required for this category.");
        if (locationId is not null && !await database.LocationNodes.AnyAsync(x => x.Id == locationId && x.SiteId == siteId && x.IsActive, ct)) throw new ValidationException("The location must be active and belong to the selected site.");
        if (category.AssetRequired && assetId is null) throw new ValidationException("An asset is required for this category.");
        if (assetId is not null && !await database.Assets.AnyAsync(x => x.Id == assetId && x.SiteId == siteId && x.LifecycleState == AssetLifecycleState.Active, ct)) throw new ValidationException("The asset must be active and belong to the selected site.");
        return category;
    }

    private IQueryable<ServiceRequest> Accessible(ClaimsPrincipal user, string permission) => scopes.HasTenantAccess(user, permission) ? database.ServiceRequests : database.ServiceRequests.Where(x => GrantedSites(user, permission).Contains(x.SiteId));
    private async Task<ServiceRequest> Request(Guid id, CancellationToken ct) => await database.ServiceRequests.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Service request was not found.");
    private async Task<ServiceRequest> Owned(Guid id, ClaimsPrincipal user, CancellationToken ct) { var x = await Request(id, ct); if (x.RequesterId != Actor(user)) throw new UnauthorizedAccessException("Only the requester can perform this action."); return x; }
    private async Task<ServiceRequest> Load(Guid id, CancellationToken ct) => await database.ServiceRequests.AsNoTracking().IncludeRequest().SingleAsync(x => x.Id == id, ct);
    private void RequireExecutor(ClaimsPrincipal user, ServiceRequest entity) { RequireSite(user, TenantPermissions.ServiceRequestsExecute, entity.SiteId); if (entity.AssignedToId != Actor(user) && !scopes.HasAccess(user, TenantPermissions.ServiceRequestsAssign, entity.SiteId)) throw new UnauthorizedAccessException("Only the assignee or an authorized coordinator may execute this request."); }
    private void RequireExecutorOr(ClaimsPrincipal user, ServiceRequest entity, string permission) { if (scopes.HasAccess(user, permission, entity.SiteId)) return; RequireExecutor(user, entity); }
    private async Task<bool> CanExecute(Guid userId, Guid siteId, CancellationToken ct) => await database.TenantMemberships.AsNoTracking().AnyAsync(m => m.UserId == userId && m.IsActive && m.User != null && m.User.IsActive && m.Roles.Any(mr => mr.Role != null && mr.Role.IsActive && mr.Role.Permissions.Any(rp => rp.Permission != null && rp.Permission.Code == TenantPermissions.ServiceRequestsExecute) && (mr.ScopeKind == AccessScopeKind.Tenant || mr.ScopeKind == AccessScopeKind.Site && mr.ScopeId == siteId)), ct);
    private void Change(ServiceRequest entity, ServiceRequestStatus status, ClaimsPrincipal user, string? details = null) { var from = entity.Status; entity.Status = status; entity.Version = Guid.NewGuid(); Activity(entity, "ServiceRequest.StatusChanged", from, status, user, details); audit.Add("ServiceRequest.StatusChanged", nameof(ServiceRequest), entity.Id, new { From = from, To = status, details }); }
    private void Activity(ServiceRequest entity, string action, ServiceRequestStatus? from, ServiceRequestStatus? to, ClaimsPrincipal user, string? details = null) => database.ServiceRequestActivities.Add(new() { Id = Guid.NewGuid(), TenantId = TenantId, ServiceRequestId = entity.Id, ActorUserId = Actor(user), Action = action, FromStatus = from?.ToString(), ToStatus = to?.ToString(), Details = Clean(details), OccurredAtUtc = Now });
    private Task<string> NextNumber(string type, CancellationToken ct) => TenantDocumentNumber.Next(database, TenantId, Now, type, ct);
    private async Task<IDbContextTransaction?> Transaction(CancellationToken ct) => database.Database.IsRelational() && database.Database.CurrentTransaction is null ? await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
    private static MaintenancePriority ToMaintenance(ServiceRequestPriority p) => p switch { ServiceRequestPriority.Low => MaintenancePriority.Low, ServiceRequestPriority.Normal => MaintenancePriority.Medium, ServiceRequestPriority.High => MaintenancePriority.High, _ => MaintenancePriority.Critical };
    private void RequireSite(ClaimsPrincipal user, string permission, Guid siteId) { if (!scopes.HasAccess(user, permission, siteId)) throw new UnauthorizedAccessException("The requested site is outside the granted scope."); }
    private void RequireTenant(ClaimsPrincipal user, string permission) { if (!scopes.HasTenantAccess(user, permission)) throw new UnauthorizedAccessException("Tenant-wide permission is required."); }
    private static void RequireAny(ClaimsPrincipal user, string permission) { if (!user.HasClaim(TenantClaims.Permission, permission)) throw new UnauthorizedAccessException("Permission is required."); }
    private static bool HasAny(ClaimsPrincipal user, string permission) => user.HasClaim(TenantClaims.Permission, permission);
    private static Guid[] GrantedSites(ClaimsPrincipal user, string permission) => user.FindAll(TenantClaims.Grant).Select(x => x.Value.Split('|')).Where(x => x.Length == 3 && x[0] == permission && x[1] == nameof(AccessScopeKind.Site) && Guid.TryParse(x[2], out _)).Select(x => Guid.Parse(x[2])).Distinct().ToArray();
    private static Guid Actor(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue("sub"), out var id) ? id : throw new UnauthorizedAccessException();
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ValidationException($"{name} is required.") : value.Trim();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Valid(ServiceRequestPriority value) { if (!Enum.IsDefined(value)) throw new ValidationException("Priority is invalid."); }
    private static void Version(Guid current, Guid supplied) { if (supplied == Guid.Empty || current != supplied) throw new ConflictException("The record was changed by another user."); }
    private async Task Save(CancellationToken ct) { try { await database.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new ConflictException("The record was changed by another user."); } catch (DbUpdateException) { throw new ConflictException("The service request conflicts with current data."); } }
}

internal static class ServiceRequestQueryExtensions
{
    internal static IQueryable<ServiceRequest> IncludeRequest(this IQueryable<ServiceRequest> query) => query
        .Include(x => x.Site).Include(x => x.Location).Include(x => x.Category).Include(x => x.Requester).Include(x => x.Asset)
        .Include(x => x.AssignedTo).Include(x => x.AssignedBy).Include(x => x.ResolvedBy).Include(x => x.LinkedBreakdownReport).ThenInclude(x => x!.WorkOrder)
        .Include(x => x.Assignments).ThenInclude(x => x.Assignee).Include(x => x.Assignments).ThenInclude(x => x.AssignedBy)
        .Include(x => x.Notes).ThenInclude(x => x.Author).Include(x => x.Activities).ThenInclude(x => x.ActorUser);
}
