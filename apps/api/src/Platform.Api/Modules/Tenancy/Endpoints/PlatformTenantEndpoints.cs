using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Tenancy.Endpoints;

public static class PlatformTenantEndpoints
{
    public static IEndpointRouteBuilder MapPlatformTenantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/platform/tenants").WithTags("Platform tenants");
        group.MapGet("/", async (IPlatformTenantService service, CancellationToken ct) => Results.Ok((await service.List(ct)).Select(Map)))
            .RequireAuthorization(PlatformPermissions.TenantView);
        group.MapPost("/", async ([FromBody] CreateTenantRequest request, IPlatformTenantService service, CancellationToken ct) =>
            Results.Created("/api/v1/platform/tenants", Map(await service.Create(request, ct))))
            .RequireAuthorization(PlatformPermissions.TenantCreate);
        group.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] TenantStatusRequest request, IPlatformTenantService service, CancellationToken ct) =>
            Results.Ok(Map(await service.SetStatus(id, request, ct)))).RequireAuthorization("PlatformAdministrator");
        return endpoints;
    }

    private static object Map(Modules.Tenancy.Domain.Tenant x) => new
        { x.Id, x.Name, x.Slug, x.IsActive, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc };
}
