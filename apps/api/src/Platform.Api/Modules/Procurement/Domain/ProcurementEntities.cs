using SaaSPlatform.Api.BuildingBlocks.Domain;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Inventory.Domain;
using SaaSPlatform.Api.Modules.Locations.Domain;

namespace SaaSPlatform.Api.Modules.Procurement.Domain;

public enum SupplierStatus { Active = 0, Inactive = 1 }
public enum ProcurementPriority { Low = 0, Medium = 1, High = 2, Critical = 3 }
public enum PurchaseRequisitionStatus { Draft = 0, Submitted = 1, Approved = 2, Rejected = 3, PartiallyConverted = 4, Converted = 5, Closed = 6, Cancelled = 7 }
public enum PurchaseOrderStatus { Draft = 0, Issued = 1, PartiallyReceived = 2, FullyReceived = 3, Closed = 4, Cancelled = 5 }

public sealed class ProcurementDocumentSequence : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string DocumentType { get; set; }
    public int Year { get; set; }
    public long NextValue { get; set; } = 1;
    public Guid Version { get; set; } = Guid.NewGuid();
}

public sealed class Supplier : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Code { get; set; }
    public required string NameEn { get; set; }
    public string? NameAr { get; set; }
    public SupplierStatus Status { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? TaxNumber { get; set; }
    public string? CommercialRegistrationNumber { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Website { get; set; }
    public string? PaymentTerms { get; set; }
    public required string DefaultCurrency { get; set; }
    public string? Notes { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<SupplierPart> Parts { get; set; } = [];
}

public sealed class SupplierPart : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid PartId { get; set; }
    public string? SupplierPartNumber { get; set; }
    public decimal? LastUnitPrice { get; set; }
    public required string Currency { get; set; }
    public decimal MinimumOrderQuantity { get; set; } = 1;
    public int LeadTimeDays { get; set; }
    public bool IsPreferred { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Supplier? Supplier { get; set; }
    public SparePart? Part { get; set; }
}

public sealed class PurchaseRequisition : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Number { get; set; }
    public Guid SiteId { get; set; }
    public Guid TargetWarehouseId { get; set; }
    public Guid RequestedById { get; set; }
    public DateOnly RequestDate { get; set; }
    public DateOnly? NeedByDate { get; set; }
    public ProcurementPriority Priority { get; set; }
    public required string BusinessJustification { get; set; }
    public string? Notes { get; set; }
    public PurchaseRequisitionStatus Status { get; set; }
    public Guid? ReviewedById { get; set; }
    public DateTimeOffset? ReviewedAtUtc { get; set; }
    public string? ReviewComment { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Site? Site { get; set; }
    public Warehouse? TargetWarehouse { get; set; }
    public PlatformUser? RequestedBy { get; set; }
    public PlatformUser? ReviewedBy { get; set; }
    public ICollection<PurchaseRequisitionLine> Lines { get; set; } = [];
}

public sealed class PurchaseRequisitionLine : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequisitionId { get; set; }
    public Guid PartId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public InventoryUnitOfMeasure UnitOfMeasure { get; set; }
    public Guid? SuggestedSupplierId { get; set; }
    public decimal? EstimatedUnitPrice { get; set; }
    public decimal EstimatedTotal { get; set; }
    public string? Notes { get; set; }
    public decimal ConvertedQuantity { get; set; }
    public PurchaseRequisition? Requisition { get; set; }
    public SparePart? Part { get; set; }
    public Supplier? SuggestedSupplier { get; set; }
}

public sealed class PurchaseOrder : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Number { get; set; }
    public Guid SupplierId { get; set; }
    public Guid SiteId { get; set; }
    public Guid DestinationWarehouseId { get; set; }
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDeliveryDate { get; set; }
    public required string Currency { get; set; }
    public string? PaymentTerms { get; set; }
    public string? Notes { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public Guid CreatedById { get; set; }
    public Guid? IssuedById { get; set; }
    public DateTimeOffset? IssuedAtUtc { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Supplier? Supplier { get; set; }
    public Site? Site { get; set; }
    public Warehouse? DestinationWarehouse { get; set; }
    public ICollection<PurchaseOrderLine> Lines { get; set; } = [];
}

public sealed class PurchaseOrderLine : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid PartId { get; set; }
    public Guid? SourceRequisitionLineId { get; set; }
    public string? SupplierPartNumber { get; set; }
    public decimal OrderedQuantity { get; set; }
    public InventoryUnitOfMeasure UnitOfMeasure { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineSubtotal { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public SparePart? Part { get; set; }
    public PurchaseRequisitionLine? SourceRequisitionLine { get; set; }
}

public sealed class PurchaseReceipt : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequestId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public required string ReceiptNumber { get; set; }
    public string? SupplierReference { get; set; }
    public DateOnly ReceiptDate { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid StockLocationId { get; set; }
    public Guid ReceivedById { get; set; }
    public string? Notes { get; set; }
    public decimal TotalCost { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public ICollection<PurchaseReceiptLine> Lines { get; set; } = [];
}

public sealed class PurchaseReceiptLine : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PurchaseReceiptId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid StockReceiptId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public PurchaseReceipt? PurchaseReceipt { get; set; }
    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
}

public sealed class ProcurementActivity : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public required string Action { get; set; }
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string? Comment { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public PlatformUser? ActorUser { get; set; }
}
