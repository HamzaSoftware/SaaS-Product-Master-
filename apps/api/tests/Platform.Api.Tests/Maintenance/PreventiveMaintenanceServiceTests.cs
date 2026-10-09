using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Assets.Domain;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Locations.Domain;
using SaaSPlatform.Api.Modules.Maintenance.Application;
using SaaSPlatform.Api.Modules.Maintenance.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;
using SaaSPlatform.Api.Modules.Tenancy.Domain;

namespace SaaSPlatform.Api.Tests.Maintenance;

public sealed class PreventiveMaintenanceServiceTests
{
    [Fact] public void DailyRecurrenceUsesInterval() => Assert.Equal(new DateOnly(2026, 1, 8), PreventiveRecurrence.Next(new(2026, 1, 1), new(2026, 1, 1), PreventiveRecurrenceType.Daily, 7));
    [Fact] public void WeeklyRecurrenceUsesCalendarWeeks() => Assert.Equal(new DateOnly(2026, 1, 15), PreventiveRecurrence.Next(new(2026, 1, 1), new(2026, 1, 1), PreventiveRecurrenceType.Weekly, 2));
    [Fact] public void MonthEndClampsAndRecoversAnchor() { var start = new DateOnly(2025, 1, 31); var feb = PreventiveRecurrence.Next(start, start, PreventiveRecurrenceType.Monthly, 1); Assert.Equal(new(2025, 2, 28), feb); Assert.Equal(new(2025, 3, 31), PreventiveRecurrence.Next(start, feb, PreventiveRecurrenceType.Monthly, 1)); }
    [Fact] public void LeapDayClampsAndRecovers() { var start = new DateOnly(2024, 2, 29); var next = PreventiveRecurrence.Next(start, start, PreventiveRecurrenceType.Annual, 1); Assert.Equal(new(2025, 2, 28), next); var leap = next; for (var i = 0; i < 3; i++) leap = PreventiveRecurrence.Next(start, leap, PreventiveRecurrenceType.Annual, 1); Assert.Equal(new(2028, 2, 29), leap); }
    [Fact] public void TimeZoneConversionPreservesLocalSchedule() { var utc = PreventiveRecurrence.ToUtc(new(2026, 7, 1), new(10, 0), "Asia/Riyadh"); Assert.Equal(7, utc.Hour); Assert.Equal(new DateOnly(2026, 7, 1), PreventiveRecurrence.LocalDate(utc, "Asia/Riyadh")); }

