using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Assets.Domain;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Maintenance.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Maintenance.Application;

public sealed record PreventiveChecklistItemInput(Guid? Id, int Sequence, string InstructionEn, string? InstructionAr,
    bool IsRequired, string? Guidance, bool IsActive = true);
public sealed record PreventivePlanInput(string Code, string NameEn, string? NameAr, string? Description, Guid AssetId,
    MaintenancePriority Priority, DateOnly StartLocalDate, TimeOnly ScheduledLocalTime, PreventiveRecurrenceType RecurrenceType,
    int RecurrenceInterval, int? EstimatedDurationMinutes, Guid? DefaultTechnicianId,
    IReadOnlyList<PreventiveChecklistItemInput> Checklist, Guid Version);
public sealed record PreventivePlanQuery(string? Search, PreventivePlanState? State, Guid? SiteId, Guid? AssetId,
    int Page, int PageSize);
public sealed record PreventiveCalendarQuery(DateTimeOffset FromUtc, DateTimeOffset ToUtc, Guid? SiteId, Guid? AssetId);
public sealed record PreventivePlanStatusInput(PreventivePlanState State, Guid Version);
public sealed record PreventiveChecklistResponseInput(Guid ChecklistResponseId, ChecklistResult Result, string? Notes, Guid Version);
public sealed record PreventiveDashboard(int ActivePlans, int Upcoming, int DueToday, int Overdue,
    int GeneratedWorkOrders, int Completed, int PendingVerification);
public sealed record PreventiveGenerationResult(int Generated, DateTimeOffset ThroughUtc);

public static class PreventiveRecurrence
{
    public static DateOnly Next(DateOnly start, DateOnly current, PreventiveRecurrenceType type, int interval)
    {
        if (interval < 1 || interval > 365) throw new ValidationException("Recurrence interval must be between 1 and 365.");
        return type switch
        {
            PreventiveRecurrenceType.Daily => current.AddDays(interval),
            PreventiveRecurrenceType.Weekly => current.AddDays(checked(7 * interval)),
            PreventiveRecurrenceType.Monthly => AddAnchoredMonths(start, current, interval),
            PreventiveRecurrenceType.Quarterly => AddAnchoredMonths(start, current, checked(3 * interval)),
            PreventiveRecurrenceType.Semiannual => AddAnchoredMonths(start, current, checked(6 * interval)),
            PreventiveRecurrenceType.Annual => AddAnchoredMonths(start, current, checked(12 * interval)),
            _ => throw new ValidationException("Recurrence type is invalid.")
        };
    }

    public static DateTimeOffset ToUtc(DateOnly date, TimeOnly time, string timeZoneId)
    {
        TimeZoneInfo zone;
        try { zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
        catch (TimeZoneNotFoundException) { throw new ValidationException("Time zone is invalid."); }
        catch (InvalidTimeZoneException) { throw new ValidationException("Time zone is invalid."); }
        var local = date.ToDateTime(time, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(local)) local = local.AddHours(1);
        var offset = zone.IsAmbiguousTime(local) ? zone.GetAmbiguousTimeOffsets(local).Min() : zone.GetUtcOffset(local);
        return new DateTimeOffset(local, offset).ToUniversalTime();
    }

    public static DateOnly LocalDate(DateTimeOffset utc, string timeZoneId) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)).DateTime);

    private static DateOnly AddAnchoredMonths(DateOnly start, DateOnly current, int months)
    {
        var target = new DateOnly(current.Year, current.Month, 1).AddMonths(months);
        return new DateOnly(target.Year, target.Month, Math.Min(start.Day, DateTime.DaysInMonth(target.Year, target.Month)));
    }
}

