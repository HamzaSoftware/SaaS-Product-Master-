using System.Data;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Inventory.Domain;
using SaaSPlatform.Api.Modules.Maintenance.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;
using SaaSPlatform.Api.Modules.Tenancy.Domain;

namespace SaaSPlatform.Api.Modules.Inventory.Application;

public sealed record InventoryPage<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
public sealed record InventoryListQuery(string? Search, Guid? SiteId, Guid? WarehouseId, Guid? PartId, int Page = 1, int PageSize = 50);
public sealed record WarehouseInput(string Code, string NameEn, string? NameAr, Guid SiteId, string? Description, Guid Version);
public sealed record WarehouseStatusInput(bool IsActive, Guid Version);
public sealed record StockLocationInput(Guid WarehouseId, Guid? ParentLocationId, string Code, string NameEn, string? NameAr, Guid Version);
public sealed record StockLocationStatusInput(bool IsActive, Guid Version);
public sealed record SparePartInput(string Sku, string NameEn, string? NameAr, string? Description, string? Category, string? Manufacturer, string? ManufacturerPartNumber, InventoryUnitOfMeasure UnitOfMeasure, string? Barcode, decimal MinimumStock, decimal ReorderPoint, string? PreferredSupplier, Guid Version);
public sealed record SparePartStatusInput(bool IsActive, Guid Version);
public sealed record StockReceiptInput(Guid RequestId, Guid WarehouseId, Guid StockLocationId, Guid PartId, string? ReferenceNumber, string? SupplierName, string? SupplierReference, DateOnly ReceiptDate, decimal Quantity, decimal UnitCost, string? Notes);
public sealed record StockIssueInput(Guid RequestId, Guid WarehouseId, Guid StockLocationId, Guid PartId, Guid? WorkOrderId, string? ReferenceNumber, decimal Quantity, string? Reason);
public sealed record StockTransferInput(Guid RequestId, string? ReferenceNumber, Guid PartId, Guid SourceWarehouseId, Guid SourceLocationId, Guid DestinationWarehouseId, Guid DestinationLocationId, decimal Quantity, string? Notes);
public sealed record StockAdjustmentInput(Guid RequestId, string? ReferenceNumber, Guid WarehouseId, Guid StockLocationId, Guid PartId, decimal AdjustmentQuantity, StockAdjustmentReason Reason, string? Notes);
public sealed record StockReservationInput(Guid RequestId, Guid WorkOrderId, Guid WarehouseId, Guid StockLocationId, Guid PartId, decimal Quantity);
public sealed record ReservationConsumeInput(Guid RequestId, decimal Quantity, string? Notes, Guid Version);
public sealed record ReservationReleaseInput(string? Notes, Guid Version);
public sealed record InventoryDashboard(int TotalParts, decimal TotalStockValue, int LowStockItems, int OutOfStockItems, int ActiveReservations, decimal ReservedQuantity, IReadOnlyList<InventoryMovement> RecentMovements);
public sealed record WorkOrderMaterialSummary(IReadOnlyList<StockReservation> Reservations, IReadOnlyList<WorkOrderPartConsumption> Consumptions, decimal TotalMaterialCost);

public interface IInventoryService
{
    Task<InventoryDashboard> Dashboard(ClaimsPrincipal user, CancellationToken ct);
    Task<InventoryPage<Warehouse>> Warehouses(ClaimsPrincipal user, InventoryListQuery input, CancellationToken ct);
    Task<Warehouse> Warehouse(ClaimsPrincipal user, Guid id, CancellationToken ct);
    Task<Warehouse> CreateWarehouse(ClaimsPrincipal user, WarehouseInput input, CancellationToken ct);
    Task<Warehouse> UpdateWarehouse(ClaimsPrincipal user, Guid id, WarehouseInput input, CancellationToken ct);
    Task<Warehouse> SetWarehouseStatus(ClaimsPrincipal user, Guid id, WarehouseStatusInput input, CancellationToken ct);
    Task<IReadOnlyList<WarehouseStockLocation>> Locations(ClaimsPrincipal user, Guid? warehouseId, CancellationToken ct);
    Task<WarehouseStockLocation> CreateLocation(ClaimsPrincipal user, StockLocationInput input, CancellationToken ct);
    Task<WarehouseStockLocation> UpdateLocation(ClaimsPrincipal user, Guid id, StockLocationInput input, CancellationToken ct);
    Task<WarehouseStockLocation> SetLocationStatus(ClaimsPrincipal user, Guid id, StockLocationStatusInput input, CancellationToken ct);
    Task<InventoryPage<SparePart>> Parts(ClaimsPrincipal user, InventoryListQuery input, CancellationToken ct);
    Task<SparePart> Part(ClaimsPrincipal user, Guid id, CancellationToken ct);
    Task<SparePart> CreatePart(ClaimsPrincipal user, SparePartInput input, CancellationToken ct);
    Task<SparePart> UpdatePart(ClaimsPrincipal user, Guid id, SparePartInput input, CancellationToken ct);
    Task<SparePart> SetPartStatus(ClaimsPrincipal user, Guid id, SparePartStatusInput input, CancellationToken ct);
    Task<InventoryPage<InventoryBalance>> Balances(ClaimsPrincipal user, InventoryListQuery input, CancellationToken ct);
    Task<InventoryPage<InventoryMovement>> Movements(ClaimsPrincipal user, InventoryListQuery input, CancellationToken ct);
    Task<StockReceipt> Receive(ClaimsPrincipal user, StockReceiptInput input, CancellationToken ct);
    Task<StockReceipt> ReceiveForProcurement(ClaimsPrincipal user, StockReceiptInput input, CancellationToken ct);
    Task<StockIssue> Issue(ClaimsPrincipal user, StockIssueInput input, CancellationToken ct);
    Task<StockTransfer> Transfer(ClaimsPrincipal user, StockTransferInput input, CancellationToken ct);
    Task<StockAdjustment> Adjust(ClaimsPrincipal user, StockAdjustmentInput input, CancellationToken ct);
    Task<StockReservation> Reserve(ClaimsPrincipal user, StockReservationInput input, CancellationToken ct);
    Task<WorkOrderPartConsumption> Consume(ClaimsPrincipal user, Guid reservationId, ReservationConsumeInput input, CancellationToken ct);
    Task<StockReservation> Release(ClaimsPrincipal user, Guid reservationId, ReservationReleaseInput input, CancellationToken ct);
    Task<WorkOrderMaterialSummary> WorkOrderMaterials(ClaimsPrincipal user, Guid workOrderId, CancellationToken ct);
    Task<IReadOnlyList<WorkOrderPartConsumption>> AssetConsumptions(ClaimsPrincipal user, Guid assetId, CancellationToken ct);
}

