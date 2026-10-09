using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Locations.Domain;
using SaaSPlatform.Api.Modules.Organization.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Domain;

namespace SaaSPlatform.Api.Modules.Tenancy.Application;

public sealed record CreateTenantRequest(string Name, string Slug, string? NameAr, string TimeZone,
    string DefaultLanguage, string? AdminEmail, string? AdminPassword, string? AdminDisplayName);
public sealed record TenantStatusRequest(bool IsActive, Guid Version);

public interface IPlatformTenantService
{
    Task<IReadOnlyList<Tenant>> List(CancellationToken ct);
    Task<Tenant> Create(CreateTenantRequest x, CancellationToken ct);
    Task<Tenant> SetStatus(Guid id, TenantStatusRequest x, CancellationToken ct);
}

public sealed class PlatformTenantService(
    PlatformDbContext database,
    ICurrentTenant currentTenant,
    UserManager<PlatformUser> users,
    IAuditWriter audit) : IPlatformTenantService
{
    public async Task<IReadOnlyList<Tenant>> List(CancellationToken ct) =>
        await database.Tenants.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);

    public async Task<Tenant> Create(CreateTenantRequest x, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(x.Name) || string.IsNullOrWhiteSpace(x.Slug))
            throw new ValidationException("Tenant name and slug are required.");
        var slug = x.Slug.Trim().ToLowerInvariant();
        if (await database.Tenants.AnyAsync(t => t.Slug == slug, ct)) throw new ConflictException("Tenant slug already exists.");
        if (!string.IsNullOrWhiteSpace(x.AdminEmail) && string.IsNullOrWhiteSpace(x.AdminPassword))
            throw new ValidationException("An initial administrator password is required when an email is supplied.");

        await using var transaction = await database.Database.BeginTransactionAsync(ct);
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = x.Name.Trim(), Slug = slug };
        database.Tenants.Add(tenant);
        await database.SaveChangesAsync(ct);
        currentTenant.Initialize(tenant.Id);

        database.CompanyProfiles.Add(new CompanyProfile { Id = Guid.NewGuid(), TenantId = tenant.Id,
            NameEn = tenant.Name, NameAr = Clean(x.NameAr), TimeZone = Required(x.TimeZone, "Time zone"),
            DefaultLanguage = Required(x.DefaultLanguage, "Default language").ToLowerInvariant() });
        var defaults = new[] { ("SITE", "Site", "موقع"), ("BUILDING", "Building", "مبنى"),
            ("FLOOR", "Floor", "طابق"), ("ROOM", "Room", "غرفة"), ("ZONE", "Zone", "منطقة") };
        database.LocationTypes.AddRange(defaults.Select((d, i) => new LocationType { Id = Guid.NewGuid(), TenantId = tenant.Id,
            Code = d.Item1, NameEn = d.Item2, NameAr = d.Item3, SortOrder = i * 10 }));

        var definitions = new List<PermissionDefinition>();
        foreach (var code in TenantPermissions.All)
        {
            var definition = await database.Permissions.SingleOrDefaultAsync(p => p.Code == code, ct);
            if (definition is null)
            {
                definition = new PermissionDefinition { Id = Guid.NewGuid(), Code = code, Module = code.Split('.')[0] };
                database.Permissions.Add(definition);
            }
            definitions.Add(definition);
        }
        var role = new TenantRole { Id = Guid.NewGuid(), TenantId = tenant.Id, Name = "Tenant Administrator",
            Description = "Full tenant administration access", IsSystem = true };
        database.TenantRoles.Add(role);
        database.RolePermissions.AddRange(definitions.Select(p => new RolePermissionGrant { TenantId = tenant.Id, RoleId = role.Id,
            PermissionId = p.Id, Role = role, Permission = p }));

        if (!string.IsNullOrWhiteSpace(x.AdminEmail))
        {
            var email = x.AdminEmail.Trim();
            var user = await users.FindByEmailAsync(email);
            if (user?.IsPlatformAdmin == true) throw new ConflictException("A platform administrator cannot be assigned as a tenant administrator.");
            if (user is null)
            {
                user = new PlatformUser { Id = Guid.NewGuid(), UserName = email, Email = email,
                    DisplayName = Clean(x.AdminDisplayName) ?? email, EmailConfirmed = true };
                var result = await users.CreateAsync(user, x.AdminPassword!);
                if (!result.Succeeded) throw new ValidationException(string.Join(" ", result.Errors.Select(e => e.Description)));
            }
            else if (!await users.CheckPasswordAsync(user, x.AdminPassword!))
            {
                throw new ValidationException("The supplied password does not match the existing user.");
            }
            var membership = new TenantMembership { Id = Guid.NewGuid(), TenantId = tenant.Id, UserId = user.Id };
            database.TenantMemberships.Add(membership);
            database.MembershipRoles.Add(new MembershipRole { Id = Guid.NewGuid(), TenantId = tenant.Id,
                MembershipId = membership.Id, RoleId = role.Id, ScopeKind = AccessScopeKind.Tenant });
        }

        audit.Add("Tenant.Created", nameof(Tenant), tenant.Id, new { tenant.Name, tenant.Slug });
        await database.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return tenant;
    }

    public async Task<Tenant> SetStatus(Guid id, TenantStatusRequest x, CancellationToken ct)
    {
        var tenant = await database.Tenants.SingleOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new ResourceNotFoundException("Tenant was not found.");
        if (x.Version == Guid.Empty || x.Version != tenant.Version) throw new ConflictException("The tenant was changed by another user.");
        currentTenant.Initialize(id); tenant.IsActive = x.IsActive; tenant.Version = Guid.NewGuid();
        audit.Add(x.IsActive ? "Tenant.Activated" : "Tenant.Deactivated", nameof(Tenant), id);
        await database.SaveChangesAsync(ct); return tenant;
    }

    private static string Required(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ValidationException($"{name} is required.") : value.Trim();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public static class PlatformAdminBootstrap
{
    public static async Task Run(IServiceProvider services, IConfiguration configuration, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<PlatformUser>>();
        var database = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var email = configuration["Bootstrap:Email"]?.Trim();
        var password = configuration["Bootstrap:Password"];
        var name = configuration["Bootstrap:DisplayName"]?.Trim() ?? "Platform Administrator";
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Bootstrap:Email and Bootstrap:Password are required.");
        var existingAdmin = await database.Users.SingleOrDefaultAsync(x => x.IsPlatformAdmin, ct);
        if (existingAdmin is not null)
        {
            if (string.Equals(existingAdmin.NormalizedEmail, users.NormalizeEmail(email), StringComparison.Ordinal)) return;
            throw new InvalidOperationException("A platform administrator already exists; bootstrap refused.");
        }
        var existing = await users.FindByEmailAsync(email);
        if (existing is not null) throw new InvalidOperationException("The bootstrap email already belongs to a non-platform user.");
        var user = new PlatformUser { Id = Guid.NewGuid(), UserName = email, Email = email, DisplayName = name,
            EmailConfirmed = true, IsPlatformAdmin = true };
        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
