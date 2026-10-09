using System.Security.Claims;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Identity.Endpoints;

public static class ContextEndpoints
{
    public static IEndpointRouteBuilder MapContextEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/context", (ClaimsPrincipal user, ICurrentTenant tenant) =>
            TypedResults.Ok(new
            {
                UserId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub"),
                TenantId = tenant.Id,
                Permissions = user.FindAll(TenantClaims.Permission).Select(x => x.Value).Order().ToArray()
            }))
            .RequireAuthorization("PlatformAccess")
            .WithTags("Identity")
            .WithName("GetCurrentContext");

        return endpoints;
    }
}
