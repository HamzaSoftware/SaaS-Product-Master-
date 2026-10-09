using SaaSPlatform.Api.BuildingBlocks.Domain;

namespace SaaSPlatform.Api.Modules.Tenancy.Domain;

public sealed class Tenant : IHasTimestamps
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
