using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Locations.Application;
using SaaSPlatform.Api.Modules.Locations.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Tests.Locations;

public sealed class TenantAdministrationServiceTests
{
    [Fact]
    public async Task TenantFilterHidesOtherTenantSitesAndLocations()
    {
        var tenant = Guid.NewGuid(); var other = Guid.NewGuid(); var name = Guid.NewGuid().ToString();
        await using (var seed = Database(new Current(other), name))
        {
            var site = NewSite(other, "OTHER"); seed.Sites.Add(site);
            seed.LocationNodes.Add(NewNode(other, site.Id, Guid.NewGuid(), "FOREIGN")); await seed.SaveChangesAsync();
        }
        await using var db = Database(new Current(tenant), name);
        Assert.Empty(await db.Sites.ToListAsync());
        Assert.Empty(await db.LocationNodes.ToListAsync());
    }

    [Fact]
    public async Task SameSiteCodeIsAllowedAcrossTenants()
    {
        var first = Guid.NewGuid(); var second = Guid.NewGuid(); var name = Guid.NewGuid().ToString();
        await using (var db = Database(new Current(first), name)) { db.Sites.Add(NewSite(first, "SHARED")); await db.SaveChangesAsync(); }
        await using (var db = Database(new Current(second), name)) { db.Sites.Add(NewSite(second, "SHARED")); await db.SaveChangesAsync(); }
        await using var verify = Database(new Current(first), name);
        Assert.Equal(2, await verify.Sites.IgnoreQueryFilters().CountAsync(x => x.Code == "SHARED"));
    }