public sealed class InventoryService(PlatformDbContext database, ICurrentTenant currentTenant, ISiteScopeAuthorizer scopes, IAuditWriter audit, TimeProvider clock) : IInventoryService
{
    private Guid TenantId => currentTenant.Id ?? throw new InvalidOperationException("Tenant context is required.");
    private DateTimeOffset Now => clock.GetUtcNow();

    public async Task<InventoryDashboard> Dashboard(ClaimsPrincipal user, CancellationToken ct)
    {
        RequireTenantOrAnySite(user, TenantPermissions.InventoryView);
        var warehouses = AccessibleWarehouses(user, TenantPermissions.InventoryView).Select(x => x.Id);
        var balances = database.InventoryBalances.AsNoTracking().Where(x => warehouses.Contains(x.WarehouseId));
        var grouped = balances.GroupBy(x => x.PartId).Select(g => new { OnHand = g.Sum(x => x.QuantityOnHand), Reserved = g.Sum(x => x.QuantityReserved), Value = g.Sum(x => x.QuantityOnHand * x.AverageUnitCost) });
        var totals = await grouped.ToListAsync(ct);
        var thresholds = await database.SpareParts.AsNoTracking().Select(x => new { x.Id, x.ReorderPoint }).ToListAsync(ct);
        var quantities = await balances.GroupBy(x => x.PartId).Select(g => new { Id = g.Key, OnHand = g.Sum(x => x.QuantityOnHand), Available = g.Sum(x => x.QuantityOnHand - x.QuantityReserved) }).ToDictionaryAsync(x => x.Id, ct);
        var recent = await database.InventoryMovements.AsNoTracking().IncludeInventory().Where(x => warehouses.Contains(x.WarehouseId)).OrderByDescending(x => x.OccurredAtUtc).Take(10).ToListAsync(ct);
        return new(await database.SpareParts.CountAsync(ct), Money(totals.Sum(x => x.Value)), thresholds.Count(x => !quantities.TryGetValue(x.Id, out var q) || q.Available <= x.ReorderPoint), thresholds.Count(x => !quantities.TryGetValue(x.Id, out var q) || q.OnHand <= 0), await database.StockReservations.CountAsync(x => x.State == StockReservationState.Active || x.State == StockReservationState.PartiallyConsumed, ct), totals.Sum(x => x.Reserved), recent);
    }

