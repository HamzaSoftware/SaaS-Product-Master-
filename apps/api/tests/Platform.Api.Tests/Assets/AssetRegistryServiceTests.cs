using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Assets.Application;
using SaaSPlatform.Api.Modules.Assets.Domain;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Locations.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;
using SaaSPlatform.Api.Modules.Tenancy.Domain;

namespace SaaSPlatform.Api.Tests.Assets;

public sealed class AssetRegistryServiceTests
{
    [Fact]
    public async Task CreateValidAssetAndAppendHistory()
    {
        var f = await Fixture(); var created = await f.Service.Create(f.TenantUser, Input(f), default);
        Assert.Equal("AHU-001", created.Code); Assert.Equal(f.Location.Id, created.LocationId);
        var history = await f.Service.History(f.TenantUser, created.Id, default);
        Assert.Single(history); Assert.Equal("Asset.Created", history[0].Action);
    }

    [Fact]
    public async Task DuplicateAssetCodeIsRejected()
    {
        var f = await Fixture(); await f.Service.Create(f.TenantUser, Input(f), default);
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.Create(f.TenantUser, Input(f) with { Code = "ahu-001" }, default));
    }

    [Fact]
    public async Task TenantFilterHidesForeignAssets()
    {
        var name = Guid.NewGuid().ToString(); var first = Guid.NewGuid(); var second = Guid.NewGuid();
        await using (var foreign = Database(new Current(second), name)) { foreign.Assets.Add(new Asset { Id = Guid.NewGuid(), TenantId = second, Code = "FOREIGN", NameEn = "Foreign", CategoryId = Guid.NewGuid(), SiteId = Guid.NewGuid() }); await foreign.SaveChangesAsync(); }
        await using var current = Database(new Current(first), name); Assert.Empty(await current.Assets.ToListAsync());
    }

    [Fact]
    public async Task UnknownTenantReferenceIsRejected()
    {
        var f = await Fixture();
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.Create(f.TenantUser, Input(f) with { CategoryId = Guid.NewGuid() }, default));
    }

    [Fact]
    public async Task CrossSiteLocationIsRejected()
    {
        var f = await Fixture();
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.Create(f.TenantUser, Input(f) with { LocationId = f.OtherLocation.Id }, default));
    }

    [Fact]
    public async Task SiteScopedUserCannotCreateOutsideGrant()
    {
        var f = await Fixture(); var user = User([TenantPermissions.AssetsCreate], f.Site.Id);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.Create(user, Input(f) with { SiteId = f.OtherSite.Id, LocationId = f.OtherLocation.Id }, default));
    }

    [Fact]
    public async Task RelocationRequiresSourceAndDestinationAccessAndCreatesHistory()
    {
        var f = await Fixture(); var asset = await f.Service.Create(f.TenantUser, Input(f), default);
        var sourceOnly = User([TenantPermissions.AssetsRelocate], f.Site.Id);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.Relocate(sourceOnly, asset.Id, new AssetRelocationInput(f.OtherSite.Id, f.OtherLocation.Id, asset.Version), default));
        var moved = await f.Service.Relocate(f.TenantUser, asset.Id, new AssetRelocationInput(f.OtherSite.Id, f.OtherLocation.Id, asset.Version), default);
        Assert.Equal(f.OtherSite.Id, moved.SiteId); Assert.Contains(await f.Service.History(f.TenantUser, asset.Id, default), x => x.Action == "Asset.Relocated");
    }

    [Fact]
    public async Task InvalidStatusTransitionIsRejected()
    {
        var f = await Fixture(); var asset = await f.Service.Create(f.TenantUser, Input(f) with { OperationalStatus = AssetOperationalStatus.UnderMaintenance }, default);
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.ChangeStatus(f.TenantUser, asset.Id, new AssetStatusInput(AssetOperationalStatus.Standby, asset.Version), default));
    }

    [Fact]
    public async Task InvalidDatesAndNegativeCostAreRejected()
    {
        var f = await Fixture();
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.Create(f.TenantUser, Input(f) with { PurchaseCost = -1, Currency = "SAR" }, default));
        await Assert.ThrowsAsync<ValidationException>(() => f.Service.Create(f.TenantUser, Input(f) with { WarrantyStartDate = new DateOnly(2026, 2, 1), WarrantyExpiryDate = new DateOnly(2026, 1, 1) }, default));
    }

    [Fact]
    public async Task StaleVersionIsRejected()
    {
        var f = await Fixture(); var asset = await f.Service.Create(f.TenantUser, Input(f), default);
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.ChangeStatus(f.TenantUser, asset.Id, new AssetStatusInput(AssetOperationalStatus.Standby, Guid.NewGuid()), default));
    }

    [Fact]
    public async Task RetirementBlocksUpdatesAndCanReactivateWithValidReferences()
    {
        var f = await Fixture(); var asset = await f.Service.Create(f.TenantUser, Input(f), default);
        var retired = await f.Service.SetLifecycle(f.TenantUser, asset.Id, new AssetLifecycleInput(true, asset.Version), default);
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.Update(f.TenantUser, asset.Id, Update(retired), default));
        var active = await f.Service.SetLifecycle(f.TenantUser, asset.Id, new AssetLifecycleInput(false, retired.Version), default);
        Assert.Equal(AssetLifecycleState.Active, active.LifecycleState);
    }

    [Fact]
    public async Task ActiveAssetPreventsMasterDataDeactivation()
    {
        var f = await Fixture(); await f.Service.Create(f.TenantUser, Input(f), default);
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.SetCategoryStatus(f.Category.Id, new MasterDataStatusInput(false, f.Category.Version), default));
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.SetManufacturerStatus(f.Manufacturer.Id, new MasterDataStatusInput(false, f.Manufacturer.Version), default));
        await Assert.ThrowsAsync<ConflictException>(() => f.Service.SetModelStatus(f.Model.Id, new MasterDataStatusInput(false, f.Model.Version), default));
    }

    private static async Task<FixtureData> Fixture()
    {
        var tenant = Guid.NewGuid(); var current = new Current(tenant); var db = Database(current);
        var site = Site(tenant, "MAIN"); var other = Site(tenant, "OTHER");
        var type = new LocationType { Id = Guid.NewGuid(), TenantId = tenant, Code = "ROOM", NameEn = "Room" };
        var location = Location(tenant, site.Id, type.Id, "ROOM-1"); var otherLocation = Location(tenant, other.Id, type.Id, "ROOM-2");
        var category = new AssetCategory { Id = Guid.NewGuid(), TenantId = tenant, Code = "HVAC", NameEn = "HVAC" };
        var manufacturer = new Manufacturer { Id = Guid.NewGuid(), TenantId = tenant, Name = "Carrier", NormalizedName = "CARRIER" };
        var model = new AssetModel { Id = Guid.NewGuid(), TenantId = tenant, ManufacturerId = manufacturer.Id, CategoryId = category.Id, Name = "A1", NormalizedName = "A1" };
        var responsible = new PlatformUser { Id = Guid.NewGuid(), UserName = "owner@example.com", Email = "owner@example.com", DisplayName = "Owner" };
        db.AddRange(site, other, type, location, otherLocation, category, manufacturer, model, responsible,
            new TenantMembership { Id = Guid.NewGuid(), TenantId = tenant, UserId = responsible.Id });
        await db.SaveChangesAsync();
        var context = new DefaultHttpContext(); context.TraceIdentifier = "asset-test"; context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", responsible.Id.ToString())], "test"));
        var service = new AssetRegistryService(db, current, new SiteScopeAuthorizer(), new NoAudit(), new HttpContextAccessor { HttpContext = context }, TimeProvider.System);
        return new FixtureData(db, service, site, other, location, otherLocation, category, manufacturer, model, responsible, User(TenantPermissions.All));
    }

    private static AssetCreateInput Input(FixtureData f) => new("AHU-001", "Air handling unit", "وحدة مناولة الهواء", f.Category.Id, f.Site.Id, f.Location.Id, f.Manufacturer.Id, f.Model.Id, "SN-1", "QR-1", null, new DateOnly(2026, 1, 1), 1200, "SAR", new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 1), new DateOnly(2027, 1, 1), "Supplier", "PO-1", f.Responsible.Id, null, AssetOperationalStatus.Operational);
    private static AssetUpdateInput Update(Asset x) => new(x.Code, x.NameEn, x.NameAr, x.CategoryId, x.ManufacturerId, x.ModelId, x.SerialNumber, x.Barcode, x.Description, x.PurchaseDate, x.PurchaseCost, x.Currency, x.InstallationDate, x.WarrantyStartDate, x.WarrantyExpiryDate, x.SupplierName, x.SupplierReference, x.ResponsibleUserId, x.Notes, x.Version);
    private static ClaimsPrincipal User(IEnumerable<string> permissions, Guid? site = null) => new(new ClaimsIdentity(permissions.SelectMany(p => new[] { new Claim(TenantClaims.Permission, p), new Claim(TenantClaims.Grant, new PermissionGrant(p, site is null ? AccessScopeKind.Tenant : AccessScopeKind.Site, site).ToClaimValue()) }), "test"));
    private static Site Site(Guid tenant, string code) => new() { Id = Guid.NewGuid(), TenantId = tenant, Code = code, NameEn = code, TimeZone = "UTC" };
    private static LocationNode Location(Guid tenant, Guid site, Guid type, string code) => new() { Id = Guid.NewGuid(), TenantId = tenant, SiteId = site, LocationTypeId = type, Code = code, NameEn = code };
    private static PlatformDbContext Database(ICurrentTenant tenant, string? name = null) => new(new DbContextOptionsBuilder<PlatformDbContext>().UseInMemoryDatabase(name ?? Guid.NewGuid().ToString()).Options, tenant, TimeProvider.System);
    private sealed class Current(Guid id) : ICurrentTenant { public Guid? Id { get; private set; } = id; public void Initialize(Guid tenantId) => Id = tenantId; }
    private sealed class NoAudit : IAuditWriter { public void Add(string action, string entityType, Guid entityId, object? changes = null) { } }
    private sealed record FixtureData(PlatformDbContext Db, AssetRegistryService Service, Site Site, Site OtherSite, LocationNode Location, LocationNode OtherLocation, AssetCategory Category, Manufacturer Manufacturer, AssetModel Model, PlatformUser Responsible, ClaimsPrincipal TenantUser);
}
