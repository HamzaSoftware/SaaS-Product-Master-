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

public sealed class CorrectiveMaintenanceServiceTests
{
    [Fact]
    public async Task BreakdownCreationDerivesAssetPlacementAndAuditIdentity()
    {
        await using var f = await Fixture.Create(); var report = await f.CreateBreakdown();
        Assert.Equal(f.Asset.SiteId, report.SiteId); Assert.StartsWith("BR-", report.Number); Assert.Equal(f.Reporter.Id, report.ReportedById);
    }

    [Fact]
    public async Task RetiredOrUnknownAssetIsRejected()
    {
        await using var f = await Fixture.Create(); f.Asset.LifecycleState = AssetLifecycleState.Retired; await f.Database.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.CreateBreakdown(f.Principal(f.Reporter), f.BreakdownInput(f.Asset.Id), default));
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.CreateBreakdown(f.Principal(f.Reporter), f.BreakdownInput(Guid.NewGuid()), default));
    }

    [Fact]
    public async Task SiteScopeAndTenantFiltersProtectBreakdowns()
    {
        await using var f = await Fixture.Create(); await f.CreateBreakdown();
        var denied = f.Principal(f.Other, (TenantPermissions.MaintenanceBreakdownsView, AccessScopeKind.Site, Guid.NewGuid()));
        Assert.Empty((await f.Service.ListBreakdowns(denied, new(null, null, null, null, null, 1, 25), default)).Items);
        f.Current.Initialize(Guid.NewGuid()); Assert.Empty(await f.Database.BreakdownReports.ToListAsync());
    }

    [Fact]
    public async Task ReporterCannotSelfApproveAndRejectionRequiresReason()
    {
        await using var f = await Fixture.Create(); var report = await f.CreateBreakdown();
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.ReviewBreakdown(f.Principal(f.Reporter), report.Id, new(true, null, report.Version), default));
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.ReviewBreakdown(f.Principal(f.Reviewer), report.Id, new(false, "", report.Version), default));
    }

    [Fact]
    public async Task ApprovalCreatesReviewAndRejectsSecondTransition()
    {
        await using var f = await Fixture.Create(); var report = await f.CreateBreakdown();
        var approved = await f.Service.ReviewBreakdown(f.Principal(f.Reviewer), report.Id, new(true, null, report.Version), default);
        Assert.Equal(BreakdownStatus.Approved, approved.Status); Assert.Single(approved.Reviews);
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.ReviewBreakdown(f.Principal(f.Reviewer), report.Id, new(false, "late", approved.Version), default));
    }

    [Fact]
    public async Task OneApprovedBreakdownCreatesOnlyOneWorkOrder()
    {
        await using var f = await Fixture.Create(); var (report, order) = await f.CreateOrder();
        Assert.Equal(BreakdownStatus.ConvertedToWorkOrder, report.Status); Assert.StartsWith("CWO-", order.Number);
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.CreateWorkOrder(f.Principal(f.Reviewer), new(report.Id, null, null, report.Version), default));
    }

    [Fact]
    public async Task AssignmentRequiresActiveAuthorizedTechnician()
    {
        await using var f = await Fixture.Create(); var (_, order) = await f.CreateOrder();
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.Assign(f.Principal(f.Reviewer), order.Id, new(f.Other.Id, null, order.Version), default));
        var assigned = await f.Service.Assign(f.Principal(f.Reviewer), order.Id, new(f.Technician.Id, "dispatch", order.Version), default);
        Assert.Equal(CorrectiveWorkOrderStatus.Assigned, assigned.Status); Assert.Single(assigned.Assignments);
    }

    [Fact]
    public async Task UnrelatedTechnicianCannotExecuteAssignedWork()
    {
        await using var f = await Fixture.Create(); var order = await f.AssignedOrder();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.Start(f.Principal(f.Other, (TenantPermissions.MaintenanceWorkOrdersExecute, AccessScopeKind.Tenant, null)), order.Id, order.Version, default));
        var started = await f.Service.Start(f.Principal(f.Technician), order.Id, order.Version, default); Assert.Equal(CorrectiveWorkOrderStatus.InProgress, started.Status);
    }

    [Fact]
    public async Task IncompleteMaintenanceReportCannotBeSubmitted()
    {
        await using var f = await Fixture.Create(); var order = await f.StartedOrder();
        order = await f.Service.SaveReport(f.Principal(f.Technician), order.Id, new(null, null, null, order.ActualStartUtc, null, null, null, order.Version), default);
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.SubmitReport(f.Principal(f.Technician), order.Id, order.Version, default));
    }

    [Fact]
    public async Task SubmittedReportIsImmutableAndVerificationClosesWorkOrder()
    {
        await using var f = await Fixture.Create(); var order = await f.SubmittedOrder();
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.SaveReport(f.Principal(f.Technician), order.Id, f.CompleteReport(order.Version), default));
        var closed = await f.Service.Verify(f.Principal(f.Reviewer), order.Id, new(true, "verified", order.Version), default);
        Assert.Equal(CorrectiveWorkOrderStatus.Completed, closed.Status); Assert.Equal(MaintenanceReportStatus.Verified, closed.MaintenanceReport!.Status); Assert.NotNull(closed.ClosedAtUtc);
    }

    [Fact]
    public async Task TechnicianCannotSelfVerifyAndReturnRequiresReason()
    {
        await using var f = await Fixture.Create(); var order = await f.SubmittedOrder();
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.Verify(f.Principal(f.Technician, (TenantPermissions.MaintenanceWorkOrdersVerify, AccessScopeKind.Tenant, null)), order.Id, new(true, null, order.Version), default));
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.Verify(f.Principal(f.Reviewer), order.Id, new(false, "", order.Version), default));
        var returned = await f.Service.Verify(f.Principal(f.Reviewer), order.Id, new(false, "Add evidence", order.Version), default);
        Assert.Equal(CorrectiveWorkOrderStatus.ReturnedForCorrection, returned.Status); Assert.Equal(MaintenanceReportStatus.Returned, returned.MaintenanceReport!.Status);
    }

    [Fact]
    public async Task StaleVersionsAndUnsafeClosureAreRejected()
    {
        await using var f = await Fixture.Create(); var order = await f.AssignedOrder();
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.Start(f.Principal(f.Technician), order.Id, Guid.NewGuid(), default));
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.Verify(f.Principal(f.Reviewer), order.Id, new(true, null, order.Version), default));
    }

    [Fact]
    public async Task WorkNotesAndLifecycleEventsRemainInHistory()
    {
        await using var f = await Fixture.Create(); var order = await f.StartedOrder();
        var updated = await f.Service.AddActivity(f.Principal(f.Technician), order.Id, new("Replaced damaged coupling", order.Version), default);
        Assert.Contains(updated.Activities, x => x.Action == "WorkOrder.Started"); Assert.Contains(updated.Activities, x => x.Action == "WorkOrder.NoteAdded" && x.Details != null);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider provider;
        public MutableTenant Current { get; }
        public PlatformDbContext Database { get; }
        public CorrectiveMaintenanceService Service { get; }
        public Site Site { get; }
        public Asset Asset { get; }
        public PlatformUser Reporter { get; }
        public PlatformUser Reviewer { get; }
        public PlatformUser Technician { get; }
        public PlatformUser Other { get; }

        private Fixture(ServiceProvider provider, MutableTenant current, PlatformDbContext database, CorrectiveMaintenanceService service,
            Site site, Asset asset, PlatformUser reporter, PlatformUser reviewer, PlatformUser technician, PlatformUser other)
        { this.provider = provider; Current = current; Database = database; Service = service; Site = site; Asset = asset; Reporter = reporter; Reviewer = reviewer; Technician = technician; Other = other; }

        public static async Task<Fixture> Create()
        {
            var tenantId = Guid.NewGuid(); var current = new MutableTenant(tenantId); var services = new ServiceCollection();
            services.AddSingleton<ICurrentTenant>(current); services.AddSingleton(TimeProvider.System); services.AddHttpContextAccessor();
            services.AddDbContext<PlatformDbContext>(x => x.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            var provider = services.BuildServiceProvider(); var db = provider.GetRequiredService<PlatformDbContext>();
            var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = $"tenant-{tenantId:N}" }; db.Tenants.Add(tenant);
            var site = new Site { Id = Guid.NewGuid(), TenantId = tenantId, Code = "MAIN", NameEn = "Main", TimeZone = "UTC" };
            var category = new AssetCategory { Id = Guid.NewGuid(), TenantId = tenantId, Code = "EQ", NameEn = "Equipment" };
            var reporter = User("reporter"); var reviewer = User("reviewer"); var technician = User("technician"); var other = User("other");
            db.Users.AddRange(reporter, reviewer, technician, other); db.Sites.Add(site); db.AssetCategories.Add(category);
            var asset = new Asset { Id = Guid.NewGuid(), TenantId = tenantId, Code = "A-001", NameEn = "Pump", CategoryId = category.Id, SiteId = site.Id };
            db.Assets.Add(asset);
            var permission = new PermissionDefinition { Id = Guid.NewGuid(), Code = TenantPermissions.MaintenanceWorkOrdersExecute, Module = "Maintenance" };
            var role = new TenantRole { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Technician" }; var grant = new RolePermissionGrant { TenantId = tenantId, RoleId = role.Id, PermissionId = permission.Id, Role = role, Permission = permission }; role.Permissions.Add(grant);
            db.Permissions.Add(permission); db.TenantRoles.Add(role); db.RolePermissions.Add(grant);
            foreach (var user in new[] { reporter, reviewer, technician, other }) db.TenantMemberships.Add(new TenantMembership { Id = Guid.NewGuid(), TenantId = tenantId, UserId = user.Id, User = user, Tenant = tenant });
            await db.SaveChangesAsync();
            var membership = await db.TenantMemberships.SingleAsync(x => x.UserId == technician.Id);
            db.MembershipRoles.Add(new MembershipRole { Id = Guid.NewGuid(), TenantId = tenantId, MembershipId = membership.Id, Membership = membership, RoleId = role.Id, Role = role, ScopeKind = AccessScopeKind.Tenant }); await db.SaveChangesAsync();
            var service = new CorrectiveMaintenanceService(db, current, new SiteScopeAuthorizer(), new NoAudit(), TimeProvider.System);
            return new(provider, current, db, service, site, asset, reporter, reviewer, technician, other);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822")]
        public BreakdownCreateInput BreakdownInput(Guid assetId) => new(assetId, "Pump stopped", "Unexpected shutdown", "Noise", DateTimeOffset.UtcNow.AddMinutes(-10), MaintenancePriority.High);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822")]
        public ClaimsPrincipal Principal(PlatformUser actor, params (string Permission, AccessScopeKind Scope, Guid? ScopeId)[] grants)
        {
            var effective = grants.Length == 0 ? TenantPermissions.All.Select(x => (x, AccessScopeKind.Tenant, (Guid?)null)) : grants;
            var claims = new List<Claim> { new("sub", actor.Id.ToString()) };
            foreach (var grant in effective) { claims.Add(new(TenantClaims.Permission, grant.Item1)); claims.Add(new(TenantClaims.Grant, new PermissionGrant(grant.Item1, grant.Item2, grant.Item3).ToClaimValue())); }
            return new(new ClaimsIdentity(claims, "test"));
        }
        public async Task<BreakdownReport> CreateBreakdown() => await Service.CreateBreakdown(Principal(Reporter), BreakdownInput(Asset.Id), default);
        public async Task<(BreakdownReport Report, CorrectiveWorkOrder Order)> CreateOrder()
        {
            var report = await CreateBreakdown(); report = await Service.ReviewBreakdown(Principal(Reviewer), report.Id, new(true, null, report.Version), default);
            var order = await Service.CreateWorkOrder(Principal(Reviewer), new(report.Id, null, null, report.Version), default);
            report = await Service.GetBreakdown(Principal(Reviewer), report.Id, default); return (report, order);
        }
        public async Task<CorrectiveWorkOrder> AssignedOrder() { var (_, order) = await CreateOrder(); return await Service.Assign(Principal(Reviewer), order.Id, new(Technician.Id, null, order.Version), default); }
        public async Task<CorrectiveWorkOrder> StartedOrder() { var order = await AssignedOrder(); return await Service.Start(Principal(Technician), order.Id, order.Version, default); }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822")]
        public MaintenanceReportInput CompleteReport(Guid version) { var start = DateTimeOffset.UtcNow.AddHours(-1); return new("Failed bearing", "Replaced bearing and tested", RepairOutcome.Resolved, start, start.AddMinutes(50), 50, "Stable", version); }
        public async Task<CorrectiveWorkOrder> SubmittedOrder() { var order = await StartedOrder(); order = await Service.SaveReport(Principal(Technician), order.Id, CompleteReport(order.Version), default); return await Service.SubmitReport(Principal(Technician), order.Id, order.Version, default); }
        private static PlatformUser User(string name) => new() { Id = Guid.NewGuid(), UserName = $"{name}@example.test", Email = $"{name}@example.test", DisplayName = name, EmailConfirmed = true };
        public async ValueTask DisposeAsync() { await Database.DisposeAsync(); await provider.DisposeAsync(); }
    }

    private sealed class MutableTenant(Guid id) : ICurrentTenant { public Guid? Id { get; private set; } = id; public void Initialize(Guid tenantId) => Id = tenantId; }
    private sealed class NoAudit : IAuditWriter { public void Add(string action, string entityType, Guid entityId, object? changes = null) { } }
}
