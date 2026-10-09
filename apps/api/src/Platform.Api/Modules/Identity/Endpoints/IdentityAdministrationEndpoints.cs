using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Api.Modules.Identity.Application;

namespace SaaSPlatform.Api.Modules.Identity.Endpoints;

public static class IdentityAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapIdentityAdministrationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var users = endpoints.MapGroup("/api/v1/administration/users").WithTags("Identity administration");
        users.MapGet("/", async (string? search, int? page, int? pageSize, IIdentityAdministrationService service, CancellationToken ct) =>
            Results.Ok(await service.Users(search, page ?? 1, pageSize ?? 25, ct))).RequireAuthorization(TenantPermissions.UsersView);
        users.MapGet("/{id:guid}", async (Guid id, IIdentityAdministrationService service, CancellationToken ct) => Results.Ok(await service.User(id, ct))).RequireAuthorization(TenantPermissions.UsersView);
        users.MapPost("/", async ([FromBody] CreateTenantUserRequest request, IIdentityAdministrationService service, CancellationToken ct) => Results.Created("/api/v1/administration/users", await service.CreateUser(request, ct))).RequireAuthorization(TenantPermissions.UsersManage);
        users.MapPut("/{id:guid}", async (Guid id, [FromBody] UpdateTenantUserRequest request, IIdentityAdministrationService service, CancellationToken ct) => Results.Ok(await service.UpdateUser(id, request, ct))).RequireAuthorization(TenantPermissions.UsersManage);
        users.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] UserAccessStatusRequest request, ClaimsPrincipal actor, IIdentityAdministrationService service, CancellationToken ct) => Results.Ok(await service.SetAccess(id, request, actor, ct))).RequireAuthorization(TenantPermissions.MembershipsManage);
        users.MapPut("/{id:guid}/access", async (Guid id, [FromBody] MembershipAccessInput request, ClaimsPrincipal actor, IIdentityAdministrationService service, CancellationToken ct) => Results.Ok(await service.SetAccessAssignments(id, request, actor, ct))).RequireAuthorization(TenantPermissions.MembershipsManage);
        users.MapPost("/{id:guid}/reset-password", async (Guid id, [FromBody] AdminPasswordRequest request, IIdentityAdministrationService service, CancellationToken ct) => { await service.ResetPassword(id, request, ct); return Results.NoContent(); }).RequireAuthorization(TenantPermissions.UsersManage);

        var roles = endpoints.MapGroup("/api/v1/administration/roles").WithTags("Identity administration");
        roles.MapGet("/", async (IIdentityAdministrationService service, CancellationToken ct) => Results.Ok(await service.Roles(ct))).RequireAuthorization(TenantPermissions.RolesView);
        roles.MapPost("/", async ([FromBody] RoleInput request, ClaimsPrincipal actor, IIdentityAdministrationService service, CancellationToken ct) => Results.Created("/api/v1/administration/roles", await service.CreateRole(request, actor, ct))).RequireAuthorization(TenantPermissions.RolesManage);
        roles.MapPut("/{id:guid}", async (Guid id, [FromBody] RoleInput request, ClaimsPrincipal actor, IIdentityAdministrationService service, CancellationToken ct) => Results.Ok(await service.UpdateRole(id, request, actor, ct))).RequireAuthorization(TenantPermissions.RolesManage);
        roles.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] RoleStatusRequest request, ClaimsPrincipal actor, IIdentityAdministrationService service, CancellationToken ct) => Results.Ok(await service.SetRoleStatus(id, request, actor, ct))).RequireAuthorization(TenantPermissions.RolesManage);
        roles.MapGet("/permissions", async (IIdentityAdministrationService service, CancellationToken ct) => Results.Ok(await service.PermissionCatalog(ct))).RequireAuthorization(TenantPermissions.RolesView);
        return endpoints;
    }
}