    [Fact]
    public async Task PlanCreationValidatesAssetAndSnapshotsChecklist()
    {
        await using var f = await Fixture.Create(); var plan = await f.CreatePlan();
        Assert.Equal(f.Asset.Id, plan.AssetId); Assert.Single(plan.ChecklistItems); Assert.Equal(f.Site.TimeZone, plan.TimeZoneId);
        f.Asset.LifecycleState = AssetLifecycleState.Retired; await f.Database.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.Create(f.Principal(f.Admin), f.Input("PM-2"), default));
    }

    [Fact]
    public async Task SiteScopeAndTenantIsolationAreEnforced()
    {
        await using var f = await Fixture.Create(); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.Create(f.Principal(f.Admin, (TenantPermissions.MaintenancePmCreate, AccessScopeKind.Site, Guid.NewGuid())), f.Input(), default));
        var plan = await f.CreatePlan(); f.Current.Set(Guid.NewGuid()); Assert.Empty(await f.Database.PreventiveMaintenancePlans.ToListAsync()); f.Current.Set(f.TenantId); Assert.NotNull(await f.Service.GetPlan(f.Principal(f.Admin), plan.Id, default));
    }

    [Fact]
    public async Task PausedAndArchivedPlansDoNotGenerate()
    {
        await using var f = await Fixture.Create(); var plan = await f.CreatePlan();
        plan = await f.Service.ChangeStatus(f.Principal(f.Admin), plan.Id, new(PreventivePlanState.Paused, plan.Version), default);
        Assert.Equal(0, (await f.Service.Generate(f.Principal(f.Admin), f.Clock.GetUtcNow().AddDays(30), default)).Generated);
        plan = await f.Service.ChangeStatus(f.Principal(f.Admin), plan.Id, new(PreventivePlanState.Archived, plan.Version), default);
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.ChangeStatus(f.Principal(f.Admin), plan.Id, new(PreventivePlanState.Active, plan.Version), default));
    }

    [Fact]
    public async Task GenerationIsIdempotentBoundedAndPreservesDueDate()
    {
        await using var f = await Fixture.Create(); await f.CreatePlan(start: new DateOnly(2025, 1, 1));
        var first = await f.Service.Generate(f.Principal(f.Admin), f.Clock.GetUtcNow(), default); var second = await f.Service.Generate(f.Principal(f.Admin), f.Clock.GetUtcNow(), default);
        Assert.Equal(3, first.Generated); Assert.Equal(3, second.Generated);
        var occurrences = await f.Database.PreventiveMaintenanceOccurrences.OrderBy(x => x.ScheduledDueAtUtc).ToListAsync();
        Assert.Equal(6, occurrences.Count); Assert.Equal(occurrences.Count, occurrences.Select(x => new { x.PlanId, x.ScheduledDueAtUtc }).Distinct().Count());
        Assert.All(await f.Database.CorrectiveWorkOrders.ToListAsync(), x => Assert.Equal(x.ScheduledDueAtUtc, x.PlannedStartUtc));
    }

    [Fact]
    public async Task GeneratedChecklistIsImmutableSnapshot()
    {
        await using var f = await Fixture.Create(); await f.CreatePlan(); await f.Service.Generate(f.Principal(f.Admin), f.Clock.GetUtcNow().AddDays(1), default);
        var order = await f.Database.CorrectiveWorkOrders.Include(x => x.ChecklistResponses).SingleAsync(); var original = order.ChecklistResponses.Single().InstructionEn;
        var definition = await f.Database.PreventiveMaintenanceChecklistItems.SingleAsync(); definition.InstructionEn = "Changed instruction"; await f.Database.SaveChangesAsync();
        Assert.Equal(original, (await f.Database.PreventiveWorkOrderChecklistResponses.SingleAsync()).InstructionEn);
    }

    [Fact]
    public async Task RequiredChecklistAndFailedNotesAreEnforcedBeforeSubmission()
    {
        await using var f = await Fixture.Create(); await f.CreatePlan(); await f.Service.Generate(f.Principal(f.Admin), f.Clock.GetUtcNow().AddDays(1), default);
        var order = await f.Database.CorrectiveWorkOrders.Include(x => x.ChecklistResponses).SingleAsync(); order = await f.Corrective.Assign(f.Principal(f.Admin), order.Id, new(f.Technician.Id, null, order.Version), default);
        order = await f.Corrective.Start(f.Principal(f.Technician, (TenantPermissions.MaintenancePmExecute, AccessScopeKind.Tenant, null)), order.Id, order.Version, default);
        var start = f.Clock.GetUtcNow().AddHours(-1); order = await f.Corrective.SaveReport(f.Principal(f.Technician, (TenantPermissions.MaintenancePmExecute, AccessScopeKind.Tenant, null)), order.Id, new("Inspection", "Serviced", RepairOutcome.Resolved, start, start.AddMinutes(30), 30, null, order.Version), default);
        await Assert.ThrowsAsync<ValidationException>(() => f.Corrective.SubmitReport(f.Principal(f.Technician, (TenantPermissions.MaintenancePmExecute, AccessScopeKind.Tenant, null)), order.Id, order.Version, default));
        var checklistId = order.ChecklistResponses.Single().Id;
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.RecordChecklist(f.Principal(f.Technician, (TenantPermissions.MaintenancePmExecute, AccessScopeKind.Tenant, null)), order.Id, new(checklistId, ChecklistResult.Fail, null, order.Version), default));
        order = await f.Service.RecordChecklist(f.Principal(f.Technician, (TenantPermissions.MaintenancePmExecute, AccessScopeKind.Tenant, null)), order.Id, new(checklistId, ChecklistResult.Pass, null, order.Version), default);
        order = await f.Corrective.SubmitReport(f.Principal(f.Technician, (TenantPermissions.MaintenancePmExecute, AccessScopeKind.Tenant, null)), order.Id, order.Version, default); Assert.Equal(CorrectiveWorkOrderStatus.PendingVerification, order.Status);
        order = await f.Corrective.Verify(f.Principal(f.Admin, (TenantPermissions.MaintenancePmVerify, AccessScopeKind.Tenant, null)), order.Id, new(true, "PM work verified", order.Version), default);
        Assert.Equal(CorrectiveWorkOrderStatus.Completed, order.Status);
    }

    [Fact]
    public async Task OccurrenceUniquenessIsAConcurrentGenerationDatabaseGuard()
    {
        await using var f = await Fixture.Create();
        var entity = f.Database.Model.FindEntityType(typeof(PreventiveMaintenanceOccurrence));
        Assert.NotNull(entity);
        Assert.Contains(entity!.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(PreventiveMaintenanceOccurrence.TenantId),
                nameof(PreventiveMaintenanceOccurrence.PlanId),
                nameof(PreventiveMaintenanceOccurrence.ScheduledDueAtUtc)]));
    }

    [Fact]
    public async Task StalePlanUpdateIsRejected()
    {
        await using var f = await Fixture.Create(); var plan = await f.CreatePlan(); await Assert.ThrowsAsync<ConflictException>(() => f.Service.Update(f.Principal(f.Admin), plan.Id, f.Input() with { Version = Guid.NewGuid() }, default));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider provider;
        public Guid TenantId { get; } public MutableTenant Current { get; } public TestClock Clock { get; }
        public PlatformDbContext Database { get; } public PreventiveMaintenanceService Service { get; } public CorrectiveMaintenanceService Corrective { get; }
        public Site Site { get; } public Asset Asset { get; } public PlatformUser Admin { get; } public PlatformUser Technician { get; }
        private Fixture(ServiceProvider provider, Guid tenantId, MutableTenant current, TestClock clock, PlatformDbContext db, PreventiveMaintenanceService service, CorrectiveMaintenanceService corrective, Site site, Asset asset, PlatformUser admin, PlatformUser technician)
        { this.provider = provider; TenantId = tenantId; Current = current; Clock = clock; Database = db; Service = service; Corrective = corrective; Site = site; Asset = asset; Admin = admin; Technician = technician; }
        public static async Task<Fixture> Create()
        {
            var tenantId = Guid.NewGuid(); var current = new MutableTenant(tenantId); var clock = new TestClock(new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero)); var services = new ServiceCollection();
            services.AddSingleton<ICurrentTenant>(current); services.AddSingleton<TimeProvider>(clock); services.AddHttpContextAccessor(); services.AddDbContext<PlatformDbContext>(x => x.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            var provider = services.BuildServiceProvider(); var db = provider.GetRequiredService<PlatformDbContext>(); var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = $"tenant-{tenantId:N}" };
            var site = new Site { Id = Guid.NewGuid(), TenantId = tenantId, Code = "MAIN", NameEn = "Main", TimeZone = "Asia/Riyadh" }; var category = new AssetCategory { Id = Guid.NewGuid(), TenantId = tenantId, Code = "EQ", NameEn = "Equipment" };
            var admin = User("admin"); var tech = User("tech"); db.AddRange(tenant, site, category, admin, tech); var asset = new Asset { Id = Guid.NewGuid(), TenantId = tenantId, Code = "A-1", NameEn = "Pump", CategoryId = category.Id, SiteId = site.Id }; db.Add(asset);
            var permission = new PermissionDefinition { Id = Guid.NewGuid(), Code = TenantPermissions.MaintenancePmExecute, Module = "Maintenance" }; var role = new TenantRole { Id = Guid.NewGuid(), TenantId = tenantId, Name = "PM Technician" }; var grant = new RolePermissionGrant { TenantId = tenantId, RoleId = role.Id, PermissionId = permission.Id, Role = role, Permission = permission }; role.Permissions.Add(grant); db.AddRange(permission, role, grant);
            db.TenantMemberships.AddRange(new TenantMembership { Id = Guid.NewGuid(), TenantId = tenantId, UserId = admin.Id, User = admin, Tenant = tenant }, new TenantMembership { Id = Guid.NewGuid(), TenantId = tenantId, UserId = tech.Id, User = tech, Tenant = tenant }); await db.SaveChangesAsync();
            var membership = await db.TenantMemberships.SingleAsync(x => x.UserId == tech.Id); db.MembershipRoles.Add(new MembershipRole { Id = Guid.NewGuid(), TenantId = tenantId, MembershipId = membership.Id, Membership = membership, RoleId = role.Id, Role = role, ScopeKind = AccessScopeKind.Tenant }); await db.SaveChangesAsync();
            var scopes = new SiteScopeAuthorizer(); var audit = new NoAudit(); var service = new PreventiveMaintenanceService(db, current, scopes, audit, clock); var corrective = new CorrectiveMaintenanceService(db, current, scopes, audit, clock);
            return new(provider, tenantId, current, clock, db, service, corrective, site, asset, admin, tech);
        }
        public PreventivePlanInput Input(string code = "PM-1", DateOnly? start = null) => new(code, "Monthly service", "صيانة شهرية", "Inspect and service", Asset.Id, MaintenancePriority.Medium, start ?? new DateOnly(2026, 9, 29), new TimeOnly(10, 0), PreventiveRecurrenceType.Monthly, 1, 60, null, [new(null, 1, "Inspect filter", "فحص المرشح", true, "Record condition")], Guid.Empty);
        public Task<PreventiveMaintenancePlan> CreatePlan(string code = "PM-1", DateOnly? start = null) => Service.Create(Principal(Admin), Input(code, start), default);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822")]
        public ClaimsPrincipal Principal(PlatformUser actor, params (string Permission, AccessScopeKind Scope, Guid? ScopeId)[] grants) { var effective = grants.Length == 0 ? TenantPermissions.All.Select(x => (x, AccessScopeKind.Tenant, (Guid?)null)) : grants; var claims = new List<Claim> { new("sub", actor.Id.ToString()) }; foreach (var g in effective) { claims.Add(new(TenantClaims.Permission, g.Item1)); claims.Add(new(TenantClaims.Grant, new PermissionGrant(g.Item1, g.Item2, g.Item3).ToClaimValue())); } return new(new ClaimsIdentity(claims, "test")); }
        private static PlatformUser User(string name) => new() { Id = Guid.NewGuid(), UserName = $"{name}@test.local", Email = $"{name}@test.local", DisplayName = name, EmailConfirmed = true };
        public async ValueTask DisposeAsync() { await Database.DisposeAsync(); await provider.DisposeAsync(); }
    }
    private sealed class MutableTenant(Guid id) : ICurrentTenant { public Guid? Id { get; private set; } = id; public void Initialize(Guid tenantId) => Set(tenantId); public void Set(Guid tenantId) => Id = tenantId; }
    private sealed class TestClock(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }
    private sealed class NoAudit : IAuditWriter { public void Add(string action, string entityType, Guid entityId, object? changes = null) { } }
}
