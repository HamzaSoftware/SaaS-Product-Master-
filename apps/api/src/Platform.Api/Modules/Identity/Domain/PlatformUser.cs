using Microsoft.AspNetCore.Identity;
using SaaSPlatform.Api.BuildingBlocks.Domain;

namespace SaaSPlatform.Api.Modules.Identity.Domain;

public sealed class PlatformUser : IdentityUser<Guid>, IHasTimestamps
{
    public required string DisplayName { get; set; }
    public bool IsPlatformAdmin { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