    public async Task<InventoryPage<Warehouse>> Warehouses(ClaimsPrincipal user, InventoryListQuery input, CancellationToken ct)
    {
        var query = AccessibleWarehouses(user, TenantPermissions.InventoryView).AsNoTracking().Include(x => x.Site).AsQueryable();
        if (!string.IsNullOrWhiteSpace(input.Search)) { var term = $"%{input.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Code, term) || EF.Functions.ILike(x.NameEn, term) || (x.NameAr != null && EF.Functions.ILike(x.NameAr, term))); }
        if (input.SiteId is { } site) query = query.Where(x => x.SiteId == site);
        var (page, size) = Page(input.Page, input.PageSize); var total = await query.CountAsync(ct);
        return new(await query.OrderBy(x => x.Code).Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, total);
    }

    public async Task<Warehouse> Warehouse(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var entity = await database.Warehouses.AsNoTracking().Include(x => x.Site).Include(x => x.Locations).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Warehouse was not found.");
        RequireSite(user, TenantPermissions.InventoryView, entity.SiteId); return entity;
    }

    public async Task<Warehouse> CreateWarehouse(ClaimsPrincipal user, WarehouseInput input, CancellationToken ct)
    {
        var site = await database.Sites.SingleOrDefaultAsync(x => x.Id == input.SiteId && x.IsActive, ct) ?? throw new ValidationException("An active site is required.");
        RequireSite(user, TenantPermissions.InventoryWarehousesManage, site.Id); var code = Code(input.Code);
        if (await database.Warehouses.AnyAsync(x => x.Code == code, ct)) throw new ConflictException("Warehouse code already exists.");
        var entity = new Warehouse { Id = Guid.NewGuid(), TenantId = TenantId, Code = code, NameEn = Required(input.NameEn, "English name"), NameAr = Clean(input.NameAr), SiteId = site.Id, Description = Clean(input.Description) };
        database.Warehouses.Add(entity); audit.Add("Inventory.WarehouseCreated", nameof(Warehouse), entity.Id, new { entity.Code, entity.SiteId }); await Save(ct); return await Warehouse(user, entity.Id, ct);
    }

    public async Task<Warehouse> UpdateWarehouse(ClaimsPrincipal user, Guid id, WarehouseInput input, CancellationToken ct)
    {
        var entity = await database.Warehouses.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Warehouse was not found."); Version(entity.Version, input.Version); RequireSite(user, TenantPermissions.InventoryWarehousesManage, entity.SiteId); RequireSite(user, TenantPermissions.InventoryWarehousesManage, input.SiteId);
        var site = await database.Sites.SingleOrDefaultAsync(x => x.Id == input.SiteId && x.IsActive, ct) ?? throw new ValidationException("An active site is required."); var code = Code(input.Code);
        if (await database.Warehouses.AnyAsync(x => x.Id != id && x.Code == code, ct)) throw new ConflictException("Warehouse code already exists.");
        if (entity.SiteId != site.Id && await database.InventoryBalances.AnyAsync(x => x.WarehouseId == id && (x.QuantityOnHand != 0 || x.QuantityReserved != 0), ct)) throw new ConflictException("A warehouse with stock cannot move to another site.");
        entity.Code = code; entity.NameEn = Required(input.NameEn, "English name"); entity.NameAr = Clean(input.NameAr); entity.SiteId = site.Id; entity.Description = Clean(input.Description); entity.Version = Guid.NewGuid(); audit.Add("Inventory.WarehouseUpdated", nameof(Warehouse), id); await Save(ct); return await Warehouse(user, id, ct);
    }

    public async Task<Warehouse> SetWarehouseStatus(ClaimsPrincipal user, Guid id, WarehouseStatusInput input, CancellationToken ct)
    {
        var entity = await database.Warehouses.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Warehouse was not found."); RequireSite(user, TenantPermissions.InventoryWarehousesManage, entity.SiteId); Version(entity.Version, input.Version);
        if (!input.IsActive && (await database.StockReservations.AnyAsync(x => x.WarehouseId == id && (x.State == StockReservationState.Active || x.State == StockReservationState.PartiallyConsumed), ct) || await database.StockTransfers.AnyAsync(x => (x.SourceWarehouseId == id || x.DestinationWarehouseId == id) && (x.State == StockTransferState.Draft || x.State == StockTransferState.Submitted), ct))) throw new ConflictException("Release reservations and complete active transfers before deactivating this warehouse.");
        entity.IsActive = input.IsActive; entity.Version = Guid.NewGuid(); audit.Add("Inventory.WarehouseStatusChanged", nameof(Warehouse), id, new { input.IsActive }); await Save(ct); return await Warehouse(user, id, ct);
    }

    public async Task<IReadOnlyList<WarehouseStockLocation>> Locations(ClaimsPrincipal user, Guid? warehouseId, CancellationToken ct)
    {
        var warehouses = AccessibleWarehouses(user, TenantPermissions.InventoryView).Select(x => x.Id); var query = database.WarehouseStockLocations.AsNoTracking().Where(x => warehouses.Contains(x.WarehouseId)); if (warehouseId is { } id) query = query.Where(x => x.WarehouseId == id); return await query.OrderBy(x => x.Code).ToListAsync(ct);
    }

    public async Task<WarehouseStockLocation> CreateLocation(ClaimsPrincipal user, StockLocationInput input, CancellationToken ct)
    {
        var warehouse = await ActiveWarehouse(input.WarehouseId, ct); RequireSite(user, TenantPermissions.InventoryWarehousesManage, warehouse.SiteId); await ValidateParent(input.WarehouseId, input.ParentLocationId, null, ct); var code = Code(input.Code);
        if (await database.WarehouseStockLocations.AnyAsync(x => x.WarehouseId == input.WarehouseId && x.Code == code, ct)) throw new ConflictException("Stock-location code already exists in this warehouse.");
        var entity = new WarehouseStockLocation { Id = Guid.NewGuid(), TenantId = TenantId, WarehouseId = input.WarehouseId, ParentLocationId = input.ParentLocationId, Code = code, NameEn = Required(input.NameEn, "English name"), NameAr = Clean(input.NameAr) };
        database.WarehouseStockLocations.Add(entity); audit.Add("Inventory.StockLocationCreated", nameof(WarehouseStockLocation), entity.Id); await Save(ct); return entity;
    }

    public async Task<WarehouseStockLocation> UpdateLocation(ClaimsPrincipal user, Guid id, StockLocationInput input, CancellationToken ct)
    {
        var entity = await database.WarehouseStockLocations.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Stock location was not found."); Version(entity.Version, input.Version); var warehouse = await ActiveWarehouse(entity.WarehouseId, ct); RequireSite(user, TenantPermissions.InventoryWarehousesManage, warehouse.SiteId);
        if (input.WarehouseId != entity.WarehouseId) throw new ValidationException("Stock locations cannot move between warehouses."); await ValidateParent(entity.WarehouseId, input.ParentLocationId, id, ct); var code = Code(input.Code); if (await database.WarehouseStockLocations.AnyAsync(x => x.Id != id && x.WarehouseId == entity.WarehouseId && x.Code == code, ct)) throw new ConflictException("Stock-location code already exists in this warehouse.");
        entity.ParentLocationId = input.ParentLocationId; entity.Code = code; entity.NameEn = Required(input.NameEn, "English name"); entity.NameAr = Clean(input.NameAr); entity.Version = Guid.NewGuid(); await Save(ct); return entity;
    }

    public async Task<WarehouseStockLocation> SetLocationStatus(ClaimsPrincipal user, Guid id, StockLocationStatusInput input, CancellationToken ct)
    {
        var entity = await database.WarehouseStockLocations.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Stock location was not found."); Version(entity.Version, input.Version); var warehouse = await database.Warehouses.SingleAsync(x => x.Id == entity.WarehouseId, ct); RequireSite(user, TenantPermissions.InventoryWarehousesManage, warehouse.SiteId);
        if (!input.IsActive && (await database.InventoryBalances.AnyAsync(x => x.StockLocationId == id && (x.QuantityOnHand != 0 || x.QuantityReserved != 0), ct) || await database.WarehouseStockLocations.AnyAsync(x => x.ParentLocationId == id && x.IsActive, ct))) throw new ConflictException("A location with stock, reservations, or active children cannot be deactivated.");
        entity.IsActive = input.IsActive; entity.Version = Guid.NewGuid(); await Save(ct); return entity;
    }

    public async Task<InventoryPage<SparePart>> Parts(ClaimsPrincipal user, InventoryListQuery input, CancellationToken ct)
    {
        RequireTenantOrAnySite(user, TenantPermissions.InventoryView); var query = database.SpareParts.AsNoTracking(); if (!string.IsNullOrWhiteSpace(input.Search)) { var term = $"%{input.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Sku, term) || EF.Functions.ILike(x.NameEn, term) || (x.NameAr != null && EF.Functions.ILike(x.NameAr, term)) || (x.Barcode != null && EF.Functions.ILike(x.Barcode, term))); }
        var (page, size) = Page(input.Page, input.PageSize); var total = await query.CountAsync(ct); return new(await query.OrderBy(x => x.Sku).Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, total);
    }

    public async Task<SparePart> Part(ClaimsPrincipal user, Guid id, CancellationToken ct) { RequireTenantOrAnySite(user, TenantPermissions.InventoryView); return await database.SpareParts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Spare part was not found."); }

    public async Task<SparePart> CreatePart(ClaimsPrincipal user, SparePartInput input, CancellationToken ct)
    {
        RequireTenant(user, TenantPermissions.InventoryPartsManage); ValidatePart(input); var sku = Code(input.Sku); if (await database.SpareParts.AnyAsync(x => x.Sku == sku, ct)) throw new ConflictException("SKU already exists."); var entity = new SparePart { Id = Guid.NewGuid(), TenantId = TenantId, Sku = sku, NameEn = Required(input.NameEn, "English name") }; Apply(entity, input); database.SpareParts.Add(entity); audit.Add("Inventory.PartCreated", nameof(SparePart), entity.Id, new { entity.Sku }); await Save(ct); return entity;
    }

    public async Task<SparePart> UpdatePart(ClaimsPrincipal user, Guid id, SparePartInput input, CancellationToken ct)
    {
        RequireTenant(user, TenantPermissions.InventoryPartsManage); ValidatePart(input); var entity = await database.SpareParts.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Spare part was not found."); Version(entity.Version, input.Version); var sku = Code(input.Sku); if (await database.SpareParts.AnyAsync(x => x.Id != id && x.Sku == sku, ct)) throw new ConflictException("SKU already exists."); entity.Sku = sku; Apply(entity, input); entity.Version = Guid.NewGuid(); audit.Add("Inventory.PartUpdated", nameof(SparePart), id); await Save(ct); return entity;
    }

    public async Task<SparePart> SetPartStatus(ClaimsPrincipal user, Guid id, SparePartStatusInput input, CancellationToken ct)
    {
        RequireTenant(user, TenantPermissions.InventoryPartsManage); var entity = await database.SpareParts.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Spare part was not found."); Version(entity.Version, input.Version); if (!input.IsActive && await database.StockReservations.AnyAsync(x => x.PartId == id && (x.State == StockReservationState.Active || x.State == StockReservationState.PartiallyConsumed), ct)) throw new ConflictException("Release active reservations before deactivating this part."); entity.IsActive = input.IsActive; entity.Version = Guid.NewGuid(); await Save(ct); return entity;
    }

    public async Task<InventoryPage<InventoryBalance>> Balances(ClaimsPrincipal user, InventoryListQuery input, CancellationToken ct)
    {
        var warehouses = AccessibleWarehouses(user, TenantPermissions.InventoryView).Select(x => x.Id); var query = database.InventoryBalances.AsNoTracking().IncludeInventoryBalance().Where(x => warehouses.Contains(x.WarehouseId)); if (input.WarehouseId is { } warehouse) query = query.Where(x => x.WarehouseId == warehouse); if (input.PartId is { } part) query = query.Where(x => x.PartId == part); if (!string.IsNullOrWhiteSpace(input.Search)) { var term = $"%{input.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Part!.Sku, term) || EF.Functions.ILike(x.Part.NameEn, term)); } var (page, size) = Page(input.Page, input.PageSize); var total = await query.CountAsync(ct); return new(await query.OrderBy(x => x.Part!.Sku).ThenBy(x => x.Warehouse!.Code).Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, total);
    }

    public async Task<InventoryPage<InventoryMovement>> Movements(ClaimsPrincipal user, InventoryListQuery input, CancellationToken ct)
    {
        var warehouses = AccessibleWarehouses(user, TenantPermissions.InventoryAudit).Select(x => x.Id); var query = database.InventoryMovements.AsNoTracking().IncludeInventory().Where(x => warehouses.Contains(x.WarehouseId)); if (input.WarehouseId is { } warehouse) query = query.Where(x => x.WarehouseId == warehouse); if (input.PartId is { } part) query = query.Where(x => x.PartId == part); var (page, size) = Page(input.Page, input.PageSize); var total = await query.CountAsync(ct); return new(await query.OrderByDescending(x => x.OccurredAtUtc).Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, total);
    }

    public Task<StockReceipt> Receive(ClaimsPrincipal user, StockReceiptInput input, CancellationToken ct) => ReceiveCore(user, input, TenantPermissions.InventoryReceive, ct);
    public Task<StockReceipt> ReceiveForProcurement(ClaimsPrincipal user, StockReceiptInput input, CancellationToken ct) => ReceiveCore(user, input, TenantPermissions.ProcurementReceivingCreate, ct);

    private async Task<StockReceipt> ReceiveCore(ClaimsPrincipal user, StockReceiptInput input, string permission, CancellationToken ct)
    {
        ValidateRequest(input.RequestId); Positive(input.Quantity, "Quantity"); if (input.UnitCost < 0) throw new ValidationException("Unit cost cannot be negative."); var warehouse = await ActiveWarehouse(input.WarehouseId, ct); RequireSite(user, permission, warehouse.SiteId); var location = await ActiveLocation(input.StockLocationId, input.WarehouseId, ct); var part = await ActivePart(input.PartId, ct);
        var duplicate = await database.StockReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == input.RequestId, ct); if (duplicate is not null) return duplicate;
        await using var tx = await Transaction(ct); var balance = await Balance(input.WarehouseId, location.Id, part.Id, ct); var old = await WarehouseTotals(part.Id, warehouse.Id, ct); var total = Money(input.Quantity * input.UnitCost); var average = Unit((old.Value + total) / (old.Quantity + input.Quantity));
        balance.QuantityOnHand += input.Quantity; balance.Version = Guid.NewGuid(); await ApplyWarehouseAverage(part.Id, warehouse.Id, average, balance, ct);
        var entity = new StockReceipt { Id = Guid.NewGuid(), TenantId = TenantId, RequestId = input.RequestId, WarehouseId = warehouse.Id, StockLocationId = location.Id, PartId = part.Id, ReferenceNumber = Reference(input.ReferenceNumber, "RCV"), SupplierName = Clean(input.SupplierName), SupplierReference = Clean(input.SupplierReference), ReceiptDate = input.ReceiptDate, Quantity = input.Quantity, UnitCost = Unit(input.UnitCost), TotalCost = total, ReceivedById = Actor(user), Notes = Clean(input.Notes) };
        database.StockReceipts.Add(entity); Movement(warehouse.Id, location.Id, part.Id, InventoryMovementType.Receipt, input.Quantity, entity.UnitCost, entity.TotalCost, nameof(StockReceipt), entity.Id, entity.ReferenceNumber, user, entity.Notes); audit.Add("Inventory.StockReceived", nameof(StockReceipt), entity.Id, new { part.Id, input.Quantity }); await Save(ct); if (tx is not null) await tx.CommitAsync(ct); return entity;
    }

    public async Task<StockIssue> Issue(ClaimsPrincipal user, StockIssueInput input, CancellationToken ct)
    {
        ValidateRequest(input.RequestId); Positive(input.Quantity, "Quantity"); var warehouse = await ActiveWarehouse(input.WarehouseId, ct); RequireSite(user, TenantPermissions.InventoryIssue, warehouse.SiteId); var location = await ActiveLocation(input.StockLocationId, input.WarehouseId, ct); var part = await ActivePart(input.PartId, ct); CorrectiveWorkOrder? order = null;
        if (input.WorkOrderId is { } workOrderId) { order = await EligibleWorkOrder(workOrderId, warehouse.SiteId, user, TenantPermissions.InventoryIssue, false, ct); }
        var duplicate = await database.StockIssues.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == input.RequestId, ct); if (duplicate is not null) return duplicate; await using var tx = await Transaction(ct); var balance = await Balance(warehouse.Id, location.Id, part.Id, ct); if (balance.QuantityAvailable < input.Quantity) throw new ConflictException("Insufficient available stock."); var cost = await WarehouseCost(part.Id, warehouse.Id, ct); balance.QuantityOnHand -= input.Quantity; balance.Version = Guid.NewGuid();
        var entity = new StockIssue { Id = Guid.NewGuid(), TenantId = TenantId, RequestId = input.RequestId, WarehouseId = warehouse.Id, StockLocationId = location.Id, PartId = part.Id, WorkOrderId = order?.Id, ReferenceNumber = Reference(input.ReferenceNumber, "ISS"), Quantity = input.Quantity, UnitCost = cost, TotalCost = Money(input.Quantity * cost), IssuedById = Actor(user), Reason = Clean(input.Reason) };
        database.StockIssues.Add(entity); Movement(warehouse.Id, location.Id, part.Id, InventoryMovementType.Issue, -input.Quantity, cost, -entity.TotalCost, nameof(StockIssue), entity.Id, entity.ReferenceNumber, user, entity.Reason); if (order is not null) Consumption(input.RequestId, order, null, part.Id, warehouse.Id, location.Id, input.Quantity, cost, user, input.Reason); audit.Add("Inventory.StockIssued", nameof(StockIssue), entity.Id, new { PartId = part.Id, WorkOrderId = order?.Id, input.Quantity }); await Save(ct); if (tx is not null) await tx.CommitAsync(ct); return entity;
    }

    public async Task<StockTransfer> Transfer(ClaimsPrincipal user, StockTransferInput input, CancellationToken ct)
    {
        ValidateRequest(input.RequestId); Positive(input.Quantity, "Quantity"); if (input.SourceWarehouseId == input.DestinationWarehouseId && input.SourceLocationId == input.DestinationLocationId) throw new ValidationException("Source and destination must differ."); var sourceWarehouse = await ActiveWarehouse(input.SourceWarehouseId, ct); var destinationWarehouse = await ActiveWarehouse(input.DestinationWarehouseId, ct); RequireSite(user, TenantPermissions.InventoryTransfer, sourceWarehouse.SiteId); RequireSite(user, TenantPermissions.InventoryTransfer, destinationWarehouse.SiteId); var sourceLocation = await ActiveLocation(input.SourceLocationId, sourceWarehouse.Id, ct); var destinationLocation = await ActiveLocation(input.DestinationLocationId, destinationWarehouse.Id, ct); var part = await ActivePart(input.PartId, ct);
        var duplicate = await database.StockTransfers.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == input.RequestId, ct); if (duplicate is not null) return duplicate; await using var tx = await Transaction(ct); var source = await Balance(sourceWarehouse.Id, sourceLocation.Id, part.Id, ct); if (source.QuantityAvailable < input.Quantity) throw new ConflictException("Insufficient available stock."); var cost = await WarehouseCost(part.Id, sourceWarehouse.Id, ct); var destinationOld = await WarehouseTotals(part.Id, destinationWarehouse.Id, ct); var destination = await Balance(destinationWarehouse.Id, destinationLocation.Id, part.Id, ct); source.QuantityOnHand -= input.Quantity; source.Version = Guid.NewGuid(); destination.QuantityOnHand += input.Quantity; destination.Version = Guid.NewGuid(); if (sourceWarehouse.Id != destinationWarehouse.Id) { var destinationAverage = Unit((destinationOld.Value + input.Quantity * cost) / (destinationOld.Quantity + input.Quantity)); await ApplyWarehouseAverage(part.Id, destinationWarehouse.Id, destinationAverage, destination, ct); }
        var entity = new StockTransfer { Id = Guid.NewGuid(), TenantId = TenantId, RequestId = input.RequestId, ReferenceNumber = Reference(input.ReferenceNumber, "TRF"), PartId = part.Id, SourceWarehouseId = sourceWarehouse.Id, SourceLocationId = sourceLocation.Id, DestinationWarehouseId = destinationWarehouse.Id, DestinationLocationId = destinationLocation.Id, Quantity = input.Quantity, UnitCost = cost, TotalValue = Money(input.Quantity * cost), State = StockTransferState.Completed, PerformedById = Actor(user), Notes = Clean(input.Notes) };
        database.StockTransfers.Add(entity); Movement(sourceWarehouse.Id, sourceLocation.Id, part.Id, InventoryMovementType.TransferOut, -input.Quantity, cost, -entity.TotalValue, nameof(StockTransfer), entity.Id, entity.ReferenceNumber, user, entity.Notes); Movement(destinationWarehouse.Id, destinationLocation.Id, part.Id, InventoryMovementType.TransferIn, input.Quantity, cost, entity.TotalValue, nameof(StockTransfer), entity.Id, entity.ReferenceNumber, user, entity.Notes); audit.Add("Inventory.StockTransferred", nameof(StockTransfer), entity.Id, new { part.Id, input.Quantity }); await Save(ct); if (tx is not null) await tx.CommitAsync(ct); return entity;
    }

    public async Task<StockAdjustment> Adjust(ClaimsPrincipal user, StockAdjustmentInput input, CancellationToken ct)
    {
        ValidateRequest(input.RequestId); if (input.AdjustmentQuantity == 0) throw new ValidationException("Adjustment quantity cannot be zero."); if (!Enum.IsDefined(input.Reason)) throw new ValidationException("Adjustment reason is invalid."); var warehouse = await ActiveWarehouse(input.WarehouseId, ct); RequireSite(user, TenantPermissions.InventoryAdjust, warehouse.SiteId); var location = await ActiveLocation(input.StockLocationId, warehouse.Id, ct); var part = await ActivePart(input.PartId, ct); var duplicate = await database.StockAdjustments.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == input.RequestId, ct); if (duplicate is not null) return duplicate; await using var tx = await Transaction(ct); var balance = await Balance(warehouse.Id, location.Id, part.Id, ct); var next = balance.QuantityOnHand + input.AdjustmentQuantity; if (next < 0 || next < balance.QuantityReserved) throw new ConflictException("Adjustment would make stock or available quantity negative."); var previous = balance.QuantityOnHand; var cost = await WarehouseCost(part.Id, warehouse.Id, ct); balance.QuantityOnHand = next; balance.Version = Guid.NewGuid(); var entity = new StockAdjustment { Id = Guid.NewGuid(), TenantId = TenantId, RequestId = input.RequestId, ReferenceNumber = Reference(input.ReferenceNumber, "ADJ"), WarehouseId = warehouse.Id, StockLocationId = location.Id, PartId = part.Id, PreviousQuantity = previous, AdjustmentQuantity = input.AdjustmentQuantity, NewQuantity = next, Reason = input.Reason, Notes = Clean(input.Notes), PerformedById = Actor(user) }; database.StockAdjustments.Add(entity); var value = Money(Math.Abs(input.AdjustmentQuantity) * cost); Movement(warehouse.Id, location.Id, part.Id, input.AdjustmentQuantity > 0 ? InventoryMovementType.AdjustmentIncrease : InventoryMovementType.AdjustmentDecrease, input.AdjustmentQuantity, cost, input.AdjustmentQuantity > 0 ? value : -value, nameof(StockAdjustment), entity.Id, entity.ReferenceNumber, user, entity.Notes); audit.Add("Inventory.StockAdjusted", nameof(StockAdjustment), entity.Id, new { part.Id, input.AdjustmentQuantity, input.Reason }); await Save(ct); if (tx is not null) await tx.CommitAsync(ct); return entity;
    }

