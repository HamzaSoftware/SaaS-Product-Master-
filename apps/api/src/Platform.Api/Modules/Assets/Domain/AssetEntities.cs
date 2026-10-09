using SaaSPlatform.Api.BuildingBlocks.Domain;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Locations.Domain;

namespace SaaSPlatform.Api.Modules.Assets.Domain;

public enum AssetOperationalStatus { Operational = 0, UnderMaintenance = 1, OutOfService = 2, Standby = 3 }
public enum AssetLifecycleState { Active = 0, Retired = 1 }

public sealed class AssetCategory : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Code { get; set; }
    public required string NameEn { get; set; }
    public string? NameAr { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class Manufacturer : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public string? Code { get; set; }
    public string? Country { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class AssetModel : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ManufacturerId { get; set; }
    public Guid? CategoryId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public string? TechnicalDescription { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Manufacturer? Manufacturer { get; set; }
    public AssetCategory? Category { get; set; }
}

public sealed class Asset : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Code { get; set; }
    public required string NameEn { get; set; }
    public string? NameAr { get; set; }
    public Guid CategoryId { get; set; }
    public Guid SiteId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid? ManufacturerId { get; set; }
    public Guid? ModelId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Barcode { get; set; }
    public string? Description { get; set; }
    public DateOnly? PurchaseDate { get; set; }
    public decimal? PurchaseCost { get; set; }
    public string? Currency { get; set; }
    public DateOnly? InstallationDate { get; set; }
    public DateOnly? WarrantyStartDate { get; set; }
    public DateOnly? WarrantyExpiryDate { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierReference { get; set; }
    public Guid? ResponsibleUserId { get; set; }
    public string? Notes { get; set; }
    public AssetOperationalStatus OperationalStatus { get; set; } = AssetOperationalStatus.Operational;
    public AssetLifecycleState LifecycleState { get; set; } = AssetLifecycleState.Active;
    public DateTimeOffset? RetiredAtUtc { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public AssetCategory? Category { get; set; }
    public Site? Site { get; set; }
    public LocationNode? Location { get; set; }
    public Manufacturer? Manufacturer { get; set; }
    public AssetModel? Model { get; set; }
    public PlatformUser? ResponsibleUser { get; set; }
}

public sealed class AssetActivity : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AssetId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string Action { get; set; }
    public string? ChangesJson { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Asset? Asset { get; set; }
}
