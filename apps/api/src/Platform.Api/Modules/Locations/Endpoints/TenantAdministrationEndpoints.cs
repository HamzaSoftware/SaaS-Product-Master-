using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Locations.Application;
using SaaSPlatform.Api.Modules.Locations.Domain;
using SaaSPlatform.Api.Modules.Organization.Domain;

namespace SaaSPlatform.Api.Modules.Locations.Endpoints;

public static class TenantAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapTenantAdministrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var company = endpoints.MapGroup("/api/v1/company").WithTags("Company");
        company.MapGet("/", async (ITenantAdministrationService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) =>
            scopes.HasTenantAccess(user, TenantPermissions.CompanyView) ? Results.Ok(Map(await service.GetCompany(ct))) : Results.Forbid())
            .RequireAuthorization(TenantPermissions.CompanyView);
        company.MapPut("/", async ([FromBody] CompanyInput input, ITenantAdministrationService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) =>
            scopes.HasTenantAccess(user, TenantPermissions.CompanyEdit) ? Results.Ok(Map(await service.UpdateCompany(input, ct))) : Results.Forbid())
            .RequireAuthorization(TenantPermissions.CompanyEdit);

        var sites = endpoints.MapGroup("/api/v1/sites").WithTags("Sites");
        sites.MapGet("/", async (string? search, int? page, int? pageSize, ITenantAdministrationService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var result = await service.GetSites(user, search, page ?? 1, pageSize ?? 25, ct);
            return Results.Ok(new { Items = result.Items.Select(Map), result.Page, result.PageSize, result.Total });
        }).RequireAuthorization(TenantPermissions.SiteView);
        sites.MapPost("/", async ([FromBody] SiteInput input, ITenantAdministrationService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Created("/api/v1/sites", Map(await service.CreateSite(user, input, ct)))).RequireAuthorization(TenantPermissions.SiteCreate);
        sites.MapPut("/{id:guid}", async (Guid id, [FromBody] SiteInput input, ITenantAdministrationService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(Map(await service.UpdateSite(user, id, input, ct)))).RequireAuthorization(TenantPermissions.SiteEdit);
        sites.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] StatusInput input, ITenantAdministrationService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(Map(await service.SetSiteStatus(user, id, input, ct)))).RequireAuthorization();

        var types = endpoints.MapGroup("/api/v1/location-types").WithTags("Location types");
        types.MapGet("/", async (ITenantAdministrationService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) =>
            scopes.HasTenantAccess(user, TenantPermissions.LocationTypeView) ? Results.Ok((await service.GetLocationTypes(ct)).Select(Map)) : Results.Forbid())
            .RequireAuthorization(TenantPermissions.LocationTypeView);
        types.MapPost("/", async ([FromBody] LocationTypeInput input, ITenantAdministrationService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) =>
            scopes.HasTenantAccess(user, TenantPermissions.LocationTypeCreate) ? Results.Created("/api/v1/location-types", Map(await service.CreateLocationType(input, ct))) : Results.Forbid())
            .RequireAuthorization(TenantPermissions.LocationTypeCreate);
        types.MapPut("/{id:guid}", async (Guid id, [FromBody] LocationTypeInput input, ITenantAdministrationService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) =>
            scopes.HasTenantAccess(user, TenantPermissions.LocationTypeEdit) ? Results.Ok(Map(await service.UpdateLocationType(id, input, ct))) : Results.Forbid())
            .RequireAuthorization(TenantPermissions.LocationTypeEdit);
        types.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] StatusInput input, ITenantAdministrationService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var permission = input.IsActive ? TenantPermissions.LocationTypeActivate : TenantPermissions.LocationTypeDeactivate;
            return scopes.HasTenantAccess(user, permission) ? Results.Ok(Map(await service.SetLocationTypeStatus(id, input, ct))) : Results.Forbid();
        }).RequireAuthorization();

        var locations = endpoints.MapGroup("/api/v1/locations").WithTags("Locations");
        locations.MapGet("/", async (Guid siteId, ITenantAdministrationService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(MapLocations(await service.GetLocations(user, siteId, ct)))).RequireAuthorization(TenantPermissions.LocationView);
        locations.MapPost("/", async ([FromBody] LocationInput input, ITenantAdministrationService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Created("/api/v1/locations", Map(await service.CreateLocation(user, input, ct)))).RequireAuthorization(TenantPermissions.LocationCreate);
        locations.MapPut("/{id:guid}", async (Guid id, [FromBody] LocationInput input, ITenantAdministrationService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(Map(await service.UpdateLocation(user, id, input, ct)))).RequireAuthorization(TenantPermissions.LocationEdit);
        locations.MapPatch("/{id:guid}/move", async (Guid id, [FromBody] MoveLocationInput input, ITenantAdministrationService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(Map(await service.MoveLocation(user, id, input, ct)))).RequireAuthorization(TenantPermissions.LocationMove);
        locations.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] StatusInput input, ITenantAdministrationService service, ClaimsPrincipal user, CancellationToken ct) =>
            Results.Ok(Map(await service.SetLocationStatus(user, id, input, ct)))).RequireAuthorization();

        return endpoints;
    }

    private static object Map(CompanyProfile x) => new { x.Id, x.NameEn, x.NameAr, x.LegalNameEn, x.LegalNameAr,
        x.LogoReference, x.Email, x.Phone, x.Website, x.Country, x.City, x.Address, x.TimeZone, x.DefaultLanguage,
        x.Version, x.CreatedAtUtc, x.UpdatedAtUtc };
    private static object Map(Site x) => new { x.Id, x.Code, x.NameEn, x.NameAr, x.Description, x.Address, x.City,
        x.Country, x.TimeZone, x.Latitude, x.Longitude, x.IsActive, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc };
    private static object Map(LocationType x) => new { x.Id, x.Code, x.NameEn, x.NameAr, x.Description, x.IsActive,
        x.SortOrder, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc };
    private static object Map(LocationNode x) => new { x.Id, x.SiteId, x.ParentLocationId, x.LocationTypeId, x.Code,
        x.NameEn, x.NameAr, x.Description, x.IsActive, x.SortOrder, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc };

    private static IEnumerable<object> MapLocations(IReadOnlyList<LocationNode> nodes)
    {
        var byId = nodes.ToDictionary(x => x.Id);
        IReadOnlyList<LocationNode> Path(LocationNode node)
        {
            var path = new List<LocationNode>(); var seen = new HashSet<Guid>(); LocationNode? cursor = node;
            while (cursor is not null && seen.Add(cursor.Id))
            {
                path.Add(cursor);
                cursor = cursor.ParentLocationId is Guid parentId ? byId.GetValueOrDefault(parentId) : null;
            }
            path.Reverse(); return path;
        }
        return nodes.Select(x =>
        {
            var path = Path(x);
            return (object)new { x.Id, x.SiteId, x.ParentLocationId, x.LocationTypeId, x.Code, x.NameEn, x.NameAr,
                x.Description, x.IsActive, x.SortOrder, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc,
                PathIds = path.Select(p => p.Id), PathEn = string.Join(" / ", path.Select(p => p.NameEn)),
                PathAr = string.Join(" / ", path.Select(p => p.NameAr ?? p.NameEn)) };
        });
    }
}