    public async Task<StockReservation> Reserve(ClaimsPrincipal user, StockReservationInput input, CancellationToken ct)
    {
        ValidateRequest(input.RequestId); Positive(input.Quantity, "Quantity"); var warehouse = await ActiveWarehouse(input.WarehouseId, ct); RequireSite(user, TenantPermissions.InventoryReserve, warehouse.SiteId); var location = await ActiveLocation(input.StockLocationId, warehouse.Id, ct); var part = await ActivePart(input.PartId, ct); var order = await EligibleWorkOrder(input.WorkOrderId, warehouse.SiteId, user, TenantPermissions.InventoryReserve, false, ct); var duplicate = await database.StockReservations.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == input.RequestId, ct); if (duplicate is not null) return duplicate; await using var tx = await Transaction(ct); var balance = await Balance(warehouse.Id, location.Id, part.Id, ct); if (balance.QuantityAvailable < input.Quantity) throw new ConflictException("Insufficient available stock for this reservation."); balance.QuantityReserved += input.Quantity; balance.Version = Guid.NewGuid(); var entity = new StockReservation { Id = Guid.NewGuid(), TenantId = TenantId, RequestId = input.RequestId, WorkOrderId = order.Id, PartId = part.Id, WarehouseId = warehouse.Id, StockLocationId = location.Id, ReservedQuantity = input.Quantity, CreatedById = Actor(user) }; database.StockReservations.Add(entity); Movement(warehouse.Id, location.Id, part.Id, InventoryMovementType.ReservationCreated, input.Quantity, 0, 0, nameof(StockReservation), entity.Id, order.Number, user, null); WorkOrderActivity(order, "Inventory.PartReserved", $"{part.Sku}: {input.Quantity}", user); await Save(ct); if (tx is not null) await tx.CommitAsync(ct); return entity;
    }

    public async Task<WorkOrderPartConsumption> Consume(ClaimsPrincipal user, Guid reservationId, ReservationConsumeInput input, CancellationToken ct)
    {
        ValidateRequest(input.RequestId); Positive(input.Quantity, "Quantity"); var duplicate = await database.WorkOrderPartConsumptions.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == input.RequestId, ct); if (duplicate is not null) return duplicate; var reservation = await database.StockReservations.Include(x => x.Part).SingleOrDefaultAsync(x => x.Id == reservationId, ct) ?? throw new ResourceNotFoundException("Reservation was not found."); Version(reservation.Version, input.Version); if (reservation.State is StockReservationState.Consumed or StockReservationState.Released) throw new ConflictException("This reservation is no longer active."); var warehouse = await ActiveWarehouse(reservation.WarehouseId, ct); RequireSite(user, TenantPermissions.InventoryIssue, warehouse.SiteId); var order = await EligibleWorkOrder(reservation.WorkOrderId, warehouse.SiteId, user, TenantPermissions.InventoryIssue, true, ct); var remaining = reservation.ReservedQuantity - reservation.ConsumedQuantity; if (input.Quantity > remaining) throw new ConflictException("Consumption exceeds the remaining reserved quantity."); await using var tx = await Transaction(ct); var balance = await Balance(reservation.WarehouseId, reservation.StockLocationId, reservation.PartId, ct); if (balance.QuantityOnHand < input.Quantity || balance.QuantityReserved < input.Quantity) throw new ConflictException("Reserved stock is no longer available."); var cost = await WarehouseCost(reservation.PartId, reservation.WarehouseId, ct); balance.QuantityOnHand -= input.Quantity; balance.QuantityReserved -= input.Quantity; balance.Version = Guid.NewGuid(); reservation.ConsumedQuantity += input.Quantity; reservation.State = reservation.ConsumedQuantity == reservation.ReservedQuantity ? StockReservationState.Consumed : StockReservationState.PartiallyConsumed; reservation.Version = Guid.NewGuid(); var consumption = Consumption(input.RequestId, order, reservation.Id, reservation.PartId, reservation.WarehouseId, reservation.StockLocationId, input.Quantity, cost, user, input.Notes); Movement(reservation.WarehouseId, reservation.StockLocationId, reservation.PartId, InventoryMovementType.Issue, -input.Quantity, cost, -consumption.TotalCost, nameof(WorkOrderPartConsumption), consumption.Id, order.Number, user, input.Notes); WorkOrderActivity(order, "Inventory.PartConsumed", $"{reservation.Part?.Sku}: {input.Quantity} @ {cost}", user); await Save(ct); if (tx is not null) await tx.CommitAsync(ct); return consumption;
    }

    public async Task<StockReservation> Release(ClaimsPrincipal user, Guid reservationId, ReservationReleaseInput input, CancellationToken ct)
    {
        var reservation = await database.StockReservations.Include(x => x.Part).SingleOrDefaultAsync(x => x.Id == reservationId, ct) ?? throw new ResourceNotFoundException("Reservation was not found."); Version(reservation.Version, input.Version); if (reservation.State is StockReservationState.Consumed or StockReservationState.Released) throw new ConflictException("This reservation is no longer active."); var warehouse = await database.Warehouses.SingleAsync(x => x.Id == reservation.WarehouseId, ct); RequireSite(user, TenantPermissions.InventoryReserve, warehouse.SiteId); await using var tx = await Transaction(ct); var remaining = reservation.ReservedQuantity - reservation.ConsumedQuantity; var balance = await Balance(reservation.WarehouseId, reservation.StockLocationId, reservation.PartId, ct); if (balance.QuantityReserved < remaining) throw new ConflictException("Reservation balance is inconsistent."); balance.QuantityReserved -= remaining; balance.Version = Guid.NewGuid(); reservation.State = StockReservationState.Released; reservation.ReleasedAtUtc = Now; reservation.Version = Guid.NewGuid(); Movement(reservation.WarehouseId, reservation.StockLocationId, reservation.PartId, InventoryMovementType.ReservationReleased, remaining, 0, 0, nameof(StockReservation), reservation.Id, null, user, input.Notes); var order = await database.CorrectiveWorkOrders.SingleAsync(x => x.Id == reservation.WorkOrderId, ct); WorkOrderActivity(order, "Inventory.ReservationReleased", $"{reservation.Part?.Sku}: {remaining}", user); await Save(ct); if (tx is not null) await tx.CommitAsync(ct); return reservation;
    }

    public async Task<WorkOrderMaterialSummary> WorkOrderMaterials(ClaimsPrincipal user, Guid workOrderId, CancellationToken ct)
    {
        var order = await database.CorrectiveWorkOrders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == workOrderId, ct) ?? throw new ResourceNotFoundException("Work order was not found."); RequireSite(user, TenantPermissions.InventoryView, order.SiteId); var reservations = await database.StockReservations.AsNoTracking().Include(x => x.Part).Where(x => x.WorkOrderId == workOrderId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct); var consumptions = await database.WorkOrderPartConsumptions.AsNoTracking().Include(x => x.Part).Where(x => x.WorkOrderId == workOrderId).OrderByDescending(x => x.ConsumedAtUtc).ToListAsync(ct); return new(reservations, consumptions, Money(consumptions.Sum(x => x.TotalCost)));
    }

    public async Task<IReadOnlyList<WorkOrderPartConsumption>> AssetConsumptions(ClaimsPrincipal user, Guid assetId, CancellationToken ct)
    {
        var asset = await database.Assets.AsNoTracking().SingleOrDefaultAsync(x => x.Id == assetId, ct) ?? throw new ResourceNotFoundException("Asset was not found."); RequireSite(user, TenantPermissions.InventoryView, asset.SiteId); return await database.WorkOrderPartConsumptions.AsNoTracking().Include(x => x.Part).Where(x => x.WorkOrder != null && x.WorkOrder.AssetId == assetId).OrderByDescending(x => x.ConsumedAtUtc).ToListAsync(ct);
    }

    private IQueryable<Warehouse> AccessibleWarehouses(ClaimsPrincipal user, string permission) => scopes.HasTenantAccess(user, permission) ? database.Warehouses : database.Warehouses.Where(x => GrantedSites(user, permission).Contains(x.SiteId));
    private async Task<Warehouse> ActiveWarehouse(Guid id, CancellationToken ct) => await database.Warehouses.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct) ?? throw new ValidationException("An active warehouse is required.");
    private async Task<WarehouseStockLocation> ActiveLocation(Guid id, Guid warehouse, CancellationToken ct) => await database.WarehouseStockLocations.SingleOrDefaultAsync(x => x.Id == id && x.WarehouseId == warehouse && x.IsActive, ct) ?? throw new ValidationException("An active stock location in the selected warehouse is required.");
    private async Task<SparePart> ActivePart(Guid id, CancellationToken ct) => await database.SpareParts.SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct) ?? throw new ValidationException("An active spare part is required.");
    private async Task<InventoryBalance> Balance(Guid warehouse, Guid location, Guid part, CancellationToken ct) { var entity = await database.InventoryBalances.SingleOrDefaultAsync(x => x.WarehouseId == warehouse && x.StockLocationId == location && x.PartId == part, ct); if (entity is not null) return entity; entity = new() { Id = Guid.NewGuid(), TenantId = TenantId, WarehouseId = warehouse, StockLocationId = location, PartId = part }; database.InventoryBalances.Add(entity); return entity; }
    private async Task<(decimal Quantity, decimal Value)> WarehouseTotals(Guid part, Guid warehouse, CancellationToken ct) { var rows = await database.InventoryBalances.Where(x => x.PartId == part && x.WarehouseId == warehouse).Select(x => new { x.QuantityOnHand, x.AverageUnitCost }).ToListAsync(ct); return (rows.Sum(x => x.QuantityOnHand), rows.Sum(x => x.QuantityOnHand * x.AverageUnitCost)); }
    private async Task<decimal> WarehouseCost(Guid part, Guid warehouse, CancellationToken ct) { var totals = await WarehouseTotals(part, warehouse, ct); return totals.Quantity <= 0 ? 0 : Unit(totals.Value / totals.Quantity); }
    private async Task ApplyWarehouseAverage(Guid part, Guid warehouse, decimal average, InventoryBalance current, CancellationToken ct) { var balances = await database.InventoryBalances.Where(x => x.PartId == part && x.WarehouseId == warehouse && x.Id != current.Id).ToListAsync(ct); current.AverageUnitCost = average; foreach (var balance in balances) { balance.AverageUnitCost = average; balance.Version = Guid.NewGuid(); } }
    private async Task<CorrectiveWorkOrder> EligibleWorkOrder(Guid id, Guid warehouseSite, ClaimsPrincipal user, string permission, bool consumption, CancellationToken ct) { var order = await database.CorrectiveWorkOrders.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ValidationException("Work order was not found."); RequireSite(user, permission, order.SiteId); if (order.SiteId != warehouseSite) throw new ValidationException("The warehouse and work order must belong to the same site."); if (order.Status is CorrectiveWorkOrderStatus.Completed or CorrectiveWorkOrderStatus.Cancelled or CorrectiveWorkOrderStatus.PendingVerification || (consumption && order.Status == CorrectiveWorkOrderStatus.Open)) throw new ConflictException("The work order is not eligible for this inventory operation."); return order; }
    private async Task ValidateParent(Guid warehouse, Guid? parent, Guid? id, CancellationToken ct) { if (parent is null) return; var node = await database.WarehouseStockLocations.SingleOrDefaultAsync(x => x.Id == parent, ct) ?? throw new ValidationException("Parent stock location was not found."); if (node.WarehouseId != warehouse) throw new ValidationException("Parent stock location belongs to another warehouse."); for (var cursor = node; ; ) { if (cursor.Id == id) throw new ValidationException("Stock-location hierarchy cannot contain a cycle."); if (cursor.ParentLocationId is null) break; cursor = await database.WarehouseStockLocations.SingleAsync(x => x.Id == cursor.ParentLocationId, ct); } }
    private WorkOrderPartConsumption Consumption(Guid requestId, CorrectiveWorkOrder order, Guid? reservationId, Guid part, Guid warehouse, Guid location, decimal quantity, decimal cost, ClaimsPrincipal user, string? notes) { var entity = new WorkOrderPartConsumption { Id = Guid.NewGuid(), TenantId = TenantId, RequestId = requestId, WorkOrderId = order.Id, ReservationId = reservationId, PartId = part, WarehouseId = warehouse, StockLocationId = location, Quantity = quantity, UnitCost = cost, TotalCost = Money(quantity * cost), ConsumedById = Actor(user), ConsumedAtUtc = Now, Notes = Clean(notes) }; database.WorkOrderPartConsumptions.Add(entity); return entity; }
    private void Movement(Guid warehouse, Guid location, Guid part, InventoryMovementType type, decimal quantity, decimal unitCost, decimal value, string referenceType, Guid referenceId, string? referenceNumber, ClaimsPrincipal user, string? notes) => database.InventoryMovements.Add(new() { Id = Guid.NewGuid(), TenantId = TenantId, WarehouseId = warehouse, StockLocationId = location, PartId = part, Type = type, Quantity = quantity, UnitCost = unitCost, TotalValue = value, ReferenceType = referenceType, ReferenceId = referenceId, ReferenceNumber = referenceNumber, ActorUserId = Actor(user), OccurredAtUtc = Now, Notes = Clean(notes) });
    private void WorkOrderActivity(CorrectiveWorkOrder order, string action, string details, ClaimsPrincipal user) => database.WorkOrderActivities.Add(new() { Id = Guid.NewGuid(), TenantId = TenantId, WorkOrderId = order.Id, ActorUserId = Actor(user), Action = action, Details = details, OccurredAtUtc = Now });
    private async Task<IDbContextTransaction?> Transaction(CancellationToken ct) => database.Database.IsRelational() && database.Database.CurrentTransaction is null ? await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct) : null;
    private async Task Save(CancellationToken ct) { try { await database.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new ConflictException("Stock changed concurrently. Refresh and retry with a new request ID."); } catch (DbUpdateException ex) when (ex.InnerException is not null) { throw new ConflictException("The inventory operation conflicts with current stock or a previously submitted request."); } }
    private void RequireTenant(ClaimsPrincipal user, string permission) { if (!scopes.HasTenantAccess(user, permission)) throw new UnauthorizedAccessException("Tenant-wide permission is required."); }
    private void RequireTenantOrAnySite(ClaimsPrincipal user, string permission) { if (!scopes.HasTenantAccess(user, permission) && GrantedSites(user, permission).Length == 0) throw new UnauthorizedAccessException("Inventory access is required."); }
    private void RequireSite(ClaimsPrincipal user, string permission, Guid site) { if (!scopes.HasAccess(user, permission, site)) throw new UnauthorizedAccessException("The requested site is outside the granted scope."); }
    private static Guid[] GrantedSites(ClaimsPrincipal user, string permission) => user.FindAll(TenantClaims.Grant).Select(x => x.Value.Split('|')).Where(x => x.Length == 3 && x[0] == permission && x[1] == nameof(AccessScopeKind.Site) && Guid.TryParse(x[2], out _)).Select(x => Guid.Parse(x[2])).Distinct().ToArray();
    private static (int, int) Page(int page, int size) => (Math.Max(1, page), Math.Clamp(size, 1, 100));
    private static Guid Actor(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue("sub"), out var id) ? id : throw new UnauthorizedAccessException();
    private static string Required(string? value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ValidationException($"{name} is required.") : value.Trim();
    private static string Code(string? value) => Required(value, "Code").ToUpperInvariant();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Reference(string? value, string prefix) => string.IsNullOrWhiteSpace(value) ? $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25].ToUpperInvariant() : value.Trim();
    private static decimal Money(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    private static decimal Unit(decimal value) => decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    private static void Positive(decimal value, string name) { if (value <= 0) throw new ValidationException($"{name} must be greater than zero."); }
    private static void ValidateRequest(Guid value) { if (value == Guid.Empty) throw new ValidationException("A request ID is required for retry safety."); }
    private static void Version(Guid current, Guid supplied) { if (supplied == Guid.Empty || current != supplied) throw new ConflictException("The record was changed by another user."); }
    private static void ValidatePart(SparePartInput input) { if (!Enum.IsDefined(input.UnitOfMeasure)) throw new ValidationException("Unit of measure is invalid."); if (input.MinimumStock < 0 || input.ReorderPoint < 0) throw new ValidationException("Stock thresholds cannot be negative."); Required(input.NameEn, "English name"); }
    private static void Apply(SparePart entity, SparePartInput input) { entity.NameEn = Required(input.NameEn, "English name"); entity.NameAr = Clean(input.NameAr); entity.Description = Clean(input.Description); entity.Category = Clean(input.Category); entity.Manufacturer = Clean(input.Manufacturer); entity.ManufacturerPartNumber = Clean(input.ManufacturerPartNumber); entity.UnitOfMeasure = input.UnitOfMeasure; entity.Barcode = Clean(input.Barcode); entity.MinimumStock = input.MinimumStock; entity.ReorderPoint = input.ReorderPoint; entity.PreferredSupplier = Clean(input.PreferredSupplier); }
}

