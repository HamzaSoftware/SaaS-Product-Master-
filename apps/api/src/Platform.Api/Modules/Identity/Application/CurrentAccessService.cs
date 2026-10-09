using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Identity.Application;

public interface ICurrentAccessService
{
    Task<bool> IsActiveMember(ClaimsPrincipal user, CancellationToken ct);
    bool HasPermission(ClaimsPrincipal user, string permission);
    Task<bool> HasSiteAccess(ClaimsPrincipal user, string permission, Guid siteId, CancellationToken ct);
    IReadOnlyCollection<Guid> GrantedSiteIds(ClaimsPrincipal user, string permission);
}

public sealed class CurrentAccessService(PlatformDbContext database, ISiteScopeAuthorizer scopes) : ICurrentAccessService
{
    public async Task<bool> IsActiveMember(ClaimsPrincipal user, CancellationToken ct) =>
        Guid.TryParse(user.FindFirstValue(TenantClaims.MembershipId), out var membershipId) &&
        await database.TenantMemberships.AsNoTracking().AnyAsync(x => x.Id == membershipId && x.IsActive, ct);
    public bool HasPermission(ClaimsPrincipal user, string permission) => user.HasClaim(TenantClaims.Permission, permission);
    public async Task<bool> HasSiteAccess(ClaimsPrincipal user, string permission, Guid siteId, CancellationToken ct) =>
        scopes.HasAccess(user, permission, siteId) && await database.Sites.AsNoTracking().AnyAsync(x => x.Id == siteId && x.IsActive, ct);
    public IReadOnlyCollection<Guid> GrantedSiteIds(ClaimsPrincipal user, string permission) => user.FindAll(TenantClaims.Grant)
        .Select(x => x.Value.Split('|')).Where(x => x.Length == 3 && x[0] == permission && x[1] == "Site" && Guid.TryParse(x[2], out _))
        .Select(x => Guid.Parse(x[2])).Distinct().ToArray();
}
