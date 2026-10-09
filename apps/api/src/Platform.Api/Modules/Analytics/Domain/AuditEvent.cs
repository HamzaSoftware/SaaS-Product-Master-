using SaaSPlatform.Api.BuildingBlocks.Domain;

namespace SaaSPlatform.Api.Modules.Analytics.Domain;

public sealed class AuditEvent : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public string? ChangesJson { get; set; }
    public string? CorrelationId { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
}
