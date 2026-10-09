using System.Security.Claims;
using System.Text.Json;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Analytics.Application;

public interface IAuditWriter
{
    void Add(string action, string entityType, Guid entityId, object? changes = null);
}

public sealed class AuditWriter(
    PlatformDbContext database,
    ICurrentTenant currentTenant,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider) : IAuditWriter
{
    public void Add(string action, string entityType, Guid entityId, object? changes = null)
    {
        var context = httpContextAccessor.HttpContext;
        database.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = currentTenant.Id ?? throw new InvalidOperationException("A tenant context is required for auditing."),
            UserId = Guid.TryParse(context?.User.FindFirstValue("sub"), out var userId) ? userId : null,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ChangesJson = changes is null ? null : JsonSerializer.Serialize(changes),
            CorrelationId = context?.TraceIdentifier,
            OccurredAtUtc = timeProvider.GetUtcNow()
        });
    }
}
