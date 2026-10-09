using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.BuildingBlocks.Domain;

namespace SaaSPlatform.Api.Modules.Tenancy.Domain;

public sealed class TenantMembership : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public Guid AuthorizationVersion { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Tenant? Tenant { get; set; }
    public PlatformUser? User { get; set; }
    public ICollection<MembershipRole> Roles { get; set; } = [];
}
