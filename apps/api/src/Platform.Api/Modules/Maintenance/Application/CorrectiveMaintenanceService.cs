using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Assets.Domain;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Maintenance.Domain;
using SaaSPlatform.Api.Modules.Inventory.Application;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Maintenance.Application;

public sealed record BreakdownCreateInput(Guid AssetId, string Title, string Description, string? ObservedSymptoms,
    DateTimeOffset? DowntimeStartedAtUtc, MaintenancePriority Priority);
public sealed record BreakdownReviewInput(bool Approved, string? Reason, Guid Version);
public sealed record BreakdownListQuery(string? Search, MaintenancePriority? Priority, BreakdownStatus? Status,
    Guid? SiteId, Guid? AssetId, int Page, int PageSize);
public sealed record WorkOrderCreateInput(Guid BreakdownReportId, string? Description, DateTimeOffset? PlannedStartUtc, Guid BreakdownVersion);
public sealed record WorkOrderAssignmentInput(Guid TechnicianId, string? Note, Guid Version);
public sealed record WorkOrderActivityInput(string Details, Guid Version);
public sealed record MaintenanceReportInput(string? Diagnosis, string? WorkPerformed, RepairOutcome? RepairOutcome,
    DateTimeOffset? ActualStartUtc, DateTimeOffset? ActualEndUtc, int? LaborMinutes, string? AdditionalNotes, Guid Version);
public sealed record WorkOrderVerificationInput(bool Verified, string? Notes, Guid Version);
public sealed record WorkOrderCancelInput(string Reason, Guid Version);
public sealed record WorkOrderListQuery(string? Search, MaintenancePriority? Priority, CorrectiveWorkOrderStatus? Status,
    Guid? SiteId, Guid? AssetId, Guid? AssignedTechnicianId, bool Mine, int Page, int PageSize);
public sealed record MaintenancePage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public sealed record MaintenanceDashboard(int OpenBreakdowns, int ActiveWorkOrders, int CriticalWork,
    int PendingVerification, int CompletedWorkOrders);
public sealed record TechnicianOption(Guid UserId, string DisplayName, string Email);
public sealed record MaintenanceAssetOption(Guid Id, string Code, string NameEn, string? NameAr, Guid SiteId,
    string SiteNameEn, string? SiteNameAr, string? LocationNameEn, string? LocationNameAr, Guid? ResponsibleUserId, string? ResponsibleUserName);

public interface ICorrectiveMaintenanceService
{
    Task<MaintenancePage<BreakdownReport>> ListBreakdowns(ClaimsPrincipal user, BreakdownListQuery queryInput, CancellationToken ct);
    Task<BreakdownReport> GetBreakdown(ClaimsPrincipal user, Guid id, CancellationToken ct);
    Task<BreakdownReport> CreateBreakdown(ClaimsPrincipal user, BreakdownCreateInput input, CancellationToken ct);
    Task<BreakdownReport> CreateBreakdownFromServiceRequest(ClaimsPrincipal user, BreakdownCreateInput input, CancellationToken ct);
    Task<BreakdownReport> ReviewBreakdown(ClaimsPrincipal user, Guid id, BreakdownReviewInput input, CancellationToken ct);
    Task<MaintenancePage<CorrectiveWorkOrder>> ListWorkOrders(ClaimsPrincipal user, WorkOrderListQuery queryInput, CancellationToken ct);
    Task<CorrectiveWorkOrder> GetWorkOrder(ClaimsPrincipal user, Guid id, CancellationToken ct);
    Task<CorrectiveWorkOrder> CreateWorkOrder(ClaimsPrincipal user, WorkOrderCreateInput input, CancellationToken ct);
    Task<CorrectiveWorkOrder> Assign(ClaimsPrincipal user, Guid id, WorkOrderAssignmentInput input, CancellationToken ct);
    Task<CorrectiveWorkOrder> Start(ClaimsPrincipal user, Guid id, Guid version, CancellationToken ct);
    Task<CorrectiveWorkOrder> AddActivity(ClaimsPrincipal user, Guid id, WorkOrderActivityInput input, CancellationToken ct);
    Task<CorrectiveWorkOrder> SaveReport(ClaimsPrincipal user, Guid id, MaintenanceReportInput input, CancellationToken ct);
    Task<CorrectiveWorkOrder> SubmitReport(ClaimsPrincipal user, Guid id, Guid version, CancellationToken ct);
    Task<CorrectiveWorkOrder> Verify(ClaimsPrincipal user, Guid id, WorkOrderVerificationInput input, CancellationToken ct);
    Task<CorrectiveWorkOrder> Cancel(ClaimsPrincipal user, Guid id, WorkOrderCancelInput input, CancellationToken ct);
    Task<MaintenanceDashboard> Dashboard(ClaimsPrincipal user, CancellationToken ct);
    Task<IReadOnlyList<TechnicianOption>> Technicians(ClaimsPrincipal user, Guid siteId, CancellationToken ct);
    Task<IReadOnlyList<MaintenanceAssetOption>> BreakdownAssets(ClaimsPrincipal user, string? search, CancellationToken ct);
    Task<IReadOnlyList<CorrectiveWorkOrder>> AssetHistory(ClaimsPrincipal user, Guid assetId, CancellationToken ct);
}

