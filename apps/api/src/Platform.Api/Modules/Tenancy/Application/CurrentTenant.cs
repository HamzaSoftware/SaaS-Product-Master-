using System.Security.Claims;

namespace SaaSPlatform.Api.Modules.Tenancy.Application;

public interface ICurrentTenant
{
    Guid? Id { get; }
    void Initialize(Guid tenantId);
}

public sealed class CurrentTenant : ICurrentTenant
{
    public Guid? Id { get; private set; }

    public void Initialize(Guid tenantId)
    {
        if (Id is not null && Id != tenantId)
        {
            throw new InvalidOperationException("The tenant context cannot change during a request.");
        }

        Id = tenantId;
    }
}

public static class TenantClaims
{
    public const string TenantId = "tenant_id";
    public const string Permission = "permission";
    public const string MembershipId = "membership_id";
    public const string Grant = "grant";
    public const string PlatformAdmin = "platform_admin";
    public const string SessionId = "session_id";
    public const string AuthorizationVersion = "authorization_version";

    public static Guid? GetTenantId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(TenantId), out var tenantId) ? tenantId : null;
}
