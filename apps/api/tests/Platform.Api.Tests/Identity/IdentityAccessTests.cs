using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Locations.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;
using SaaSPlatform.Api.Modules.Tenancy.Domain;

namespace SaaSPlatform.Api.Tests.Identity;

public sealed class IdentityAccessTests
{
    [Fact]
    public async Task RefreshRotatesSecretAndRejectsReusedSecret()
    {
        await using var f = await Fixture.Create();
        var session = await f.Sessions.CreateTenant(f.User, f.Membership, [], new DefaultHttpContext(), default);
        var refreshed = await f.Sessions.Refresh(session.RefreshSecret, new DefaultHttpContext(), default);
        Assert.NotEqual(session.RefreshSecret, refreshed.RefreshSecret);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sessions.Refresh(session.RefreshSecret, new DefaultHttpContext(), default));
    }

    [Fact]
    public async Task RevokedSessionCannotRefreshOrAuthorize()
    {
        await using var f = await Fixture.Create();
        var session = await f.Sessions.CreateTenant(f.User, f.Membership, [], new DefaultHttpContext(), default);
        var principal = Principal(session.AccessToken);
        await f.Sessions.RevokeOwn(principal, Guid.Parse(principal.FindFirstValue(TenantClaims.SessionId)!), "test", default);
        Assert.False(await f.Sessions.ValidatePrincipal(principal, default));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sessions.Refresh(session.RefreshSecret, new DefaultHttpContext(), default));
    }

    [Fact]
    public async Task InactiveMembershipAndChangedAuthorizationVersionInvalidateAccessImmediately()
    {
        await using var f = await Fixture.Create();
        var session = await f.Sessions.CreateTenant(f.User, f.Membership, [], new DefaultHttpContext(), default);
        var principal = Principal(session.AccessToken); Assert.True(await f.Sessions.ValidatePrincipal(principal, default));
        f.Membership.AuthorizationVersion = Guid.NewGuid(); await f.Database.SaveChangesAsync();
        Assert.False(await f.Sessions.ValidatePrincipal(principal, default));
        var renewed = await f.Sessions.Refresh(session.RefreshSecret, new DefaultHttpContext(), default);
        f.Membership.IsActive = false; await f.Database.SaveChangesAsync();
        Assert.False(await f.Sessions.ValidatePrincipal(Principal(renewed.AccessToken), default));
    }

    [Fact]
    public async Task InactiveUserCannotRefresh()
    {
        await using var f = await Fixture.Create();
        var session = await f.Sessions.CreateTenant(f.User, f.Membership, [], new DefaultHttpContext(), default);
        f.User.IsActive = false; await f.Database.SaveChangesAsync();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Sessions.Refresh(session.RefreshSecret, new DefaultHttpContext(), default));
    }

    [Fact]
    public async Task RoleCannotContainPermissionActorDoesNotHoldTenantWide()
    {
        await using var f = await Fixture.Create();
        var request = new RoleInput("Escalated", null, [TenantPermissions.SiteCreate], Guid.Empty);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Administration.CreateRole(request,
            Actor(TenantPermissions.RolesManage), default));
    }

    [Fact]
    public async Task ForeignRoleAndInvalidSiteScopeAreRejected()
    {
        await using var f = await Fixture.Create();
        var foreignRole = Guid.NewGuid();
        await Assert.ThrowsAsync<ValidationException>(() => f.Administration.SetAccessAssignments(f.User.Id,
            new MembershipAccessInput([new RoleAssignmentInput(foreignRole, AccessScopeKind.Tenant, null)], f.Membership.Version),
            Actor(TenantPermissions.UsersManage, TenantPermissions.RolesManage), default));
        var role = await f.Administration.CreateRole(new RoleInput("Viewer", null, [TenantPermissions.UsersView], Guid.Empty),
            Actor(TenantPermissions.UsersView, TenantPermissions.RolesManage), default);
        await Assert.ThrowsAsync<ValidationException>(() => f.Administration.SetAccessAssignments(f.User.Id,
            new MembershipAccessInput([new RoleAssignmentInput(role.Id, AccessScopeKind.Site, Guid.NewGuid())], f.Membership.Version),
            Actor(TenantPermissions.UsersView, TenantPermissions.RolesManage), default));
    }

    [Fact]
    public async Task StaleUserUpdateReturnsConflict()
    {
        await using var f = await Fixture.Create();
        await Assert.ThrowsAsync<ConflictException>(() => f.Administration.UpdateUser(f.User.Id,
            new UpdateTenantUserRequest("Changed", Guid.NewGuid()), default));
    }

    [Fact]
    public async Task FinalTenantAdministratorCannotDeactivateSelf()
    {
        await using var f = await Fixture.CreateAdmin();
        await Assert.ThrowsAsync<ConflictException>(() => f.Administration.SetAccess(f.User.Id,
            new UserAccessStatusRequest(false, f.Membership.Version), Actor(TenantPermissions.MembershipsManage), default));
    }

    [Fact]
    public async Task ActiveSiteScopeAssignmentSucceedsAndBumpsAuthorizationVersion()
    {
        await using var f = await Fixture.Create(); var before = f.Membership.AuthorizationVersion;
        var role = await f.Administration.CreateRole(new RoleInput("Site viewer", null, [TenantPermissions.SiteView], Guid.Empty),
            Actor(TenantPermissions.SiteView, TenantPermissions.RolesManage), default);
        var site = new Site { Id = Guid.NewGuid(), TenantId = f.Tenant.Id, Code = "MAIN", NameEn = "Main", TimeZone = "UTC" };
        f.Database.Sites.Add(site); await f.Database.SaveChangesAsync();
        var result = await f.Administration.SetAccessAssignments(f.User.Id,
            new MembershipAccessInput([new RoleAssignmentInput(role.Id, AccessScopeKind.Site, site.Id)], f.Membership.Version),
            Actor(TenantPermissions.SiteView, TenantPermissions.RolesManage), default);
        Assert.Single(result.Assignments); Assert.NotEqual(before, f.Membership.AuthorizationVersion);
    }

    private static ClaimsPrincipal Principal(string jwt) => new(new ClaimsIdentity(new JwtSecurityTokenHandler().ReadJwtToken(jwt).Claims, "test"));
    private static ClaimsPrincipal Actor(params string[] permissions) => new(new ClaimsIdentity(permissions.SelectMany(p => new[]
        { new Claim(TenantClaims.Permission, p), new Claim(TenantClaims.Grant, new PermissionGrant(p, AccessScopeKind.Tenant, null).ToClaimValue()) }), "test"));

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider provider;
        public PlatformDbContext Database { get; }
        public UserManager<PlatformUser> Users { get; }
        public SessionService Sessions { get; }
        public IdentityAdministrationService Administration { get; }
        public Tenant Tenant { get; }
        public PlatformUser User { get; }
        public TenantMembership Membership { get; }

        private Fixture(ServiceProvider provider, PlatformDbContext db, UserManager<PlatformUser> users, SessionService sessions,
            IdentityAdministrationService admin, Tenant tenant, PlatformUser user, TenantMembership membership)
        { this.provider = provider; Database = db; Users = users; Sessions = sessions; Administration = admin; Tenant = tenant; User = user; Membership = membership; }

        public static async Task<Fixture> Create(bool admin = false)
        {
            var tenantId = Guid.NewGuid(); var current = new Current(tenantId); var services = new ServiceCollection();
            services.AddSingleton<ICurrentTenant>(current); services.AddSingleton(TimeProvider.System);
            services.AddDataProtection();
            services.AddDbContext<PlatformDbContext>(x => x.UseInMemoryDatabase(Guid.NewGuid().ToString()));
            services.AddIdentityCore<PlatformUser>(o => { o.Password.RequiredLength = 12; o.User.RequireUniqueEmail = true; })
                .AddEntityFrameworkStores<PlatformDbContext>().AddDefaultTokenProviders();
            var provider = services.BuildServiceProvider(); var db = provider.GetRequiredService<PlatformDbContext>(); var users = provider.GetRequiredService<UserManager<PlatformUser>>();
            var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = $"tenant-{tenantId:N}" }; db.Tenants.Add(tenant);
            var user = new PlatformUser { Id = Guid.NewGuid(), UserName = "admin@example.com", Email = "admin@example.com", DisplayName = "Admin", EmailConfirmed = true };
            Assert.True((await users.CreateAsync(user, "Strong!Password123")).Succeeded);
            var membership = new TenantMembership { Id = Guid.NewGuid(), TenantId = tenantId, UserId = user.Id, User = user, Tenant = tenant }; db.TenantMemberships.Add(membership);
            foreach (var code in TenantPermissions.All) db.Permissions.Add(new PermissionDefinition { Id = Guid.NewGuid(), Code = code, Module = code.Split('.')[0] });
            await db.SaveChangesAsync();
            if (admin)
            {
                var defs = await db.Permissions.Where(x => x.Code == TenantPermissions.UsersManage || x.Code == TenantPermissions.RolesManage).ToListAsync();
                var role = new TenantRole { Id = Guid.NewGuid(), TenantId = tenantId, Name = "Admin", IsSystem = true }; db.TenantRoles.Add(role);
                foreach (var p in defs) { var grant = new RolePermissionGrant { TenantId = tenantId, RoleId = role.Id, PermissionId = p.Id, Role = role, Permission = p }; db.RolePermissions.Add(grant); role.Permissions.Add(grant); }
                var assignment = new MembershipRole { Id = Guid.NewGuid(), TenantId = tenantId, MembershipId = membership.Id, RoleId = role.Id, Role = role, Membership = membership, ScopeKind = AccessScopeKind.Tenant };
                db.MembershipRoles.Add(assignment); await db.SaveChangesAsync();
            }
            var authOptions = Options.Create(new AuthenticationOptions { Issuer = "test", Audience = "test", SigningKey = "a-test-signing-key-with-more-than-32-bytes", AccessTokenMinutes = 10, SessionDays = 7 });
            var tokenService = new TokenService(authOptions, TimeProvider.System); var sessions = new SessionService(db, tokenService, authOptions, TimeProvider.System);
            var adminService = new IdentityAdministrationService(db, current, users, sessions, new NoAudit());
            return new Fixture(provider, db, users, sessions, adminService, tenant, user, membership);
        }
        public static Task<Fixture> CreateAdmin() => Create(true);
        public async ValueTask DisposeAsync() { await Database.DisposeAsync(); await provider.DisposeAsync(); }
    }
    private sealed class Current(Guid id) : ICurrentTenant { public Guid? Id { get; private set; } = id; public void Initialize(Guid tenantId) => Id = tenantId; }
    private sealed class NoAudit : IAuditWriter { public void Add(string action, string entityType, Guid entityId, object? changes = null) { } }
}
