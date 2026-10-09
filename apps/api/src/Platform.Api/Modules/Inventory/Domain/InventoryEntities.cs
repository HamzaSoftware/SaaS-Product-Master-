using SaaSPlatform.Api.BuildingBlocks.Domain;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Locations.Domain;
using SaaSPlatform.Api.Modules.Maintenance.Domain;

namespace SaaSPlatform.Api.Modules.Inventory.Domain;

public enum InventoryUnitOfMeasure { Piece = 0, Box = 1, Meter = 2, Liter = 3, Kilogram = 4 }
public enum InventoryMovementType { Receipt = 0, Issue = 1, TransferOut = 2, TransferIn = 3, AdjustmentIncrease = 4, AdjustmentDecrease = 5, ReservationCreated = 6, ReservationReleased = 7, ReservationConsumed = 8 }
public enum StockTransferState { Draft = 0, Submitted = 1, Completed = 2, Cancelled = 3 }
public enum StockReservationState { Active = 0, PartiallyConsumed = 1, Consumed = 2, Released = 3 }
public enum StockAdjustmentReason { PhysicalCount = 0, Damaged = 1, Lost = 2, AdministrativeCorrection = 3 }

public sealed class Warehouse : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Code { get; set; }
    public required string NameEn { get; set; }
    public string? NameAr { get; set; }
    public Guid SiteId { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Site? Site { get; set; }
    public ICollection<WarehouseStockLocation> Locations { get; set; } = [];
}

public sealed class WarehouseStockLocation : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? ParentLocationId { get; set; }
    public required string Code { get; set; }
    public required string NameEn { get; set; }
    public string? NameAr { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Warehouse? Warehouse { get; set; }
    public WarehouseStockLocation? Parent { get; set; }
    public ICollection<WarehouseStockLocation> Children { get; set; } = [];
}

public sealed class SparePart : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Sku { get; set; }
    public required string NameEn { get; set; }
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Manufacturer { get; set; }
    public string? ManufacturerPartNumber { get; set; }
    public InventoryUnitOfMeasure UnitOfMeasure { get; set; }
    public string? Barcode { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal MinimumStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public string? PreferredSupplier { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class InventoryBalance : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid StockLocationId { get; set; }
    public Guid PartId { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal QuantityReserved { get; set; }
    public decimal AverageUnitCost { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;
    public decimal InventoryValue => decimal.Round(QuantityOnHand * AverageUnitCost, 2, MidpointRounding.AwayFromZero);
    public Warehouse? Warehouse { get; set; }
    public WarehouseStockLocation? StockLocation { get; set; }
    public SparePart? Part { get; set; }
}

public sealed class InventoryMovement : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid StockLocationId { get; set; }
    public Guid PartId { get; set; }
    public InventoryMovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public required string ReferenceType { get; set; }
    public Guid ReferenceId { get; set; }
    public string? ReferenceNumber { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string? Notes { get; set; }
    public Warehouse? Warehouse { get; set; }
    public WarehouseStockLocation? StockLocation { get; set; }
    public SparePart? Part { get; set; }
    public PlatformUser? ActorUser { get; set; }
}

public sealed class StockReceipt : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid StockLocationId { get; set; }
    public Guid PartId { get; set; }
    public required string ReferenceNumber { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierReference { get; set; }
    public DateOnly ReceiptDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public Guid ReceivedById { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class StockIssue : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid StockLocationId { get; set; }
    public Guid PartId { get; set; }
    public Guid? WorkOrderId { get; set; }
    public Guid? ReservationId { get; set; }
    public required string ReferenceNumber { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public Guid IssuedById { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class StockTransfer : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public required string ReferenceNumber { get; set; }
    public Guid PartId { get; set; }
    public Guid SourceWarehouseId { get; set; }
    public Guid SourceLocationId { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public Guid DestinationLocationId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public StockTransferState State { get; set; } = StockTransferState.Completed;
    public Guid PerformedById { get; set; }
    public string? Notes { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class StockAdjustment : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public required string ReferenceNumber { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid StockLocationId { get; set; }
    public Guid PartId { get; set; }
    public decimal PreviousQuantity { get; set; }
    public decimal AdjustmentQuantity { get; set; }
    public decimal NewQuantity { get; set; }
    public StockAdjustmentReason Reason { get; set; }
    public string? Notes { get; set; }
    public Guid PerformedById { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class StockReservation : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid PartId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid StockLocationId { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public StockReservationState State { get; set; } = StockReservationState.Active;
    public Guid CreatedById { get; set; }
    public DateTimeOffset? ReleasedAtUtc { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public CorrectiveWorkOrder? WorkOrder { get; set; }
    public SparePart? Part { get; set; }
}

public sealed class WorkOrderPartConsumption : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public Guid WorkOrderId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid PartId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid StockLocationId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public Guid ConsumedById { get; set; }
    public DateTimeOffset ConsumedAtUtc { get; set; }
    public string? Notes { get; set; }
    public CorrectiveWorkOrder? WorkOrder { get; set; }
    public SparePart? Part { get; set; }
}
