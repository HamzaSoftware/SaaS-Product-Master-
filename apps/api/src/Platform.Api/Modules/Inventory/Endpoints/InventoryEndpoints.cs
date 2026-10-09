using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Api.Modules.Inventory.Application;
using SaaSPlatform.Api.Modules.Inventory.Domain;

namespace SaaSPlatform.Api.Modules.Inventory.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/inventory").WithTags("Inventory").RequireAuthorization("PlatformAccess");
        group.MapGet("/dashboard", async (IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Dashboard(user, ct))));
        group.MapGet("/warehouses", async ([AsParameters] InventoryListQuery query, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(MapPage(await service.Warehouses(user, query, ct), Map)));
        group.MapGet("/warehouses/{id:guid}", async (Guid id, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Warehouse(user, id, ct))));
        group.MapPost("/warehouses", async ([FromBody] WarehouseInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Created("/api/v1/inventory/warehouses", Map(await service.CreateWarehouse(user, input, ct))));
        group.MapPut("/warehouses/{id:guid}", async (Guid id, [FromBody] WarehouseInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.UpdateWarehouse(user, id, input, ct))));
        group.MapPatch("/warehouses/{id:guid}/status", async (Guid id, [FromBody] WarehouseStatusInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.SetWarehouseStatus(user, id, input, ct))));
        group.MapGet("/locations", async (Guid? warehouseId, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok((await service.Locations(user, warehouseId, ct)).Select(Map)));
        group.MapPost("/locations", async ([FromBody] StockLocationInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Created("/api/v1/inventory/locations", Map(await service.CreateLocation(user, input, ct))));
        group.MapPut("/locations/{id:guid}", async (Guid id, [FromBody] StockLocationInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.UpdateLocation(user, id, input, ct))));
        group.MapPatch("/locations/{id:guid}/status", async (Guid id, [FromBody] StockLocationStatusInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.SetLocationStatus(user, id, input, ct))));
        group.MapGet("/parts", async ([AsParameters] InventoryListQuery query, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(MapPage(await service.Parts(user, query, ct), Map)));
        group.MapGet("/parts/{id:guid}", async (Guid id, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Part(user, id, ct))));
        group.MapPost("/parts", async ([FromBody] SparePartInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Created("/api/v1/inventory/parts", Map(await service.CreatePart(user, input, ct))));
        group.MapPut("/parts/{id:guid}", async (Guid id, [FromBody] SparePartInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.UpdatePart(user, id, input, ct))));
        group.MapPatch("/parts/{id:guid}/status", async (Guid id, [FromBody] SparePartStatusInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.SetPartStatus(user, id, input, ct))));
        group.MapGet("/balances", async ([AsParameters] InventoryListQuery query, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(MapPage(await service.Balances(user, query, ct), Map)));
        group.MapGet("/movements", async ([AsParameters] InventoryListQuery query, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(MapPage(await service.Movements(user, query, ct), Map)));
        group.MapPost("/receipts", async ([FromBody] StockReceiptInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Created("/api/v1/inventory/receipts", Map(await service.Receive(user, input, ct))));
        group.MapPost("/issues", async ([FromBody] StockIssueInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Created("/api/v1/inventory/issues", Map(await service.Issue(user, input, ct))));
        group.MapPost("/transfers", async ([FromBody] StockTransferInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Created("/api/v1/inventory/transfers", Map(await service.Transfer(user, input, ct))));
        group.MapPost("/adjustments", async ([FromBody] StockAdjustmentInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Created("/api/v1/inventory/adjustments", Map(await service.Adjust(user, input, ct))));
        group.MapPost("/reservations", async ([FromBody] StockReservationInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Created("/api/v1/inventory/reservations", Map(await service.Reserve(user, input, ct))));
        group.MapPost("/reservations/{id:guid}/consume", async (Guid id, [FromBody] ReservationConsumeInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Consume(user, id, input, ct))));
        group.MapPost("/reservations/{id:guid}/release", async (Guid id, [FromBody] ReservationReleaseInput input, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.Release(user, id, input, ct))));
        endpoints.MapGet("/api/v1/work-orders/{id:guid}/materials", async (Guid id, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(Map(await service.WorkOrderMaterials(user, id, ct)))).RequireAuthorization("PlatformAccess").WithTags("Inventory");
        endpoints.MapGet("/api/v1/assets/{id:guid}/part-consumptions", async (Guid id, IInventoryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok((await service.AssetConsumptions(user, id, ct)).Select(Map))).RequireAuthorization("PlatformAccess").WithTags("Inventory");
        return endpoints;
    }

    private static object Map(InventoryDashboard x) => new { x.TotalParts, x.TotalStockValue, x.LowStockItems, x.OutOfStockItems, x.ActiveReservations, x.ReservedQuantity, RecentMovements = x.RecentMovements.Select(Map) };
    private static object Map(Warehouse x) => new { x.Id, x.Code, x.NameEn, x.NameAr, x.SiteId, SiteNameEn = x.Site?.NameEn, SiteNameAr = x.Site?.NameAr, x.Description, x.IsActive, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc, Locations = x.Locations.Select(Map) };
    private static object Map(WarehouseStockLocation x) => new { x.Id, x.WarehouseId, x.ParentLocationId, x.Code, x.NameEn, x.NameAr, x.IsActive, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc };
    private static object Map(SparePart x) => new { x.Id, x.Sku, x.NameEn, x.NameAr, x.Description, x.Category, x.Manufacturer, x.ManufacturerPartNumber, x.UnitOfMeasure, x.Barcode, x.IsActive, x.MinimumStock, x.ReorderPoint, x.PreferredSupplier, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc };
    private static object Map(InventoryBalance x) => new { x.Id, x.WarehouseId, WarehouseCode = x.Warehouse?.Code, WarehouseNameEn = x.Warehouse?.NameEn, WarehouseNameAr = x.Warehouse?.NameAr, SiteId = x.Warehouse?.SiteId, x.StockLocationId, StockLocationCode = x.StockLocation?.Code, StockLocationNameEn = x.StockLocation?.NameEn, StockLocationNameAr = x.StockLocation?.NameAr, x.PartId, PartSku = x.Part?.Sku, PartNameEn = x.Part?.NameEn, PartNameAr = x.Part?.NameAr, UnitOfMeasure = x.Part?.UnitOfMeasure, x.QuantityOnHand, x.QuantityReserved, x.QuantityAvailable, x.AverageUnitCost, x.InventoryValue, x.Version };
    private static object Map(InventoryMovement x) => new { x.Id, x.WarehouseId, WarehouseCode = x.Warehouse?.Code, x.StockLocationId, StockLocationCode = x.StockLocation?.Code, x.PartId, PartSku = x.Part?.Sku, PartNameEn = x.Part?.NameEn, PartNameAr = x.Part?.NameAr, x.Type, x.Quantity, x.UnitCost, x.TotalValue, x.ReferenceType, x.ReferenceId, x.ReferenceNumber, x.ActorUserId, ActorName = x.ActorUser?.DisplayName, x.OccurredAtUtc, x.Notes };
    private static object Map(StockReceipt x) => new { x.Id, x.RequestId, x.WarehouseId, x.StockLocationId, x.PartId, x.ReferenceNumber, x.SupplierName, x.SupplierReference, x.ReceiptDate, x.Quantity, x.UnitCost, x.TotalCost, x.ReceivedById, x.Notes, x.CreatedAtUtc };
    private static object Map(StockIssue x) => new { x.Id, x.RequestId, x.WarehouseId, x.StockLocationId, x.PartId, x.WorkOrderId, x.ReservationId, x.ReferenceNumber, x.Quantity, x.UnitCost, x.TotalCost, x.IssuedById, x.Reason, x.CreatedAtUtc };
    private static object Map(StockTransfer x) => new { x.Id, x.RequestId, x.ReferenceNumber, x.PartId, x.SourceWarehouseId, x.SourceLocationId, x.DestinationWarehouseId, x.DestinationLocationId, x.Quantity, x.UnitCost, x.TotalValue, x.State, x.PerformedById, x.Notes, x.Version, x.CreatedAtUtc };
    private static object Map(StockAdjustment x) => new { x.Id, x.RequestId, x.ReferenceNumber, x.WarehouseId, x.StockLocationId, x.PartId, x.PreviousQuantity, x.AdjustmentQuantity, x.NewQuantity, x.Reason, x.Notes, x.PerformedById, x.CreatedAtUtc };
    private static object Map(StockReservation x) => new { x.Id, x.RequestId, x.WorkOrderId, x.PartId, PartSku = x.Part?.Sku, PartNameEn = x.Part?.NameEn, PartNameAr = x.Part?.NameAr, x.WarehouseId, x.StockLocationId, x.ReservedQuantity, x.ConsumedQuantity, RemainingQuantity = x.ReservedQuantity - x.ConsumedQuantity, x.State, x.CreatedById, x.ReleasedAtUtc, x.Version, x.CreatedAtUtc };
    private static object Map(WorkOrderPartConsumption x) => new { x.Id, x.RequestId, x.WorkOrderId, x.ReservationId, x.PartId, PartSku = x.Part?.Sku, PartNameEn = x.Part?.NameEn, PartNameAr = x.Part?.NameAr, x.WarehouseId, x.StockLocationId, x.Quantity, x.UnitCost, x.TotalCost, x.ConsumedById, x.ConsumedAtUtc, x.Notes };
    private static object Map(WorkOrderMaterialSummary x) => new { Reservations = x.Reservations.Select(Map), Consumptions = x.Consumptions.Select(Map), x.TotalMaterialCost };
    private static object MapPage<T>(InventoryPage<T> page, Func<T, object> map) => new { Items = page.Items.Select(map), page.Page, page.PageSize, page.Total, TotalPages = Math.Max(1, (int)Math.Ceiling(page.Total / (double)page.PageSize)) };
}