public interface IPreventiveMaintenanceService
{
    Task<MaintenancePage<PreventiveMaintenancePlan>> List(ClaimsPrincipal user, PreventivePlanQuery query, CancellationToken ct);
    Task<PreventiveMaintenancePlan> GetPlan(ClaimsPrincipal user, Guid id, CancellationToken ct);
    Task<PreventiveMaintenancePlan> Create(ClaimsPrincipal user, PreventivePlanInput input, CancellationToken ct);
    Task<PreventiveMaintenancePlan> Update(ClaimsPrincipal user, Guid id, PreventivePlanInput input, CancellationToken ct);
    Task<PreventiveMaintenancePlan> ChangeStatus(ClaimsPrincipal user, Guid id, PreventivePlanStatusInput input, CancellationToken ct);
    Task<IReadOnlyList<PreventiveMaintenanceOccurrence>> Occurrences(ClaimsPrincipal user, Guid id, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);
    Task<IReadOnlyList<PreventiveMaintenanceOccurrence>> Calendar(ClaimsPrincipal user, PreventiveCalendarQuery query, CancellationToken ct);
    Task<PreventiveDashboard> Dashboard(ClaimsPrincipal user, CancellationToken ct);
    Task<PreventiveGenerationResult> Generate(ClaimsPrincipal user, DateTimeOffset? throughUtc, CancellationToken ct);
    Task<int> GenerateSystem(DateTimeOffset throughUtc, CancellationToken ct);
    Task<CorrectiveWorkOrder> RecordChecklist(ClaimsPrincipal user, Guid workOrderId, PreventiveChecklistResponseInput input, CancellationToken ct);
    Task<MaintenancePage<CorrectiveWorkOrder>> ListWorkOrders(ClaimsPrincipal user, WorkOrderListQuery queryInput, CancellationToken ct);
    Task<IReadOnlyList<TechnicianOption>> Technicians(ClaimsPrincipal user, Guid siteId, CancellationToken ct);
    Task<IReadOnlyList<MaintenanceAssetOption>> Assets(ClaimsPrincipal user, string? search, CancellationToken ct);
}

