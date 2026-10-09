namespace SaaSPlatform.Api.BuildingBlocks.Domain;

public interface ITenantOwned
{
    Guid TenantId { get; set; }
}