internal static class InventoryQueryExtensions
{
    public static IQueryable<InventoryBalance> IncludeInventoryBalance(this IQueryable<InventoryBalance> query) => query.Include(x => x.Warehouse).ThenInclude(x => x!.Site).Include(x => x.StockLocation).Include(x => x.Part);
    public static IQueryable<InventoryMovement> IncludeInventory(this IQueryable<InventoryMovement> query) => query.Include(x => x.Warehouse).Include(x => x.StockLocation).Include(x => x.Part).Include(x => x.ActorUser);
}

public static class InventoryWorkOrderLifecycle
{
    public static async Task ReleaseOpenReservations(PlatformDbContext database, CorrectiveWorkOrder order, Guid actor, DateTimeOffset now, CancellationToken ct)
    {
        var reservations = await database.StockReservations.Where(x => x.WorkOrderId == order.Id && (x.State == StockReservationState.Active || x.State == StockReservationState.PartiallyConsumed)).ToListAsync(ct);
        foreach (var reservation in reservations)
        {
            var remaining = reservation.ReservedQuantity - reservation.ConsumedQuantity; var balance = await database.InventoryBalances.SingleAsync(x => x.WarehouseId == reservation.WarehouseId && x.StockLocationId == reservation.StockLocationId && x.PartId == reservation.PartId, ct); balance.QuantityReserved -= remaining; balance.Version = Guid.NewGuid(); reservation.State = StockReservationState.Released; reservation.ReleasedAtUtc = now; reservation.Version = Guid.NewGuid(); database.InventoryMovements.Add(new() { Id = Guid.NewGuid(), TenantId = order.TenantId, WarehouseId = reservation.WarehouseId, StockLocationId = reservation.StockLocationId, PartId = reservation.PartId, Type = InventoryMovementType.ReservationReleased, Quantity = remaining, ReferenceType = nameof(StockReservation), ReferenceId = reservation.Id, ReferenceNumber = order.Number, ActorUserId = actor, OccurredAtUtc = now, Notes = "Automatically released when work order closed." });
        }
    }
}