public sealed class PreventiveMaintenanceService(PlatformDbContext database, ICurrentTenant currentTenant,
    ISiteScopeAuthorizer scopes, IAuditWriter audit, TimeProvider timeProvider) : IPreventiveMaintenanceService
{
    private const int MaxCatchUpPerPlan = 3;
    private Guid TenantId => currentTenant.Id ?? throw new InvalidOperationException("Tenant context is required.");
    private DateTimeOffset Now => timeProvider.GetUtcNow();

    public async Task<MaintenancePage<PreventiveMaintenancePlan>> List(ClaimsPrincipal user, PreventivePlanQuery query, CancellationToken ct)
    {
        var plansQuery = AccessiblePlans(user, TenantPermissions.MaintenancePmView).AsNoTracking().IncludePlan();
        if (!string.IsNullOrWhiteSpace(query.Search)) { var term = $"%{query.Search.Trim()}%"; plansQuery = plansQuery.Where(x => EF.Functions.ILike(x.Code, term) || EF.Functions.ILike(x.NameEn, term) || (x.NameAr != null && EF.Functions.ILike(x.NameAr, term)) || EF.Functions.ILike(x.Asset!.Code, term)); }
        if (query.State is { } state) plansQuery = plansQuery.Where(x => x.State == state);
        if (query.SiteId is { } site) plansQuery = plansQuery.Where(x => x.SiteId == site);
        if (query.AssetId is { } asset) plansQuery = plansQuery.Where(x => x.AssetId == asset);
        var page = Math.Max(1, query.Page); var size = Math.Clamp(query.PageSize, 1, 100); var total = await plansQuery.CountAsync(ct);
        return new(await plansQuery.OrderBy(x => x.NextDueAtUtc).ThenBy(x => x.Code).Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, total);
    }

    public async Task<PreventiveMaintenancePlan> GetPlan(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var plan = await database.PreventiveMaintenancePlans.AsNoTracking().IncludePlan().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ResourceNotFoundException("Preventive maintenance plan was not found.");
        RequireSite(user, TenantPermissions.MaintenancePmView, plan.SiteId); return plan;
    }

    public async Task<PreventiveMaintenancePlan> Create(ClaimsPrincipal user, PreventivePlanInput input, CancellationToken ct)
    {
        Validate(input); var asset = await ValidAsset(input.AssetId, ct); RequireSite(user, TenantPermissions.MaintenancePmCreate, asset.SiteId);
        if (await database.PreventiveMaintenancePlans.AnyAsync(x => x.Code == input.Code.Trim(), ct)) throw new ConflictException("Plan code already exists.");
        var site = await database.Sites.AsNoTracking().SingleAsync(x => x.Id == asset.SiteId, ct); await ValidateTechnician(input.DefaultTechnicianId, asset.SiteId, ct);
        var entity = new PreventiveMaintenancePlan { Id = Guid.NewGuid(), TenantId = TenantId, Code = input.Code.Trim(), NameEn = input.NameEn.Trim(), NameAr = Clean(input.NameAr),
            Description = Clean(input.Description), AssetId = asset.Id, SiteId = asset.SiteId, Priority = input.Priority, StartLocalDate = input.StartLocalDate,
            ScheduledLocalTime = input.ScheduledLocalTime, TimeZoneId = site.TimeZone, RecurrenceType = input.RecurrenceType,
            RecurrenceInterval = input.RecurrenceInterval, NextDueAtUtc = PreventiveRecurrence.ToUtc(input.StartLocalDate, input.ScheduledLocalTime, site.TimeZone),
            EstimatedDurationMinutes = input.EstimatedDurationMinutes, DefaultTechnicianId = input.DefaultTechnicianId, CreatedById = Actor(user) };
        AddChecklist(entity, input.Checklist); database.PreventiveMaintenancePlans.Add(entity);
        audit.Add("Maintenance.PreventivePlanCreated", nameof(PreventiveMaintenancePlan), entity.Id, new { entity.Code, entity.AssetId, entity.SiteId });
        await Save(ct); return await Load(entity.Id, ct);
    }

    public async Task<PreventiveMaintenancePlan> Update(ClaimsPrincipal user, Guid id, PreventivePlanInput input, CancellationToken ct)
    {
        Validate(input); var entity = await database.PreventiveMaintenancePlans.Include(x => x.ChecklistItems).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new ResourceNotFoundException("Preventive maintenance plan was not found.");
        RequireSite(user, TenantPermissions.MaintenancePmUpdate, entity.SiteId); Version(entity.Version, input.Version);
        if (entity.State == PreventivePlanState.Archived) throw new ConflictException("An archived plan cannot be modified.");
        var asset = await ValidAsset(input.AssetId, ct); RequireSite(user, TenantPermissions.MaintenancePmUpdate, asset.SiteId);
        if (await database.PreventiveMaintenancePlans.AnyAsync(x => x.Id != id && x.Code == input.Code.Trim(), ct)) throw new ConflictException("Plan code already exists.");
        var site = await database.Sites.AsNoTracking().SingleAsync(x => x.Id == asset.SiteId, ct); await ValidateTechnician(input.DefaultTechnicianId, asset.SiteId, ct);
        entity.Code = input.Code.Trim(); entity.NameEn = input.NameEn.Trim(); entity.NameAr = Clean(input.NameAr); entity.Description = Clean(input.Description);
        entity.AssetId = asset.Id; entity.SiteId = asset.SiteId; entity.Priority = input.Priority; entity.StartLocalDate = input.StartLocalDate;
        entity.ScheduledLocalTime = input.ScheduledLocalTime; entity.TimeZoneId = site.TimeZone; entity.RecurrenceType = input.RecurrenceType;
        entity.RecurrenceInterval = input.RecurrenceInterval; entity.EstimatedDurationMinutes = input.EstimatedDurationMinutes; entity.DefaultTechnicianId = input.DefaultTechnicianId;
        var proposed = PreventiveRecurrence.ToUtc(input.StartLocalDate, input.ScheduledLocalTime, site.TimeZone);
        entity.NextDueAtUtc = proposed > Now ? proposed : AdvancePast(proposed, entity, Now);
        database.PreventiveMaintenanceChecklistItems.RemoveRange(entity.ChecklistItems); entity.ChecklistItems.Clear(); AddChecklist(entity, input.Checklist);
        entity.Version = Guid.NewGuid(); audit.Add("Maintenance.PreventivePlanUpdated", nameof(PreventiveMaintenancePlan), id); await Save(ct); return await Load(id, ct);
    }

    public async Task<PreventiveMaintenancePlan> ChangeStatus(ClaimsPrincipal user, Guid id, PreventivePlanStatusInput input, CancellationToken ct)
    {
        var entity = await database.PreventiveMaintenancePlans.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Preventive maintenance plan was not found.");
        RequireSite(user, TenantPermissions.MaintenancePmManage, entity.SiteId); Version(entity.Version, input.Version);
        if (!Enum.IsDefined(input.State)) throw new ValidationException("Plan state is invalid.");
        if (entity.State == PreventivePlanState.Archived && input.State != PreventivePlanState.Archived) throw new ConflictException("An archived plan cannot be resumed.");
        entity.State = input.State; if (input.State == PreventivePlanState.Active && entity.NextDueAtUtc < Now) entity.NextDueAtUtc = AdvancePast(entity.NextDueAtUtc, entity, Now);
        entity.Version = Guid.NewGuid(); audit.Add("Maintenance.PreventivePlanStateChanged", nameof(PreventiveMaintenancePlan), id, new { input.State }); await Save(ct); return await Load(id, ct);
    }

    public async Task<IReadOnlyList<PreventiveMaintenanceOccurrence>> Occurrences(ClaimsPrincipal user, Guid id, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct)
    {
        var plan = await database.PreventiveMaintenancePlans.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Preventive maintenance plan was not found.");
        RequireSite(user, TenantPermissions.MaintenancePmView, plan.SiteId); Range(fromUtc, toUtc);
        return await database.PreventiveMaintenanceOccurrences.AsNoTracking().Include(x => x.WorkOrder).Where(x => x.PlanId == id && x.ScheduledDueAtUtc >= fromUtc && x.ScheduledDueAtUtc <= toUtc).OrderBy(x => x.ScheduledDueAtUtc).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PreventiveMaintenanceOccurrence>> Calendar(ClaimsPrincipal user, PreventiveCalendarQuery query, CancellationToken ct)
    {
        Range(query.FromUtc, query.ToUtc); var sites = scopes.HasTenantAccess(user, TenantPermissions.MaintenancePmView) ? null : GrantedSites(user, TenantPermissions.MaintenancePmView);
        var occurrencesQuery = database.PreventiveMaintenanceOccurrences.AsNoTracking().Include(x => x.Plan)!.ThenInclude(x => x!.Asset).Include(x => x.WorkOrder)
            .Where(x => x.ScheduledDueAtUtc >= query.FromUtc && x.ScheduledDueAtUtc <= query.ToUtc);
        if (sites is not null) occurrencesQuery = occurrencesQuery.Where(x => x.Plan != null && sites.Contains(x.Plan.SiteId));
        if (query.SiteId is { } site) occurrencesQuery = occurrencesQuery.Where(x => x.Plan != null && x.Plan.SiteId == site);
        if (query.AssetId is { } asset) occurrencesQuery = occurrencesQuery.Where(x => x.Plan != null && x.Plan.AssetId == asset);
        return await occurrencesQuery.OrderBy(x => x.ScheduledDueAtUtc).ToListAsync(ct);
    }

    public async Task<PreventiveDashboard> Dashboard(ClaimsPrincipal user, CancellationToken ct)
    {
        var plans = AccessiblePlans(user, TenantPermissions.MaintenancePmView).AsNoTracking(); var now = Now; var upcoming = now.AddDays(30);
        var orders = AccessibleOrders(user, TenantPermissions.MaintenancePmView).AsNoTracking();
        return new(await plans.CountAsync(x => x.State == PreventivePlanState.Active, ct), await plans.CountAsync(x => x.State == PreventivePlanState.Active && x.NextDueAtUtc > now && x.NextDueAtUtc <= upcoming, ct),
            await plans.CountAsync(x => x.State == PreventivePlanState.Active && x.NextDueAtUtc >= new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero) && x.NextDueAtUtc < new DateTimeOffset(now.UtcDateTime.Date.AddDays(1), TimeSpan.Zero), ct),
            await plans.CountAsync(x => x.State == PreventivePlanState.Active && x.NextDueAtUtc < now, ct), await orders.CountAsync(ct),
            await orders.CountAsync(x => x.Status == CorrectiveWorkOrderStatus.Completed, ct), await orders.CountAsync(x => x.Status == CorrectiveWorkOrderStatus.PendingVerification, ct));
    }

    public async Task<PreventiveGenerationResult> Generate(ClaimsPrincipal user, DateTimeOffset? throughUtc, CancellationToken ct)
    {
        var through = throughUtc ?? Now; var plans = await AccessiblePlans(user, TenantPermissions.MaintenancePmGenerate).Where(x => x.State == PreventivePlanState.Active && x.NextDueAtUtc <= through).Select(x => x.Id).ToListAsync(ct);
        var count = await GeneratePlans(plans, through, Actor(user), ct); return new(count, through);
    }

    public async Task<int> GenerateSystem(DateTimeOffset throughUtc, CancellationToken ct)
    {
        var plans = await database.PreventiveMaintenancePlans.Where(x => x.State == PreventivePlanState.Active && x.NextDueAtUtc <= throughUtc).Select(x => x.Id).ToListAsync(ct);
        return await GeneratePlans(plans, throughUtc, null, ct);
    }

    public async Task<CorrectiveWorkOrder> RecordChecklist(ClaimsPrincipal user, Guid workOrderId, PreventiveChecklistResponseInput input, CancellationToken ct)
    {
        var order = await database.CorrectiveWorkOrders.Include(x => x.ChecklistResponses).SingleOrDefaultAsync(x => x.Id == workOrderId, ct) ?? throw new ResourceNotFoundException("Work order was not found.");
        if (order.Type != MaintenanceWorkOrderType.Preventive) throw new ConflictException("This work order has no preventive checklist.");
        RequireExecutor(user, order); Version(order.Version, input.Version);
        if (order.Status != CorrectiveWorkOrderStatus.InProgress) throw new ConflictException("Checklist results can be recorded only while work is in progress.");
        var item = order.ChecklistResponses.SingleOrDefault(x => x.Id == input.ChecklistResponseId) ?? throw new ResourceNotFoundException("Checklist item was not found.");
        if (!Enum.IsDefined(input.Result)) throw new ValidationException("Checklist result is invalid.");
        if (input.Result == ChecklistResult.Fail && string.IsNullOrWhiteSpace(input.Notes)) throw new ValidationException("A failed checklist item requires a note.");
        item.Result = input.Result; item.Notes = Clean(input.Notes); item.RecordedById = Actor(user); item.RecordedAtUtc = Now; order.Version = Guid.NewGuid();
        database.WorkOrderActivities.Add(new WorkOrderActivity { Id = Guid.NewGuid(), TenantId = TenantId, WorkOrderId = order.Id, ActorUserId = Actor(user), Action = "PreventiveChecklist.Recorded", Details = item.InstructionEn, OccurredAtUtc = Now });
        audit.Add("Maintenance.PreventiveChecklistRecorded", nameof(CorrectiveWorkOrder), order.Id, new { item.Id, item.Result }); await Save(ct);
        return await database.CorrectiveWorkOrders.AsNoTracking().IncludeWorkOrder().SingleAsync(x => x.Id == order.Id, ct);
    }

    public async Task<MaintenancePage<CorrectiveWorkOrder>> ListWorkOrders(ClaimsPrincipal user, WorkOrderListQuery queryInput, CancellationToken ct)
    {
        var query = AccessibleOrders(user, TenantPermissions.MaintenancePmView).AsNoTracking().IncludeWorkOrder();
        if (queryInput.Mine) query = query.Where(x => x.AssignedTechnicianId == Actor(user));
        if (!string.IsNullOrWhiteSpace(queryInput.Search)) { var term = $"%{queryInput.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Number, term) || EF.Functions.ILike(x.Description, term) || EF.Functions.ILike(x.Asset!.Code, term)); }
        if (queryInput.Priority is { } priority) query = query.Where(x => x.Priority == priority);
        if (queryInput.Status is { } status) query = query.Where(x => x.Status == status);
        if (queryInput.SiteId is { } site) query = query.Where(x => x.SiteId == site);
        if (queryInput.AssetId is { } asset) query = query.Where(x => x.AssetId == asset);
        if (queryInput.AssignedTechnicianId is { } technician) query = query.Where(x => x.AssignedTechnicianId == technician);
        var page = Math.Max(1, queryInput.Page); var size = Math.Clamp(queryInput.PageSize, 1, 100); var total = await query.CountAsync(ct);
        return new(await query.OrderBy(x => x.ScheduledDueAtUtc).ThenBy(x => x.Number).Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, total);
    }

    public async Task<IReadOnlyList<TechnicianOption>> Technicians(ClaimsPrincipal user, Guid siteId, CancellationToken ct)
    {
        RequireSite(user, TenantPermissions.MaintenancePmManage, siteId);
        return await database.TenantMemberships.AsNoTracking().Where(m => m.IsActive && m.User != null && m.User.IsActive &&
            m.Roles.Any(mr => mr.Role != null && mr.Role.IsActive && mr.Role.Permissions.Any(rp => rp.Permission != null && rp.Permission.Code == TenantPermissions.MaintenancePmExecute) &&
                (mr.ScopeKind == AccessScopeKind.Tenant || mr.ScopeKind == AccessScopeKind.Site && mr.ScopeId == siteId)))
            .OrderBy(m => m.User!.DisplayName).Select(m => new TechnicianOption(m.UserId, m.User!.DisplayName, m.User.Email ?? string.Empty)).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<MaintenanceAssetOption>> Assets(ClaimsPrincipal user, string? search, CancellationToken ct)
    {
        var query = scopes.HasTenantAccess(user, TenantPermissions.MaintenancePmCreate) ? database.Assets.AsNoTracking() : database.Assets.AsNoTracking().Where(x => GrantedSites(user, TenantPermissions.MaintenancePmCreate).Contains(x.SiteId));
        query = query.Where(x => x.LifecycleState == AssetLifecycleState.Active).Include(x => x.Site).Include(x => x.Location).Include(x => x.ResponsibleUser);
        if (!string.IsNullOrWhiteSpace(search)) { var term = $"%{search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Code, term) || EF.Functions.ILike(x.NameEn, term) || x.NameAr != null && EF.Functions.ILike(x.NameAr, term)); }
        return await query.OrderBy(x => x.Code).Take(100).Select(x => new MaintenanceAssetOption(x.Id, x.Code, x.NameEn, x.NameAr, x.SiteId, x.Site!.NameEn, x.Site.NameAr, x.Location == null ? null : x.Location.NameEn, x.Location == null ? null : x.Location.NameAr, x.ResponsibleUserId, x.ResponsibleUser == null ? null : x.ResponsibleUser.DisplayName)).ToListAsync(ct);
    }

    private async Task<int> GeneratePlans(IReadOnlyList<Guid> planIds, DateTimeOffset through, Guid? actor, CancellationToken ct)
    {
        var generated = 0;
        foreach (var planId in planIds)
        {
            var plan = await database.PreventiveMaintenancePlans.Include(x => x.Asset).ThenInclude(x => x!.Location).Include(x => x.ChecklistItems).SingleAsync(x => x.Id == planId, ct);
            if (plan.State != PreventivePlanState.Active || plan.Asset?.LifecycleState != AssetLifecycleState.Active) continue;
            for (var i = 0; i < MaxCatchUpPerPlan && plan.NextDueAtUtc <= through; i++)
            {
                var due = plan.NextDueAtUtc;
                if (!await database.PreventiveMaintenanceOccurrences.AnyAsync(x => x.PlanId == plan.Id && x.ScheduledDueAtUtc == due, ct))
                {
                    var occurrence = new PreventiveMaintenanceOccurrence { Id = Guid.NewGuid(), TenantId = TenantId, PlanId = plan.Id, ScheduledDueAtUtc = due, Status = PreventiveOccurrenceStatus.Generated, GeneratedAtUtc = Now };
                    var order = new CorrectiveWorkOrder { Id = Guid.NewGuid(), TenantId = TenantId, Number = Number("PWO"), Type = MaintenanceWorkOrderType.Preventive,
                        PreventiveMaintenancePlanId = plan.Id, PreventiveMaintenanceOccurrenceId = occurrence.Id, ScheduledDueAtUtc = due, AssetId = plan.AssetId, SiteId = plan.SiteId,
                        LocationSnapshot = plan.Asset.Location is null ? null : $"{plan.Asset.Location.Code} · {plan.Asset.Location.NameEn}", Priority = plan.Priority,
                        Description = plan.Description ?? plan.NameEn, PlannedStartUtc = due };
                    occurrence.WorkOrderId = order.Id; foreach (var item in plan.ChecklistItems.Where(x => x.IsActive).OrderBy(x => x.Sequence)) order.ChecklistResponses.Add(new PreventiveWorkOrderChecklistResponse
                    { Id = Guid.NewGuid(), TenantId = TenantId, WorkOrderId = order.Id, SourceChecklistItemId = item.Id, Sequence = item.Sequence, InstructionEn = item.InstructionEn, InstructionAr = item.InstructionAr, IsRequired = item.IsRequired, Guidance = item.Guidance });
                    if (plan.DefaultTechnicianId is { } technician && await CanExecute(technician, plan.SiteId, ct)) { order.AssignedTechnicianId = technician; order.AssignedById = actor; order.AssignedAtUtc = Now; order.Status = CorrectiveWorkOrderStatus.Assigned; }
                    database.PreventiveMaintenanceOccurrences.Add(occurrence); database.CorrectiveWorkOrders.Add(order);
                    database.WorkOrderActivities.Add(new WorkOrderActivity { Id = Guid.NewGuid(), TenantId = TenantId, WorkOrderId = order.Id, ActorUserId = actor, Action = "PreventiveWorkOrder.Generated", OccurredAtUtc = Now });
                    audit.Add("Maintenance.PreventiveWorkOrderGenerated", nameof(CorrectiveWorkOrder), order.Id, new { plan.Id, ScheduledDueAtUtc = due }); generated++;
                }
                plan.LastGeneratedAtUtc = Now; plan.NextDueAtUtc = NextDue(plan, due); plan.Version = Guid.NewGuid();
            }
            try { await Save(ct); } catch (ConflictException) { database.ChangeTracker.Clear(); }
        }
        return generated;
    }

    private static DateTimeOffset AdvancePast(DateTimeOffset current, PreventiveMaintenancePlan plan, DateTimeOffset threshold)
    {
        var next = current; for (var i = 0; i < 500 && next <= threshold; i++) next = NextDue(plan, next); return next;
    }
    private static DateTimeOffset NextDue(PreventiveMaintenancePlan plan, DateTimeOffset current)
    {
        var local = PreventiveRecurrence.LocalDate(current, plan.TimeZoneId); var next = PreventiveRecurrence.Next(plan.StartLocalDate, local, plan.RecurrenceType, plan.RecurrenceInterval);
        return PreventiveRecurrence.ToUtc(next, plan.ScheduledLocalTime, plan.TimeZoneId);
    }
    private async Task<Asset> ValidAsset(Guid id, CancellationToken ct)
    {
        var asset = await database.Assets.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ValidationException("An asset in this tenant is required.");
        if (asset.LifecycleState != AssetLifecycleState.Active) throw new ConflictException("A preventive plan cannot use a retired asset."); return asset;
    }
    private async Task ValidateTechnician(Guid? id, Guid siteId, CancellationToken ct) { if (id is { } value && !await CanExecute(value, siteId, ct)) throw new ValidationException("The default technician must be active and authorized at this site."); }
    private async Task<bool> CanExecute(Guid userId, Guid siteId, CancellationToken ct) => await database.TenantMemberships.AnyAsync(m => m.UserId == userId && m.IsActive && m.User != null && m.User.IsActive && m.Roles.Any(r => r.Role != null && r.Role.IsActive && r.Role.Permissions.Any(p => p.Permission != null && (p.Permission.Code == TenantPermissions.MaintenancePmExecute || p.Permission.Code == TenantPermissions.MaintenanceWorkOrdersExecute)) && (r.ScopeKind == AccessScopeKind.Tenant || r.ScopeKind == AccessScopeKind.Site && r.ScopeId == siteId)), ct);
    private void RequireExecutor(ClaimsPrincipal user, CorrectiveWorkOrder order) { var actor = Actor(user); if (order.AssignedTechnicianId != actor || !scopes.HasAccess(user, TenantPermissions.MaintenancePmExecute, order.SiteId)) throw new UnauthorizedAccessException("Only the assigned technician can execute this preventive work order."); }
    private static void Validate(PreventivePlanInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Code) || string.IsNullOrWhiteSpace(input.NameEn)) throw new ValidationException("Plan code and English name are required.");
        if (!Enum.IsDefined(input.Priority) || !Enum.IsDefined(input.RecurrenceType)) throw new ValidationException("Priority or recurrence type is invalid.");
        if (input.RecurrenceInterval is < 1 or > 365) throw new ValidationException("Recurrence interval must be between 1 and 365.");
        if (input.EstimatedDurationMinutes is < 0) throw new ValidationException("Estimated duration cannot be negative.");
        if (input.Checklist.Count > 200 || input.Checklist.Any(x => x.Sequence < 1 || string.IsNullOrWhiteSpace(x.InstructionEn)) || input.Checklist.Select(x => x.Sequence).Distinct().Count() != input.Checklist.Count) throw new ValidationException("Checklist items require a unique positive sequence and English instruction.");
    }
    private void AddChecklist(PreventiveMaintenancePlan plan, IReadOnlyList<PreventiveChecklistItemInput> items) { foreach (var x in items.OrderBy(x => x.Sequence)) plan.ChecklistItems.Add(new PreventiveMaintenanceChecklistItem { Id = Guid.NewGuid(), TenantId = TenantId, PlanId = plan.Id, Sequence = x.Sequence, InstructionEn = x.InstructionEn.Trim(), InstructionAr = Clean(x.InstructionAr), IsRequired = x.IsRequired, Guidance = Clean(x.Guidance), IsActive = x.IsActive }); }
    private IQueryable<PreventiveMaintenancePlan> AccessiblePlans(ClaimsPrincipal user, string permission) => scopes.HasTenantAccess(user, permission) ? database.PreventiveMaintenancePlans : database.PreventiveMaintenancePlans.Where(x => GrantedSites(user, permission).Contains(x.SiteId));
    private IQueryable<CorrectiveWorkOrder> AccessibleOrders(ClaimsPrincipal user, string permission) => (scopes.HasTenantAccess(user, permission) ? database.CorrectiveWorkOrders : database.CorrectiveWorkOrders.Where(x => GrantedSites(user, permission).Contains(x.SiteId))).Where(x => x.Type == MaintenanceWorkOrderType.Preventive);
    private static Guid[] GrantedSites(ClaimsPrincipal user, string permission) => user.FindAll(TenantClaims.Grant).Select(x => x.Value.Split('|')).Where(x => x.Length == 3 && x[0] == permission && x[1] == nameof(AccessScopeKind.Site) && Guid.TryParse(x[2], out _)).Select(x => Guid.Parse(x[2])).Distinct().ToArray();
    private void RequireSite(ClaimsPrincipal user, string permission, Guid siteId) { if (!scopes.HasAccess(user, permission, siteId)) throw new UnauthorizedAccessException("The requested site is outside the granted scope."); }
    private static Guid Actor(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue("sub"), out var id) ? id : throw new UnauthorizedAccessException();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Version(Guid current, Guid supplied) { if (supplied == Guid.Empty || current != supplied) throw new ConflictException("The record was changed by another user."); }
    private static void Range(DateTimeOffset from, DateTimeOffset to) { if (to < from || to - from > TimeSpan.FromDays(370)) throw new ValidationException("Date range must be ordered and no longer than 370 days."); }
    private static string Number(string prefix) => $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..(prefix.Length + 18)].ToUpperInvariant();
    private async Task Save(CancellationToken ct) { try { await database.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new ConflictException("The record was changed by another user."); } catch (DbUpdateException) { throw new ConflictException("The operation conflicts with an existing schedule or record."); } }
    private Task<PreventiveMaintenancePlan> Load(Guid id, CancellationToken ct) => database.PreventiveMaintenancePlans.AsNoTracking().IncludePlan().SingleAsync(x => x.Id == id, ct);
}

internal static class PreventiveQueryExtensions
{
    public static IQueryable<PreventiveMaintenancePlan> IncludePlan(this IQueryable<PreventiveMaintenancePlan> query) => query.Include(x => x.Asset).Include(x => x.DefaultTechnician).Include(x => x.CreatedBy).Include(x => x.ChecklistItems).Include(x => x.Occurrences).ThenInclude(x => x.WorkOrder);
}
