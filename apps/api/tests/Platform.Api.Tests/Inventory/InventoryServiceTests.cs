using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Assets.Domain;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Inventory.Application;
using SaaSPlatform.Api.Modules.Inventory.Domain;
using SaaSPlatform.Api.Modules.Locations.Domain;
using SaaSPlatform.Api.Modules.Maintenance.Application;
using SaaSPlatform.Api.Modules.Maintenance.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;
using SaaSPlatform.Api.Modules.Tenancy.Domain;

namespace SaaSPlatform.Api.Tests.Inventory;

public sealed class InventoryServiceTests
{
    [Fact] public async Task WarehouseCreationAndDuplicateCodeValidation() { await using var f = await Fixture.Create(); var created = await f.CreateWarehouse(); Assert.Equal("MAIN", created.Code); await Assert.ThrowsAsync<ConflictException>(() => f.Service.CreateWarehouse(f.User(), f.WarehouseInput(), default)); }
    [Fact] public async Task PartCreationAndDuplicateSkuValidation() { await using var f = await Fixture.Create(); var part = await f.CreatePart(); Assert.Equal("FLT-001", part.Sku); await Assert.ThrowsAsync<ConflictException>(() => f.Service.CreatePart(f.User(), f.PartInput(), default)); }
    [Fact] public async Task TenantFilterHidesForeignInventory() { var store = Guid.NewGuid().ToString(); var tenant1 = Guid.NewGuid(); var tenant2 = Guid.NewGuid(); await using (var foreign = Fixture.CreateDatabase(new Current(tenant2), store)) { foreign.SpareParts.Add(new SparePart { Id = Guid.NewGuid(), TenantId = tenant2, Sku = "FOREIGN", NameEn = "Foreign" }); await foreign.SaveChangesAsync(); } await using var current = Fixture.CreateDatabase(new Current(tenant1), store); Assert.Empty(await current.SpareParts.ToListAsync()); }
    [Fact] public async Task SiteScopePreventsForeignWarehouseAccess() { await using var f = await Fixture.Create(); var other = await f.AddWarehouse("OTHER", f.OtherSite); var user = f.User((TenantPermissions.InventoryReceive, f.Site.Id)); await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Service.Receive(user, f.Receipt(other.Id, other.Locations.Single().Id), default)); }

    [Fact]
    public async Task ReceivingUsesWarehouseMovingWeightedAverage()
    {
        await using var f = await Fixture.Create(); var warehouse = await f.CreateWarehouse(); var location = await f.CreateLocation(warehouse.Id); var part = await f.CreatePart();
        await f.Service.Receive(f.User(), f.Receipt(warehouse.Id, location.Id, part.Id, 10, 20), default); await f.Service.Receive(f.User(), f.Receipt(warehouse.Id, location.Id, part.Id, 10, 30), default);
        var balance = await f.Database.InventoryBalances.SingleAsync(); Assert.Equal(20, balance.QuantityOnHand); Assert.Equal(25, balance.AverageUnitCost); Assert.Equal(500, balance.InventoryValue);
    }

    [Fact]
    public async Task IssueUsesCurrentCostAndPreventsOverspend()
    {
        await using var f = await Fixture.Create(); var (warehouse, location, part) = await f.Stock(10, 20); var issue = await f.Service.Issue(f.User(), f.Issue(warehouse.Id, location.Id, part.Id, 8), default); Assert.Equal(160, issue.TotalCost); await Assert.ThrowsAsync<ConflictException>(() => f.Service.Issue(f.User(), f.Issue(warehouse.Id, location.Id, part.Id, 3), default)); Assert.Equal(2, (await f.Database.InventoryBalances.SingleAsync()).QuantityOnHand);
    }

    [Fact]
    public async Task DuplicateRequestsReturnOriginalWithoutDoublePosting()
    {
        await using var f = await Fixture.Create(); var warehouse = await f.CreateWarehouse(); var location = await f.CreateLocation(warehouse.Id); var part = await f.CreatePart(); var request = f.Receipt(warehouse.Id, location.Id, part.Id, 5, 12); var first = await f.Service.Receive(f.User(), request, default); var second = await f.Service.Receive(f.User(), request, default); Assert.Equal(first.Id, second.Id); Assert.Equal(5, (await f.Database.InventoryBalances.SingleAsync()).QuantityOnHand); Assert.Single(await f.Database.InventoryMovements.ToListAsync());
    }

    [Fact]
    public async Task TransferPreservesValueAndPostsBothLedgerSides()
    {
        await using var f = await Fixture.Create(); var (source, sourceLocation, part) = await f.Stock(10, 25); var destination = await f.AddWarehouse("SECOND", f.OtherSite); var destinationLocation = destination.Locations.Single(); await f.Service.Transfer(f.User(), new(Guid.NewGuid(), "TR-1", part.Id, source.Id, sourceLocation.Id, destination.Id, destinationLocation.Id, 4, null), default); var balances = await f.Database.InventoryBalances.ToListAsync(); Assert.Equal(10, balances.Sum(x => x.QuantityOnHand)); Assert.Equal(250, balances.Sum(x => x.InventoryValue)); Assert.Equal(2, await f.Database.InventoryMovements.CountAsync(x => x.Type == InventoryMovementType.TransferOut || x.Type == InventoryMovementType.TransferIn));
    }

    [Fact] public async Task AdjustmentRejectsNegativeResult() { await using var f = await Fixture.Create(); var (warehouse, location, part) = await f.Stock(2, 10); await Assert.ThrowsAsync<ConflictException>(() => f.Service.Adjust(f.User(), new(Guid.NewGuid(), null, warehouse.Id, location.Id, part.Id, -3, StockAdjustmentReason.PhysicalCount, "Count"), default)); }

    [Fact]
    public async Task ReservationReducesAvailableAndReleaseRestoresIt()
    {
        await using var f = await Fixture.Create(); var (warehouse, location, part) = await f.Stock(10, 5); var order = await f.Order(MaintenanceWorkOrderType.Corrective); var reservation = await f.Service.Reserve(f.User(), new(Guid.NewGuid(), order.Id, warehouse.Id, location.Id, part.Id, 4), default); Assert.Equal(6, (await f.Database.InventoryBalances.SingleAsync()).QuantityAvailable); reservation = await f.Service.Release(f.User(), reservation.Id, new("Unused", reservation.Version), default); Assert.Equal(StockReservationState.Released, reservation.State); Assert.Equal(10, (await f.Database.InventoryBalances.SingleAsync()).QuantityAvailable);
    }

    [Theory]
    [InlineData(MaintenanceWorkOrderType.Corrective)]
    [InlineData(MaintenanceWorkOrderType.Preventive)]
    public async Task ReservationsSupportPartialCmAndPmConsumption(MaintenanceWorkOrderType type)
    {
        await using var f = await Fixture.Create(); var (warehouse, location, part) = await f.Stock(10, 7.5m); var order = await f.Order(type); var reservation = await f.Service.Reserve(f.User(), new(Guid.NewGuid(), order.Id, warehouse.Id, location.Id, part.Id, 6), default); var consumed = await f.Service.Consume(f.User(), reservation.Id, new(Guid.NewGuid(), 2, "Used", reservation.Version), default); Assert.Equal(15, consumed.TotalCost); reservation = await f.Database.StockReservations.SingleAsync(); Assert.Equal(StockReservationState.PartiallyConsumed, reservation.State); Assert.Equal(8, (await f.Database.InventoryBalances.SingleAsync()).QuantityOnHand); Assert.Contains(await f.Database.WorkOrderActivities.ToListAsync(), x => x.Action == "Inventory.PartConsumed");
    }

    [Fact]
    public async Task WorkOrderCancellationReleasesUnusedReservation()
    {
        await using var f = await Fixture.Create(); var (warehouse, location, part) = await f.Stock(10, 5); var order = await f.Order(MaintenanceWorkOrderType.Corrective); await f.Service.Reserve(f.User(), new(Guid.NewGuid(), order.Id, warehouse.Id, location.Id, part.Id, 4), default); await f.Corrective.Cancel(f.User(), order.Id, new("Cancelled", order.Version), default); Assert.Equal(StockReservationState.Released, (await f.Database.StockReservations.SingleAsync()).State); Assert.Equal(0, (await f.Database.InventoryBalances.SingleAsync()).QuantityReserved);
    }

    [Fact]
    public async Task MovementLedgerMatchesPhysicalOperations()
    {
        await using var f = await Fixture.Create(); var (warehouse, location, part) = await f.Stock(10, 10); await f.Service.Issue(f.User(), f.Issue(warehouse.Id, location.Id, part.Id, 3), default); await f.Service.Adjust(f.User(), new(Guid.NewGuid(), "A-1", warehouse.Id, location.Id, part.Id, 2, StockAdjustmentReason.PhysicalCount, null), default); var movements = await f.Database.InventoryMovements.OrderBy(x => x.OccurredAtUtc).ToListAsync(); Assert.Equal(3, movements.Count); Assert.Equal(9, movements.Sum(x => x.Quantity)); Assert.Equal(90, movements.Sum(x => x.TotalValue));
    }

    [Fact] public async Task StaleWarehouseVersionIsRejected() { await using var f = await Fixture.Create(); var warehouse = await f.CreateWarehouse(); await Assert.ThrowsAsync<ConflictException>(() => f.Service.UpdateWarehouse(f.User(), warehouse.Id, f.WarehouseInput() with { Version = Guid.NewGuid() }, default)); }
    [Fact] public async Task BalanceHasConcurrencyAndUniqueStockKeyGuards() { await using var f = await Fixture.Create(); var entity = f.Database.Model.FindEntityType(typeof(InventoryBalance)); Assert.NotNull(entity); Assert.True(entity!.FindProperty(nameof(InventoryBalance.Version))!.IsConcurrencyToken); Assert.Contains(entity.GetIndexes(), x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(InventoryBalance.TenantId), nameof(InventoryBalance.WarehouseId), nameof(InventoryBalance.StockLocationId), nameof(InventoryBalance.PartId)])); }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider provider; public PlatformDbContext Database { get; } public InventoryService Service { get; } public CorrectiveMaintenanceService Corrective { get; } public Guid TenantId { get; } public Site Site { get; } public Site OtherSite { get; } public PlatformUser Actor { get; }
        private Fixture(ServiceProvider provider, PlatformDbContext database, InventoryService service, CorrectiveMaintenanceService corrective, Guid tenantId, Site site, Site otherSite, PlatformUser actor) { this.provider = provider; Database = database; Service = service; Corrective = corrective; TenantId = tenantId; Site = site; OtherSite = otherSite; Actor = actor; }
        public static async Task<Fixture> Create()
        {
            var tenantId = Guid.NewGuid(); var current = new Current(tenantId); var services = new ServiceCollection(); services.AddSingleton<ICurrentTenant>(current); services.AddSingleton<TimeProvider>(TimeProvider.System); services.AddHttpContextAccessor(); services.AddDbContext<PlatformDbContext>(x => x.UseInMemoryDatabase(Guid.NewGuid().ToString())); var provider = services.BuildServiceProvider(); var db = provider.GetRequiredService<PlatformDbContext>(); var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = $"tenant-{tenantId:N}" }; var site = SiteFor(tenantId, "MAIN"); var other = SiteFor(tenantId, "OTHER"); var actor = new PlatformUser { Id = Guid.NewGuid(), UserName = "inventory@test.local", Email = "inventory@test.local", DisplayName = "Inventory Manager" }; db.AddRange(tenant, site, other, actor, new TenantMembership { Id = Guid.NewGuid(), TenantId = tenantId, Tenant = tenant, UserId = actor.Id, User = actor }); await db.SaveChangesAsync(); var scopes = new SiteScopeAuthorizer(); var audit = new NoAudit(); return new(provider, db, new(db, current, scopes, audit, TimeProvider.System), new(db, current, scopes, audit, TimeProvider.System), tenantId, site, other, actor);
        }
        public ClaimsPrincipal User(params (string Permission, Guid Site)[] grants) { IEnumerable<(string Permission, Guid? Site)> values = grants.Length == 0 ? TenantPermissions.All.Select(x => (x, (Guid?)null)) : grants.Select(x => (x.Permission, (Guid?)x.Site)); var claims = new List<Claim> { new("sub", Actor.Id.ToString()) }; foreach (var value in values) { claims.Add(new(TenantClaims.Permission, value.Permission)); claims.Add(new(TenantClaims.Grant, new PermissionGrant(value.Permission, value.Site is null ? AccessScopeKind.Tenant : AccessScopeKind.Site, value.Site).ToClaimValue())); } return new(new ClaimsIdentity(claims, "test")); }
        public WarehouseInput WarehouseInput(string code = "MAIN") => new(code, code, null, Site.Id, null, Guid.Empty);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822")]
        public SparePartInput PartInput(string sku = "FLT-001") => new(sku, "Air filter", "مرشح هواء", null, "HVAC", "Nexa", "MPN-1", InventoryUnitOfMeasure.Piece, null, 2, 3, "Supplier", Guid.Empty);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822")]
        public StockReceiptInput Receipt(Guid warehouse, Guid location, Guid? part = null, decimal quantity = 1, decimal cost = 10) => new(Guid.NewGuid(), warehouse, location, part ?? Guid.NewGuid(), null, null, null, DateOnly.FromDateTime(DateTime.UtcNow), quantity, cost, null);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822")]
        public StockIssueInput Issue(Guid warehouse, Guid location, Guid part, decimal quantity) => new(Guid.NewGuid(), warehouse, location, part, null, null, quantity, "General issue");
        public Task<Warehouse> CreateWarehouse() => Service.CreateWarehouse(User(), WarehouseInput(), default);
        public Task<WarehouseStockLocation> CreateLocation(Guid warehouse) => Service.CreateLocation(User(), new(warehouse, null, "BIN-A", "Bin A", null, Guid.Empty), default);
        public Task<SparePart> CreatePart() => Service.CreatePart(User(), PartInput(), default);
        public async Task<Warehouse> AddWarehouse(string code, Site site) { var warehouse = await Service.CreateWarehouse(User(), new(code, code, null, site.Id, null, Guid.Empty), default); var location = await Service.CreateLocation(User(), new(warehouse.Id, null, $"{code}-BIN", $"{code} Bin", null, Guid.Empty), default); warehouse.Locations.Add(location); return warehouse; }
        public async Task<(Warehouse Warehouse, WarehouseStockLocation Location, SparePart Part)> Stock(decimal quantity, decimal cost) { var warehouse = await CreateWarehouse(); var location = await CreateLocation(warehouse.Id); var part = await CreatePart(); await Service.Receive(User(), Receipt(warehouse.Id, location.Id, part.Id, quantity, cost), default); return (warehouse, location, part); }
        public async Task<CorrectiveWorkOrder> Order(MaintenanceWorkOrderType type) { var category = new AssetCategory { Id = Guid.NewGuid(), TenantId = TenantId, Code = Guid.NewGuid().ToString("N")[..8], NameEn = "Equipment" }; var asset = new Asset { Id = Guid.NewGuid(), TenantId = TenantId, Code = Guid.NewGuid().ToString("N")[..8], NameEn = "Pump", CategoryId = category.Id, SiteId = Site.Id }; var order = new CorrectiveWorkOrder { Id = Guid.NewGuid(), TenantId = TenantId, Number = $"WO-{Guid.NewGuid():N}"[..20], Type = type, AssetId = asset.Id, SiteId = Site.Id, Priority = MaintenancePriority.Medium, Description = "Maintenance", Status = CorrectiveWorkOrderStatus.InProgress, AssignedTechnicianId = Actor.Id }; Database.AddRange(category, asset, order); await Database.SaveChangesAsync(); return order; }
        public static PlatformDbContext CreateDatabase(ICurrentTenant tenant, string name) => new(new DbContextOptionsBuilder<PlatformDbContext>().UseInMemoryDatabase(name).Options, tenant, TimeProvider.System);
        private static Site SiteFor(Guid tenant, string code) => new() { Id = Guid.NewGuid(), TenantId = tenant, Code = code, NameEn = code, TimeZone = "UTC" };
        public async ValueTask DisposeAsync() { await Database.DisposeAsync(); await provider.DisposeAsync(); }
    }
    private sealed class Current(Guid id) : ICurrentTenant { public Guid? Id { get; private set; } = id; public void Initialize(Guid tenantId) => Id = tenantId; }
    private sealed class NoAudit : IAuditWriter { public void Add(string action, string entityType, Guid entityId, object? changes = null) { } }
}
