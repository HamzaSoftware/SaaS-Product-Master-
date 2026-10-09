using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using System.Security.Claims;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Identity.Endpoints;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/v1/auth/login", LoginAsync)
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .WithTags("Identity")
            .WithName("Login")
            .Produces<LoginResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        endpoints.MapPost("/api/v1/auth/platform-login", PlatformLoginAsync)
            .AllowAnonymous().RequireRateLimiting("auth").WithTags("Identity");

        endpoints.MapPost("/api/v1/auth/refresh", RefreshAsync).AllowAnonymous().RequireRateLimiting("auth").WithTags("Identity");
        endpoints.MapPost("/api/v1/auth/logout", LogoutAsync).RequireAuthorization().WithTags("Identity");
        endpoints.MapPost("/api/v1/auth/logout-all", LogoutAllAsync).RequireAuthorization().WithTags("Identity");
        endpoints.MapGet("/api/v1/auth/sessions", ListSessionsAsync).RequireAuthorization().WithTags("Identity");
        endpoints.MapDelete("/api/v1/auth/sessions/{id:guid}", RevokeSessionAsync).RequireAuthorization().WithTags("Identity");
        endpoints.MapPost("/api/v1/auth/change-password", ChangePasswordAsync).RequireAuthorization().WithTags("Identity");

        return endpoints;
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginRequest request,
        UserManager<PlatformUser> userManager,
        PlatformDbContext database,
        ISessionService sessions,
        HttpContext http,
        ICurrentTenant currentTenant,
        IAuditWriter audit,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || request.TenantId == Guid.Empty)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["credentials"] = ["Email, password, and tenant ID are required."]
            });
        }

        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || await userManager.IsLockedOutAsync(user) || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            if (user is not null) await userManager.AccessFailedAsync(user);
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials.");
        }
        await userManager.ResetAccessFailedCountAsync(user);

        var membership = await database.TenantMemberships
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Roles)
                .ThenInclude(x => x.Role)
                    .ThenInclude(x => x!.Permissions)
                        .ThenInclude(x => x.Permission)
            .SingleOrDefaultAsync(
                x => x.TenantId == request.TenantId && x.UserId == user.Id && x.IsActive,
                cancellationToken);

        if (membership?.Tenant is null || !membership.Tenant.IsActive)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials.");
        }

        var grants = membership.Roles
            .SelectMany(membershipRole => (membershipRole.Role?.Permissions ?? [])
                .Where(rolePermission => rolePermission.Permission is not null)
                .Select(rolePermission => new PermissionGrant(
                    rolePermission.Permission!.Code,
                    membershipRole.ScopeKind,
                    membershipRole.ScopeId)))
            .Distinct()
            .ToArray();
        currentTenant.Initialize(membership.TenantId);
        audit.Add("Authentication.Login", nameof(PlatformUser), user.Id, new { membership.Id });
        var session = await sessions.CreateTenant(user, membership, grants, http, cancellationToken);
        SetRefreshCookie(http, session.RefreshSecret);
        var permissions = grants.Select(x => x.Permission).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();

        return TypedResults.Ok(new LoginResponse(
            session.AccessToken,
            session.ExpiresAtUtc,
            new UserSummary(user.Id, user.DisplayName, user.Email ?? string.Empty),
            new TenantSummary(membership.Tenant.Id, membership.Tenant.Name, membership.Tenant.Slug),
            permissions));
    }

    private static async Task<IResult> PlatformLoginAsync(
        [FromBody] PlatformLoginRequest request,
        UserManager<PlatformUser> userManager,
        ISessionService sessions,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials.");
        var user = await userManager.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive || !user.IsPlatformAdmin || await userManager.IsLockedOutAsync(user) || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            if (user is not null) await userManager.AccessFailedAsync(user);
            return TypedResults.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials.");
        }
        await userManager.ResetAccessFailedCountAsync(user);
        var session = await sessions.CreatePlatform(user, http, cancellationToken); SetRefreshCookie(http, session.RefreshSecret);
        return TypedResults.Ok(new { session.AccessToken, session.ExpiresAtUtc });
    }

    private static async Task<IResult> RefreshAsync(HttpContext http, ISessionService sessions, CancellationToken ct)
    {
        if (!http.Request.Cookies.TryGetValue(GetRefreshCookieName(http), out var cookie)) return Results.Unauthorized();
        try { var session = await sessions.Refresh(cookie, http, ct); SetRefreshCookie(http, session.RefreshSecret); return Results.Ok(new RefreshResponse(session.AccessToken, session.ExpiresAtUtc)); }
        catch (UnauthorizedAccessException) { ClearRefreshCookie(http); return Results.Unauthorized(); }
    }

    private static async Task<IResult> LogoutAsync(ClaimsPrincipal user, ISessionService sessions, IAuditWriter audit, HttpContext http, CancellationToken ct)
    {
        if (Guid.TryParse(user.FindFirstValue(TenantClaims.SessionId), out var id)) { if (user.GetTenantId() is not null) audit.Add("Session.Logout", nameof(UserSession), id); await sessions.RevokeOwn(user, id, "User logout", ct); }
        ClearRefreshCookie(http); return Results.NoContent();
    }

    private static async Task<IResult> LogoutAllAsync(ClaimsPrincipal user, ISessionService sessions, IAuditWriter audit, HttpContext http, CancellationToken ct)
    {
        var userId = Guid.Parse(user.FindFirstValue("sub")!); if (user.GetTenantId() is not null) audit.Add("Session.LogoutAll", nameof(PlatformUser), userId); await sessions.RevokeAll(userId, null, "User requested logout all", ct);
        ClearRefreshCookie(http); return Results.NoContent();
    }

    private static async Task<IResult> ListSessionsAsync(ClaimsPrincipal user, ISessionService sessions, CancellationToken ct) =>
        Results.Ok(await sessions.ListOwn(user, ct));

    private static async Task<IResult> RevokeSessionAsync(Guid id, ClaimsPrincipal user, ISessionService sessions, IAuditWriter audit, CancellationToken ct)
    { if (user.GetTenantId() is not null) audit.Add("Session.Revoked", nameof(UserSession), id); await sessions.RevokeOwn(user, id, "User revoked session", ct); return Results.NoContent(); }

    private static async Task<IResult> ChangePasswordAsync([FromBody] ChangePasswordRequest request, ClaimsPrincipal principal,
        UserManager<PlatformUser> users, ISessionService sessions, IAuditWriter audit, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(principal.FindFirstValue("sub")!) ?? throw new UnauthorizedAccessException();
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded) return Results.ValidationProblem(new Dictionary<string, string[]> { ["password"] = result.Errors.Select(x => x.Description).ToArray() });
        var current = Guid.TryParse(principal.FindFirstValue(TenantClaims.SessionId), out var id) ? id : (Guid?)null;
        if (principal.GetTenantId() is not null) audit.Add("User.PasswordChanged", nameof(PlatformUser), user.Id);
        await sessions.RevokeAll(user.Id, current, "Password changed", ct);
        return Results.NoContent();
    }

    private static string GetRefreshCookieName(HttpContext http) =>
        http.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
            ? "platform_refresh"
            : "__Host-platform_refresh";

    private static void SetRefreshCookie(HttpContext http, string value) => http.Response.Cookies.Append(GetRefreshCookieName(http), value,
        new CookieOptions { HttpOnly = true, Secure = !http.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(),
            SameSite = SameSiteMode.Lax, Path = "/", MaxAge = TimeSpan.FromDays(7), IsEssential = true });
    private static void ClearRefreshCookie(HttpContext http) => http.Response.Cookies.Delete(GetRefreshCookieName(http),
        new CookieOptions { Secure = !http.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment(), Path = "/" });
}