public sealed class CorrectiveMaintenanceService(
    PlatformDbContext database,
    ICurrentTenant currentTenant,
    ISiteScopeAuthorizer scopes,
    IAuditWriter audit,
    TimeProvider timeProvider) : ICorrectiveMaintenanceService
{
    private Guid TenantId => currentTenant.Id ?? throw new InvalidOperationException("Tenant context is required.");
    private DateTimeOffset Now => timeProvider.GetUtcNow();

    public async Task<MaintenancePage<BreakdownReport>> ListBreakdowns(ClaimsPrincipal user, BreakdownListQuery queryInput, CancellationToken ct)
    {
        var query = AccessibleBreakdowns(user, TenantPermissions.MaintenanceBreakdownsView).AsNoTracking().IncludeBreakdown();
        if (!string.IsNullOrWhiteSpace(queryInput.Search)) { var term = $"%{queryInput.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Number, term) || EF.Functions.ILike(x.Title, term) || EF.Functions.ILike(x.Asset!.Code, term)); }
        if (queryInput.Priority is { } priority) query = query.Where(x => x.Priority == priority);
        if (queryInput.Status is { } status) query = query.Where(x => x.Status == status);
        if (queryInput.SiteId is { } site) query = query.Where(x => x.SiteId == site);
        if (queryInput.AssetId is { } asset) query = query.Where(x => x.AssetId == asset);
        var (page, size) = Page(queryInput.Page, queryInput.PageSize); var total = await query.CountAsync(ct);
        return new(await query.OrderByDescending(x => x.ReportedAtUtc).ThenBy(x => x.Number).Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, total);
    }

    public async Task<BreakdownReport> GetBreakdown(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var entity = await database.BreakdownReports.AsNoTracking().IncludeBreakdown().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ResourceNotFoundException("Breakdown report was not found.");
        RequireSite(user, TenantPermissions.MaintenanceBreakdownsView, entity.SiteId); return entity;
    }

    public async Task<BreakdownReport> CreateBreakdown(ClaimsPrincipal user, BreakdownCreateInput input, CancellationToken ct)
        => await CreateBreakdownCore(user, input, TenantPermissions.MaintenanceBreakdownsCreate, ct);

    public async Task<BreakdownReport> CreateBreakdownFromServiceRequest(ClaimsPrincipal user, BreakdownCreateInput input, CancellationToken ct)
        => await CreateBreakdownCore(user, input, TenantPermissions.ServiceRequestsConvertToMaintenance, ct);

    private async Task<BreakdownReport> CreateBreakdownCore(ClaimsPrincipal user, BreakdownCreateInput input, string permission, CancellationToken ct)
    {
        Required(input.Title, "Title"); Required(input.Description, "Description"); Valid(input.Priority);
        var asset = await database.Assets.Include(x => x.Location).SingleOrDefaultAsync(x => x.Id == input.AssetId, ct)
            ?? throw new ValidationException("An asset in this tenant is required.");
        RequireSite(user, permission, asset.SiteId);
        if (asset.LifecycleState != AssetLifecycleState.Active) throw new ConflictException("A breakdown cannot be reported for a retired asset.");
        if (input.DowntimeStartedAtUtc > Now) throw new ValidationException("Downtime cannot start in the future.");
        var actor = Actor(user); var entity = new BreakdownReport { Id = Guid.NewGuid(), TenantId = TenantId,
            Number = Number("BR"), AssetId = asset.Id, SiteId = asset.SiteId,
            LocationSnapshot = asset.Location is null ? null : $"{asset.Location.Code} · {asset.Location.NameEn}",
            ReportedById = actor, ReportedAtUtc = Now, Title = input.Title.Trim(), Description = input.Description.Trim(),
            ObservedSymptoms = Clean(input.ObservedSymptoms), DowntimeStartedAtUtc = input.DowntimeStartedAtUtc, Priority = input.Priority };
        database.BreakdownReports.Add(entity); audit.Add("Maintenance.BreakdownCreated", nameof(BreakdownReport), entity.Id, new { entity.Number, entity.AssetId, entity.SiteId, entity.Priority });
        await Save(ct); return await LoadBreakdown(entity.Id, ct);
    }

    public async Task<BreakdownReport> ReviewBreakdown(ClaimsPrincipal user, Guid id, BreakdownReviewInput input, CancellationToken ct)
    {
        var entity = await database.BreakdownReports.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Breakdown report was not found.");
        RequireSite(user, TenantPermissions.MaintenanceBreakdownsReview, entity.SiteId); Version(entity.Version, input.Version);
        if (entity.Status is not (BreakdownStatus.Submitted or BreakdownStatus.UnderReview)) throw new ConflictException("Only a submitted breakdown can be reviewed.");
        var actor = Actor(user); if (actor == entity.ReportedById) throw new ConflictException("A reporter cannot review their own breakdown.");
        if (!input.Approved && string.IsNullOrWhiteSpace(input.Reason)) throw new ValidationException("A rejection reason is required.");
        entity.Status = input.Approved ? BreakdownStatus.Approved : BreakdownStatus.Rejected; entity.Version = Guid.NewGuid();
        database.BreakdownReviews.Add(new BreakdownReview { Id = Guid.NewGuid(), TenantId = TenantId, BreakdownReportId = id,
            ReviewerId = actor, Approved = input.Approved, Reason = Clean(input.Reason), ReviewedAtUtc = Now });
        audit.Add(input.Approved ? "Maintenance.BreakdownApproved" : "Maintenance.BreakdownRejected", nameof(BreakdownReport), id, new { input.Reason });
        await Save(ct); return await LoadBreakdown(id, ct);
    }

    public async Task<MaintenancePage<CorrectiveWorkOrder>> ListWorkOrders(ClaimsPrincipal user, WorkOrderListQuery queryInput, CancellationToken ct)
    {
        var query = AccessibleWorkOrders(user, TenantPermissions.MaintenanceWorkOrdersView).Where(x => x.Type == MaintenanceWorkOrderType.Corrective).AsNoTracking().IncludeWorkOrder();
        if (queryInput.Mine) query = query.Where(x => x.AssignedTechnicianId == Actor(user));
        if (!string.IsNullOrWhiteSpace(queryInput.Search)) { var term = $"%{queryInput.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Number, term) || EF.Functions.ILike(x.Description, term) || EF.Functions.ILike(x.Asset!.Code, term)); }
        if (queryInput.Priority is { } priority) query = query.Where(x => x.Priority == priority);
        if (queryInput.Status is { } status) query = query.Where(x => x.Status == status);
        if (queryInput.SiteId is { } site) query = query.Where(x => x.SiteId == site);
        if (queryInput.AssetId is { } asset) query = query.Where(x => x.AssetId == asset);
        if (queryInput.AssignedTechnicianId is { } technician) query = query.Where(x => x.AssignedTechnicianId == technician);
        var (page, size) = Page(queryInput.Page, queryInput.PageSize); var total = await query.CountAsync(ct);
        return new(await query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.Number).Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, total);
    }

    public async Task<CorrectiveWorkOrder> GetWorkOrder(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var entity = await database.CorrectiveWorkOrders.AsNoTracking().IncludeWorkOrder().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ResourceNotFoundException("Corrective work order was not found.");
        RequireSite(user, entity.Type == MaintenanceWorkOrderType.Preventive ? TenantPermissions.MaintenancePmView : TenantPermissions.MaintenanceWorkOrdersView, entity.SiteId); return entity;
    }

    public async Task<CorrectiveWorkOrder> CreateWorkOrder(ClaimsPrincipal user, WorkOrderCreateInput input, CancellationToken ct)
    {
        var breakdown = await database.BreakdownReports.SingleOrDefaultAsync(x => x.Id == input.BreakdownReportId, ct)
            ?? throw new ValidationException("An approved breakdown in this tenant is required.");
        RequireSite(user, TenantPermissions.MaintenanceWorkOrdersCreate, breakdown.SiteId); Version(breakdown.Version, input.BreakdownVersion);
        if (breakdown.Status != BreakdownStatus.Approved) throw new ConflictException("Only an approved breakdown can create a work order.");
        if (await database.CorrectiveWorkOrders.AnyAsync(x => x.BreakdownReportId == breakdown.Id, ct)) throw new ConflictException("This breakdown already has a work order.");
        var entity = new CorrectiveWorkOrder { Id = Guid.NewGuid(), TenantId = TenantId, Number = Number("CWO"),
            BreakdownReportId = breakdown.Id, AssetId = breakdown.AssetId, SiteId = breakdown.SiteId,
            LocationSnapshot = breakdown.LocationSnapshot, Priority = breakdown.Priority,
            Description = string.IsNullOrWhiteSpace(input.Description) ? breakdown.Description : input.Description.Trim(), PlannedStartUtc = input.PlannedStartUtc };
        breakdown.Status = BreakdownStatus.ConvertedToWorkOrder; breakdown.Version = Guid.NewGuid(); database.CorrectiveWorkOrders.Add(entity);
        Activity(entity, "WorkOrder.Created", null, user); audit.Add("Maintenance.WorkOrderCreated", nameof(CorrectiveWorkOrder), entity.Id, new { entity.Number, breakdown.Id, entity.AssetId });
        await Save(ct); return await LoadWorkOrder(entity.Id, ct);
    }

    public async Task<CorrectiveWorkOrder> Assign(ClaimsPrincipal user, Guid id, WorkOrderAssignmentInput input, CancellationToken ct)
    {
        var entity = await WorkOrder(id, ct); RequireSite(user, entity.Type == MaintenanceWorkOrderType.Preventive ? TenantPermissions.MaintenancePmManage : TenantPermissions.MaintenanceWorkOrdersAssign, entity.SiteId); Version(entity.Version, input.Version);
        if (entity.Status is not (CorrectiveWorkOrderStatus.Open or CorrectiveWorkOrderStatus.Assigned or CorrectiveWorkOrderStatus.ReturnedForCorrection)) throw new ConflictException("This work order cannot be assigned in its current state.");
        if (!await CanExecute(input.TechnicianId, entity.SiteId, entity.Type, ct)) throw new ValidationException("The technician must be active and authorized to execute work at this site.");
        var actor = Actor(user); var previous = entity.AssignedTechnicianId; entity.AssignedTechnicianId = input.TechnicianId;
        entity.AssignedById = actor; entity.AssignedAtUtc = Now; entity.Status = CorrectiveWorkOrderStatus.Assigned; entity.Version = Guid.NewGuid();
        database.WorkOrderAssignments.Add(new WorkOrderAssignment { Id = Guid.NewGuid(), TenantId = TenantId, WorkOrderId = id,
            TechnicianId = input.TechnicianId, AssignedById = actor, AssignedAtUtc = Now, Note = Clean(input.Note) });
        Activity(entity, previous is null ? "WorkOrder.Assigned" : "WorkOrder.Reassigned", new { PreviousTechnicianId = previous, input.TechnicianId }, user);
        audit.Add("Maintenance.WorkOrderAssigned", nameof(CorrectiveWorkOrder), id, new { PreviousTechnicianId = previous, input.TechnicianId }); await Save(ct); return await LoadWorkOrder(id, ct);
    }

    public async Task<CorrectiveWorkOrder> Start(ClaimsPrincipal user, Guid id, Guid version, CancellationToken ct)
    {
        var entity = await WorkOrder(id, ct); RequireExecutor(user, entity); Version(entity.Version, version);
        if (entity.Status is not (CorrectiveWorkOrderStatus.Assigned or CorrectiveWorkOrderStatus.ReturnedForCorrection)) throw new ConflictException("Only assigned or returned work can be started.");
        entity.Status = CorrectiveWorkOrderStatus.InProgress; entity.ActualStartUtc ??= Now; entity.Version = Guid.NewGuid(); Activity(entity, "WorkOrder.Started", null, user);
        audit.Add("Maintenance.WorkStarted", nameof(CorrectiveWorkOrder), id); await Save(ct); return await LoadWorkOrder(id, ct);
    }

    public async Task<CorrectiveWorkOrder> AddActivity(ClaimsPrincipal user, Guid id, WorkOrderActivityInput input, CancellationToken ct)
    {
        var entity = await WorkOrder(id, ct); RequireExecutor(user, entity); Version(entity.Version, input.Version);
        if (entity.Status != CorrectiveWorkOrderStatus.InProgress) throw new ConflictException("Work notes can be added only while work is in progress.");
        var details = Required(input.Details, "Work note"); entity.Version = Guid.NewGuid(); Activity(entity, "WorkOrder.NoteAdded", null, user, details);
        audit.Add("Maintenance.WorkNoteAdded", nameof(CorrectiveWorkOrder), id); await Save(ct); return await LoadWorkOrder(id, ct);
    }

    public async Task<CorrectiveWorkOrder> SaveReport(ClaimsPrincipal user, Guid id, MaintenanceReportInput input, CancellationToken ct)
    {
        var entity = await database.CorrectiveWorkOrders.Include(x => x.MaintenanceReport).Include(x => x.ChecklistResponses).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ResourceNotFoundException("Corrective work order was not found.");
        RequireExecutor(user, entity); Version(entity.Version, input.Version);
        if (entity.Status != CorrectiveWorkOrderStatus.InProgress) throw new ConflictException("A report draft can be edited only while work is in progress.");
        if (input.ActualEndUtc < input.ActualStartUtc) throw new ValidationException("Actual end cannot be before actual start.");
        if (input.LaborMinutes < 0) throw new ValidationException("Labor duration cannot be negative.");
        var actor = Actor(user); var report = entity.MaintenanceReport;
        if (report is null) { report = new MaintenanceReport { Id = Guid.NewGuid(), TenantId = TenantId, WorkOrderId = id, AssetId = entity.AssetId, TechnicianId = actor }; database.MaintenanceReports.Add(report); }
        if (report.Status is not (MaintenanceReportStatus.Draft or MaintenanceReportStatus.Returned)) throw new ConflictException("A submitted report is immutable until returned.");
        report.TechnicianId = actor; report.Diagnosis = Clean(input.Diagnosis); report.WorkPerformed = Clean(input.WorkPerformed); report.RepairOutcome = input.RepairOutcome;
        report.ActualStartUtc = input.ActualStartUtc ?? entity.ActualStartUtc; report.ActualEndUtc = input.ActualEndUtc; report.LaborMinutes = input.LaborMinutes; report.AdditionalNotes = Clean(input.AdditionalNotes);
        report.Status = MaintenanceReportStatus.Draft; report.Version = Guid.NewGuid(); entity.Version = Guid.NewGuid(); Activity(entity, "MaintenanceReport.Drafted", null, user);
        audit.Add("Maintenance.ReportDrafted", nameof(MaintenanceReport), report.Id); await Save(ct); return await LoadWorkOrder(id, ct);
    }

    public async Task<CorrectiveWorkOrder> SubmitReport(ClaimsPrincipal user, Guid id, Guid version, CancellationToken ct)
    {
        var entity = await database.CorrectiveWorkOrders.Include(x => x.MaintenanceReport).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ResourceNotFoundException("Corrective work order was not found.");
        RequireExecutor(user, entity); Version(entity.Version, version);
        if (entity.Status != CorrectiveWorkOrderStatus.InProgress || entity.MaintenanceReport is null) throw new ConflictException("An in-progress work order with a draft report is required.");
        var report = entity.MaintenanceReport;
        if (report.Status != MaintenanceReportStatus.Draft) throw new ConflictException("Only a draft report can be submitted.");
        if (string.IsNullOrWhiteSpace(report.Diagnosis) || string.IsNullOrWhiteSpace(report.WorkPerformed) || report.RepairOutcome is null || report.ActualStartUtc is null || report.ActualEndUtc is null)
            throw new ValidationException("Diagnosis, work performed, repair outcome, actual start, and actual end are required.");
        if (report.ActualEndUtc < report.ActualStartUtc) throw new ValidationException("Actual end cannot be before actual start.");
        if (entity.Type == MaintenanceWorkOrderType.Preventive && entity.ChecklistResponses.Any(x => x.IsRequired && x.Result == null))
            throw new ValidationException("All required preventive checklist items must be completed before submission.");
        report.Status = MaintenanceReportStatus.Submitted; report.SubmittedAtUtc = Now; report.Version = Guid.NewGuid();
        entity.Status = CorrectiveWorkOrderStatus.PendingVerification; entity.ActualCompletionUtc = report.ActualEndUtc; entity.Version = Guid.NewGuid(); Activity(entity, "MaintenanceReport.Submitted", new { report.RepairOutcome }, user);
        audit.Add("Maintenance.ReportSubmitted", nameof(MaintenanceReport), report.Id, new { report.RepairOutcome }); await Save(ct); return await LoadWorkOrder(id, ct);
    }

    public async Task<CorrectiveWorkOrder> Verify(ClaimsPrincipal user, Guid id, WorkOrderVerificationInput input, CancellationToken ct)
    {
        var entity = await database.CorrectiveWorkOrders.Include(x => x.MaintenanceReport).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ResourceNotFoundException("Corrective work order was not found.");
        RequireSite(user, entity.Type == MaintenanceWorkOrderType.Preventive ? TenantPermissions.MaintenancePmVerify : TenantPermissions.MaintenanceWorkOrdersVerify, entity.SiteId); Version(entity.Version, input.Version);
        if (entity.Status != CorrectiveWorkOrderStatus.PendingVerification || entity.MaintenanceReport?.Status != MaintenanceReportStatus.Submitted)
            throw new ConflictException("Only a submitted maintenance report can be verified.");
        var actor = Actor(user); if (actor == entity.MaintenanceReport.TechnicianId) throw new ConflictException("A technician cannot verify their own maintenance report.");
        if (!input.Verified && string.IsNullOrWhiteSpace(input.Notes)) throw new ValidationException("A correction reason is required.");
        database.MaintenanceReportReviews.Add(new MaintenanceReportReview { Id = Guid.NewGuid(), TenantId = TenantId,
            MaintenanceReportId = entity.MaintenanceReport.Id, ReviewerId = actor, Verified = input.Verified, Notes = Clean(input.Notes), ReviewedAtUtc = Now });
        if (input.Verified) { entity.MaintenanceReport.Status = MaintenanceReportStatus.Verified; entity.Status = CorrectiveWorkOrderStatus.Completed; entity.ClosedAtUtc = Now; entity.ActualCompletionUtc ??= Now; await InventoryWorkOrderLifecycle.ReleaseOpenReservations(database, entity, actor, Now, ct); }
        else { entity.MaintenanceReport.Status = MaintenanceReportStatus.Returned; entity.Status = CorrectiveWorkOrderStatus.ReturnedForCorrection; }
        entity.MaintenanceReport.Version = Guid.NewGuid(); entity.Version = Guid.NewGuid(); Activity(entity, input.Verified ? "MaintenanceReport.Verified" : "MaintenanceReport.Returned", new { input.Notes }, user);
        audit.Add(input.Verified ? "Maintenance.WorkOrderCompleted" : "Maintenance.ReportReturned", nameof(CorrectiveWorkOrder), id, new { input.Notes }); await Save(ct); return await LoadWorkOrder(id, ct);
    }

    public async Task<CorrectiveWorkOrder> Cancel(ClaimsPrincipal user, Guid id, WorkOrderCancelInput input, CancellationToken ct)
    {
        var entity = await WorkOrder(id, ct); RequireSite(user, entity.Type == MaintenanceWorkOrderType.Preventive ? TenantPermissions.MaintenancePmManage : TenantPermissions.MaintenanceWorkOrdersCancel, entity.SiteId); Version(entity.Version, input.Version);
        var reason = Required(input.Reason, "Cancellation reason");
        if (entity.Status is CorrectiveWorkOrderStatus.Completed or CorrectiveWorkOrderStatus.Cancelled or CorrectiveWorkOrderStatus.PendingVerification) throw new ConflictException("This work order cannot be cancelled in its current state.");
        entity.Status = CorrectiveWorkOrderStatus.Cancelled; entity.ClosedAtUtc = Now; entity.Version = Guid.NewGuid(); await InventoryWorkOrderLifecycle.ReleaseOpenReservations(database, entity, Actor(user), Now, ct); Activity(entity, "WorkOrder.Cancelled", null, user, reason);
        audit.Add("Maintenance.WorkOrderCancelled", nameof(CorrectiveWorkOrder), id, new { Reason = reason }); await Save(ct); return await LoadWorkOrder(id, ct);
    }

    public async Task<MaintenanceDashboard> Dashboard(ClaimsPrincipal user, CancellationToken ct)
    {
        var breakdowns = AccessibleBreakdowns(user, TenantPermissions.MaintenanceBreakdownsView).AsNoTracking();
        var orders = AccessibleWorkOrders(user, TenantPermissions.MaintenanceWorkOrdersView).AsNoTracking();
        return new(await breakdowns.CountAsync(x => x.Status == BreakdownStatus.Submitted || x.Status == BreakdownStatus.UnderReview || x.Status == BreakdownStatus.Approved, ct),
            await orders.CountAsync(x => x.Status != CorrectiveWorkOrderStatus.Completed && x.Status != CorrectiveWorkOrderStatus.Cancelled, ct),
            await orders.CountAsync(x => x.Priority == MaintenancePriority.Critical && x.Status != CorrectiveWorkOrderStatus.Completed && x.Status != CorrectiveWorkOrderStatus.Cancelled, ct),
            await orders.CountAsync(x => x.Status == CorrectiveWorkOrderStatus.PendingVerification, ct),
            await orders.CountAsync(x => x.Status == CorrectiveWorkOrderStatus.Completed, ct));
    }

    public async Task<IReadOnlyList<TechnicianOption>> Technicians(ClaimsPrincipal user, Guid siteId, CancellationToken ct)
    {
        RequireSite(user, TenantPermissions.MaintenanceWorkOrdersAssign, siteId);
        return await database.TenantMemberships.AsNoTracking().Where(m => m.IsActive && m.User != null && m.User.IsActive &&
            m.Roles.Any(mr => mr.Role != null && mr.Role.IsActive &&
                mr.Role.Permissions.Any(rp => rp.Permission != null && rp.Permission.Code == TenantPermissions.MaintenanceWorkOrdersExecute) &&
                (mr.ScopeKind == AccessScopeKind.Tenant || (mr.ScopeKind == AccessScopeKind.Site && mr.ScopeId == siteId))))
            .OrderBy(m => m.User!.DisplayName).Select(m => new TechnicianOption(m.UserId, m.User!.DisplayName, m.User.Email ?? string.Empty)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MaintenanceAssetOption>> BreakdownAssets(ClaimsPrincipal user, string? search, CancellationToken ct)
    {
        var query = scopes.HasTenantAccess(user, TenantPermissions.MaintenanceBreakdownsCreate)
            ? database.Assets.AsNoTracking()
            : database.Assets.AsNoTracking().Where(x => GrantedSites(user, TenantPermissions.MaintenanceBreakdownsCreate).Contains(x.SiteId));
        query = query.Where(x => x.LifecycleState == AssetLifecycleState.Active).Include(x => x.Site).Include(x => x.Location).Include(x => x.ResponsibleUser);
        if (!string.IsNullOrWhiteSpace(search)) { var term = $"%{search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Code, term) || EF.Functions.ILike(x.NameEn, term) || (x.NameAr != null && EF.Functions.ILike(x.NameAr, term))); }
        return await query.OrderBy(x => x.Code).Take(100).Select(x => new MaintenanceAssetOption(x.Id, x.Code, x.NameEn, x.NameAr,
            x.SiteId, x.Site!.NameEn, x.Site.NameAr, x.Location == null ? null : x.Location.NameEn, x.Location == null ? null : x.Location.NameAr,
            x.ResponsibleUserId, x.ResponsibleUser == null ? null : x.ResponsibleUser.DisplayName)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CorrectiveWorkOrder>> AssetHistory(ClaimsPrincipal user, Guid assetId, CancellationToken ct)
    {
        var asset = await database.Assets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assetId, ct) ?? throw new ResourceNotFoundException("Asset was not found.");
        RequireSite(user, TenantPermissions.MaintenanceWorkOrdersView, asset.SiteId);
        return await database.CorrectiveWorkOrders.AsNoTracking().IncludeWorkOrder().Where(x => x.AssetId == assetId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
    }

    private IQueryable<BreakdownReport> AccessibleBreakdowns(ClaimsPrincipal user, string permission) => scopes.HasTenantAccess(user, permission)
        ? database.BreakdownReports : database.BreakdownReports.Where(x => GrantedSites(user, permission).Contains(x.SiteId));
    private IQueryable<CorrectiveWorkOrder> AccessibleWorkOrders(ClaimsPrincipal user, string permission) => scopes.HasTenantAccess(user, permission)
        ? database.CorrectiveWorkOrders : database.CorrectiveWorkOrders.Where(x => GrantedSites(user, permission).Contains(x.SiteId));
    private static Guid[] GrantedSites(ClaimsPrincipal user, string permission) => user.FindAll(TenantClaims.Grant).Select(x => x.Value.Split('|'))
        .Where(x => x.Length == 3 && x[0] == permission && x[1] == nameof(AccessScopeKind.Site) && Guid.TryParse(x[2], out _)).Select(x => Guid.Parse(x[2])).Distinct().ToArray();
    private void RequireSite(ClaimsPrincipal user, string permission, Guid siteId) { if (!scopes.HasAccess(user, permission, siteId)) throw new UnauthorizedAccessException("The requested site is outside the granted scope."); }
    private void RequireExecutor(ClaimsPrincipal user, CorrectiveWorkOrder order)
    {
        var execute = order.Type == MaintenanceWorkOrderType.Preventive ? TenantPermissions.MaintenancePmExecute : TenantPermissions.MaintenanceWorkOrdersExecute;
        RequireSite(user, execute, order.SiteId);
        var supervise = order.Type == MaintenanceWorkOrderType.Preventive ? TenantPermissions.MaintenancePmManage : TenantPermissions.MaintenanceWorkOrdersAssign;
        if (order.AssignedTechnicianId != Actor(user) && !scopes.HasAccess(user, supervise, order.SiteId))
            throw new UnauthorizedAccessException("Only the assigned technician or an authorized supervisor may execute this work order.");
    }
    private async Task<bool> CanExecute(Guid userId, Guid siteId, MaintenanceWorkOrderType type, CancellationToken ct) { var permission = type == MaintenanceWorkOrderType.Preventive ? TenantPermissions.MaintenancePmExecute : TenantPermissions.MaintenanceWorkOrdersExecute; return await database.TenantMemberships.AsNoTracking().AnyAsync(m => m.UserId == userId && m.IsActive && m.User != null && m.User.IsActive &&
        m.Roles.Any(mr => mr.Role != null && mr.Role.IsActive && mr.Role.Permissions.Any(rp => rp.Permission != null && rp.Permission.Code == permission) &&
            (mr.ScopeKind == AccessScopeKind.Tenant || (mr.ScopeKind == AccessScopeKind.Site && mr.ScopeId == siteId))), ct); }
    private async Task<BreakdownReport> LoadBreakdown(Guid id, CancellationToken ct) => await database.BreakdownReports.AsNoTracking().IncludeBreakdown().SingleAsync(x => x.Id == id, ct);
    private async Task<CorrectiveWorkOrder> LoadWorkOrder(Guid id, CancellationToken ct) => await database.CorrectiveWorkOrders.AsNoTracking().IncludeWorkOrder().SingleAsync(x => x.Id == id, ct);
    private async Task<CorrectiveWorkOrder> WorkOrder(Guid id, CancellationToken ct) => await database.CorrectiveWorkOrders.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Corrective work order was not found.");
    private void Activity(CorrectiveWorkOrder order, string action, object? metadata, ClaimsPrincipal user, string? details = null) => database.WorkOrderActivities.Add(new WorkOrderActivity
        { Id = Guid.NewGuid(), TenantId = TenantId, WorkOrderId = order.Id, ActorUserId = Actor(user), Action = action, Details = details, MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata), OccurredAtUtc = Now });
    private static (int Page, int Size) Page(int page, int size) => (Math.Max(1, page), Math.Clamp(size, 1, 100));
    private static string Number(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..21].ToUpperInvariant();
    private static Guid Actor(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue("sub"), out var id) ? id : throw new UnauthorizedAccessException();
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ValidationException($"{name} is required.") : value.Trim();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Valid(MaintenancePriority value) { if (!Enum.IsDefined(value)) throw new ValidationException("Priority is invalid."); }
    private static void Version(Guid current, Guid supplied) { if (supplied == Guid.Empty || current != supplied) throw new ConflictException("The record was changed by another user."); }
    private async Task Save(CancellationToken ct) { try { await database.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new ConflictException("The record was changed by another user."); } }
}

internal static class MaintenanceQueryExtensions
{
    public static IQueryable<BreakdownReport> IncludeBreakdown(this IQueryable<BreakdownReport> query) => query
        .Include(x => x.Asset).Include(x => x.ReportedBy).Include(x => x.Reviews).ThenInclude(x => x.Reviewer).Include(x => x.OriginatingServiceRequest);
    public static IQueryable<CorrectiveWorkOrder> IncludeWorkOrder(this IQueryable<CorrectiveWorkOrder> query) => query
        .Include(x => x.Asset).ThenInclude(x => x!.ResponsibleUser).Include(x => x.BreakdownReport).Include(x => x.PreventiveMaintenancePlan).Include(x => x.PreventiveMaintenanceOccurrence)
        .Include(x => x.AssignedTechnician).Include(x => x.AssignedBy)
        .Include(x => x.Assignments).ThenInclude(x => x.Technician).Include(x => x.Assignments).ThenInclude(x => x.AssignedBy)
        .Include(x => x.Activities).ThenInclude(x => x.ActorUser)
        .Include(x => x.MaintenanceReport).ThenInclude(x => x!.Technician)
        .Include(x => x.MaintenanceReport).ThenInclude(x => x!.Reviews).ThenInclude(x => x.Reviewer)
        .Include(x => x.ChecklistResponses).ThenInclude(x => x.RecordedBy);
}