    [Fact]
    public async Task CreateRejectsParentFromAnotherSite()
    {
        var fixture = await Fixture();
        var second = NewSite(fixture.Tenant, "SECOND"); fixture.Db.Sites.Add(second);
        var parent = NewNode(fixture.Tenant, second.Id, fixture.Type.Id, "PARENT"); fixture.Db.LocationNodes.Add(parent);
        await fixture.Db.SaveChangesAsync();
        var input = new LocationInput(fixture.Site.Id, parent.Id, fixture.Type.Id, "CHILD", "Child", null, null, 0, Guid.Empty);
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Service.CreateLocation(fixture.User, input, default));
    }

    [Fact]
    public async Task MoveRejectsSelfAndDescendantCycles()
    {
        var f = await Fixture();
        var parent = NewNode(f.Tenant, f.Site.Id, f.Type.Id, "PARENT");
        var child = NewNode(f.Tenant, f.Site.Id, f.Type.Id, "CHILD", parent.Id);
        f.Db.LocationNodes.AddRange(parent, child); await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.MoveLocation(f.User, parent.Id,
            new MoveLocationInput(parent.Id, 0, parent.Version), default));
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.MoveLocation(f.User, parent.Id,
            new MoveLocationInput(child.Id, 0, parent.Version), default));
    }

    [Fact]
    public async Task ValidMoveUpdatesParentAndVersion()
    {
        var f = await Fixture(); var firstVersion = Guid.NewGuid();
        var parent = NewNode(f.Tenant, f.Site.Id, f.Type.Id, "PARENT");
        var child = NewNode(f.Tenant, f.Site.Id, f.Type.Id, "CHILD"); child.Version = firstVersion;
        f.Db.LocationNodes.AddRange(parent, child); await f.Db.SaveChangesAsync();
        var moved = await f.Service.MoveLocation(f.User, child.Id, new MoveLocationInput(parent.Id, 5, firstVersion), default);
        Assert.Equal(parent.Id, moved.ParentLocationId); Assert.NotEqual(firstVersion, moved.Version);
    }

    [Fact]
    public async Task DeactivateRejectsAnyActiveDescendant()
    {
        var f = await Fixture();
        var root = NewNode(f.Tenant, f.Site.Id, f.Type.Id, "ROOT");
        var middle = NewNode(f.Tenant, f.Site.Id, f.Type.Id, "MIDDLE", root.Id); middle.IsActive = false;
        var leaf = NewNode(f.Tenant, f.Site.Id, f.Type.Id, "LEAF", middle.Id);
        f.Db.LocationNodes.AddRange(root, middle, leaf); await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.SetLocationStatus(f.User, root.Id,
            new StatusInput(false, root.Version), default));
    }

    [Fact]
    public async Task SiteScopedGrantCannotAccessAnotherSite()
    {
        var f = await Fixture(); var other = NewSite(f.Tenant, "OTHER"); f.Db.Sites.Add(other); await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.GetLocations(f.User, other.Id, default));
        Assert.Empty(await f.Service.GetLocations(f.User, f.Site.Id, default));
    }

    [Fact]
    public async Task ForeignLocationTypeCannotBeUsedAndValidChildSucceeds()
    {
        var f = await Fixture();
        var foreignType = Guid.NewGuid();
        var invalid = new LocationInput(f.Site.Id, null, foreignType, "BAD", "Bad", null, null, 0, Guid.Empty);
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.CreateLocation(f.User, invalid, default));
        var parent = await f.Service.CreateLocation(f.User,
            new LocationInput(f.Site.Id, null, f.Type.Id, "BLD", "Building", null, null, 0, Guid.Empty), default);
        var child = await f.Service.CreateLocation(f.User,
            new LocationInput(f.Site.Id, parent.Id, f.Type.Id, "ROOM", "Room", null, null, 0, Guid.Empty), default);
        Assert.Equal(parent.Id, child.ParentLocationId);
    }

    [Fact]
    public async Task DuplicateCodesAreRejectedWithinBoundary()
    {
        var f = await Fixture();
        var tenantAdmin = User(TenantPermissions.SiteCreate, AccessScopeKind.Tenant, null);
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.CreateSite(tenantAdmin,
            new SiteInput("MAIN", "Duplicate", null, null, null, null, null, "UTC", null, null, Guid.Empty), default));
        await f.Service.CreateLocation(f.User, new LocationInput(f.Site.Id, null, f.Type.Id, "ROOM", "Room", null, null, 0, Guid.Empty), default);
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.CreateLocation(f.User,
            new LocationInput(f.Site.Id, null, f.Type.Id, "room", "Duplicate", null, null, 0, Guid.Empty), default));
    }

    [Fact]
    public async Task SiteCreateRequiresTenantScopeAndAuthorizedTenantAdminSucceeds()
    {
        var f = await Fixture();
        var input = new SiteInput("NEW", "New", null, null, null, null, null, "UTC", null, null, Guid.Empty);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.CreateSite(f.User, input, default));
        var created = await f.Service.CreateSite(User(TenantPermissions.SiteCreate, AccessScopeKind.Tenant, null), input, default);
        Assert.Equal("NEW", created.Code);
    }

    [Fact]
    public async Task UpdateExplicitlyRejectsCrossSiteMoveAndUnauthorizedModification()
    {
        var f = await Fixture(); var node = NewNode(f.Tenant, f.Site.Id, f.Type.Id, "NODE");
        var second = NewSite(f.Tenant, "SECOND"); f.Db.AddRange(node, second); await f.Db.SaveChangesAsync();
        var crossSite = new LocationInput(second.Id, null, f.Type.Id, node.Code, node.NameEn, null, null, 0, node.Version);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.UpdateLocation(new ClaimsPrincipal(), node.Id, crossSite, default));
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.UpdateLocation(
            User(TenantPermissions.LocationEdit, AccessScopeKind.Site, f.Site.Id), node.Id, crossSite, default));
    }

    [Fact]
    public void TenantGrantAllowsEverySiteWhileSiteGrantIsLimited()
    {
        var site = Guid.NewGuid(); var other = Guid.NewGuid(); var auth = new SiteScopeAuthorizer();
        Assert.True(auth.HasAccess(User(TenantPermissions.SiteView, AccessScopeKind.Tenant, null), TenantPermissions.SiteView, other));
        var scoped = User(TenantPermissions.SiteView, AccessScopeKind.Site, site);
        Assert.True(auth.HasAccess(scoped, TenantPermissions.SiteView, site));
        Assert.False(auth.HasAccess(scoped, TenantPermissions.SiteView, other));
        Assert.False(auth.HasTenantAccess(scoped, TenantPermissions.SiteView));
    }

    private static async Task<TestFixture> Fixture()
    {
        var tenant = Guid.NewGuid(); var db = Database(new Current(tenant));
        var site = NewSite(tenant, "MAIN"); var type = new LocationType { Id = Guid.NewGuid(), TenantId = tenant,
            Code = "ROOM", NameEn = "Room" };
        db.Sites.Add(site); db.LocationTypes.Add(type); await db.SaveChangesAsync();
        var user = User([TenantPermissions.LocationView, TenantPermissions.LocationCreate, TenantPermissions.LocationEdit,
            TenantPermissions.LocationMove, TenantPermissions.LocationDeactivate], AccessScopeKind.Site, site.Id);
        return new TestFixture(tenant, db, site, type, user,
            new TenantAdministrationService(db, new Current(tenant), new SiteScopeAuthorizer(), new NoAudit()));
    }

    private static ClaimsPrincipal User(string permission, AccessScopeKind kind, Guid? id) => User([permission], kind, id);
    private static ClaimsPrincipal User(IEnumerable<string> permissions, AccessScopeKind kind, Guid? id) =>
        new(new ClaimsIdentity(permissions.SelectMany(p => new[] { new Claim(TenantClaims.Permission, p),
            new Claim(TenantClaims.Grant, new PermissionGrant(p, kind, id).ToClaimValue()) }), "test"));
    private static Site NewSite(Guid tenant, string code) => new() { Id = Guid.NewGuid(), TenantId = tenant, Code = code, NameEn = code, TimeZone = "UTC" };
    private static LocationNode NewNode(Guid tenant, Guid site, Guid type, string code, Guid? parent = null) => new()
        { Id = Guid.NewGuid(), TenantId = tenant, SiteId = site, LocationTypeId = type, ParentLocationId = parent, Code = code, NameEn = code };
    private static PlatformDbContext Database(ICurrentTenant tenant, string? name = null) => new(
        new DbContextOptionsBuilder<PlatformDbContext>().UseInMemoryDatabase(name ?? Guid.NewGuid().ToString()).Options,
        tenant, TimeProvider.System);

    private sealed class Current(Guid id) : ICurrentTenant { public Guid? Id { get; private set; } = id; public void Initialize(Guid tenantId) => Id = tenantId; }
    private sealed class NoAudit : IAuditWriter { public void Add(string action, string entityType, Guid entityId, object? changes = null) { } }
    private sealed record TestFixture(Guid Tenant, PlatformDbContext Db, Site Site, LocationType Type, ClaimsPrincipal User, TenantAdministrationService Service);
}
