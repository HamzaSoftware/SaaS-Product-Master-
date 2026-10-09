using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;
using SaaSPlatform.Api.Modules.Tenancy.Domain;
using SaaSPlatform.Api.Modules.Locations.Application;

namespace SaaSPlatform.Api.Modules.Identity.Application;

public sealed record CreateTenantUserRequest(string Email, string DisplayName, string? InitialPassword);
public sealed record UpdateTenantUserRequest(string DisplayName, Guid Version);
public sealed record UserAccessStatusRequest(bool IsActive, Guid Version);
public sealed record AdminPasswordRequest(string NewPassword);
public sealed record RoleInput(string Name, string? Description, IReadOnlyCollection<string> Permissions, Guid Version);
public sealed record RoleStatusRequest(bool IsActive, Guid Version);
public sealed record RoleAssignmentInput(Guid RoleId, AccessScopeKind ScopeKind, Guid? ScopeId);
public sealed record MembershipAccessInput(IReadOnlyCollection<RoleAssignmentInput> Assignments, Guid Version);
public sealed record TenantUserDto(Guid Id, string Email, string DisplayName, bool IsActive, Guid UserVersion,
    Guid MembershipId, bool MembershipActive, Guid MembershipVersion, DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<RoleAssignmentDto> Assignments);
public sealed record RoleAssignmentDto(Guid Id, Guid RoleId, string RoleName, AccessScopeKind ScopeKind, Guid? ScopeId);
public sealed record TenantRoleDto(Guid Id, string Name, string? Description, bool IsSystem, bool IsActive, Guid Version,
    IReadOnlyCollection<string> Permissions);

public interface IIdentityAdministrationService
{
    Task<PageResult<TenantUserDto>> Users(string? search, int page, int pageSize, CancellationToken ct);
    Task<TenantUserDto> User(Guid id, CancellationToken ct);
    Task<TenantUserDto> CreateUser(CreateTenantUserRequest request, CancellationToken ct);
    Task<TenantUserDto> UpdateUser(Guid id, UpdateTenantUserRequest request, CancellationToken ct);
    Task<TenantUserDto> SetAccess(Guid id, UserAccessStatusRequest request, ClaimsPrincipal actor, CancellationToken ct);
    Task ResetPassword(Guid id, AdminPasswordRequest request, CancellationToken ct);
    Task<TenantUserDto> SetAccessAssignments(Guid id, MembershipAccessInput request, ClaimsPrincipal actor, CancellationToken ct);
    Task<IReadOnlyList<TenantRoleDto>> Roles(CancellationToken ct);
    Task<TenantRoleDto> CreateRole(RoleInput request, ClaimsPrincipal actor, CancellationToken ct);
    Task<TenantRoleDto> UpdateRole(Guid id, RoleInput request, ClaimsPrincipal actor, CancellationToken ct);
    Task<TenantRoleDto> SetRoleStatus(Guid id, RoleStatusRequest request, ClaimsPrincipal actor, CancellationToken ct);
    Task<IReadOnlyList<object>> PermissionCatalog(CancellationToken ct);
}

public sealed class IdentityAdministrationService(PlatformDbContext database, ICurrentTenant currentTenant,
    UserManager<PlatformUser> users, ISessionService sessions, IAuditWriter audit) : IIdentityAdministrationService
{
    private Guid TenantId => currentTenant.Id ?? throw new InvalidOperationException("Tenant context is required.");

    public async Task<PageResult<TenantUserDto>> Users(string? search, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = database.TenantMemberships.AsNoTracking().Include(x => x.User).Include(x => x.Roles).ThenInclude(x => x.Role);
        var list = await query.OrderBy(x => x.User!.DisplayName).ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(search)) list = list.Where(x =>
            x.User!.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            (x.User.Email?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        return new PageResult<TenantUserDto>(list.Skip((page - 1) * pageSize).Take(pageSize).Select(Map).ToList(), page, pageSize, list.Count);
    }

    public async Task<TenantUserDto> User(Guid id, CancellationToken ct) => Map(await Membership(id, ct));

    public async Task<TenantUserDto> CreateUser(CreateTenantUserRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.DisplayName)) throw new ValidationException("Email and display name are required.");
        var email = request.Email.Trim(); var user = await users.FindByEmailAsync(email);
        if (user?.IsPlatformAdmin == true) throw new ConflictException("A platform administrator cannot be added through tenant administration.");
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(request.InitialPassword)) throw new ValidationException("An initial password is required until invitation email delivery is available.");
            user = new PlatformUser { Id = Guid.NewGuid(), UserName = email, Email = email, EmailConfirmed = true, DisplayName = request.DisplayName.Trim() };
            var result = await users.CreateAsync(user, request.InitialPassword); Ensure(result);
        }
        if (await database.TenantMemberships.AnyAsync(x => x.UserId == user.Id, ct)) throw new ConflictException("The user already belongs to this tenant.");
        var membership = new TenantMembership { Id = Guid.NewGuid(), TenantId = TenantId, UserId = user.Id, User = user };
        database.TenantMemberships.Add(membership); audit.Add("User.Created", nameof(PlatformUser), user.Id, new { membership.Id });
        await database.SaveChangesAsync(ct); return Map(membership);
    }

    public async Task<TenantUserDto> UpdateUser(Guid id, UpdateTenantUserRequest request, CancellationToken ct)
    {
        var membership = await Membership(id, ct); var user = membership.User!;
        Version(user.Version, request.Version); if (string.IsNullOrWhiteSpace(request.DisplayName)) throw new ValidationException("Display name is required.");
        user.DisplayName = request.DisplayName.Trim(); user.Version = Guid.NewGuid(); audit.Add("User.Updated", nameof(PlatformUser), user.Id);
        await database.SaveChangesAsync(ct); return Map(membership);
    }

    public async Task<TenantUserDto> SetAccess(Guid id, UserAccessStatusRequest request, ClaimsPrincipal actor, CancellationToken ct)
    {
        var membership = await Membership(id, ct); Version(membership.Version, request.Version);
        if (!request.IsActive && await IsAdministrator(membership.Id, ct)) await EnsureOtherAdministrator(membership.Id, ct);
        membership.IsActive = request.IsActive; Touch(membership);
        audit.Add(request.IsActive ? "Membership.Activated" : "Membership.Deactivated", nameof(TenantMembership), membership.Id);
        if (!request.IsActive) await sessions.RevokeAll(membership.UserId, null, "Membership deactivated", ct);
        await database.SaveChangesAsync(ct); return Map(membership);
    }

    public async Task ResetPassword(Guid id, AdminPasswordRequest request, CancellationToken ct)
    {
        var membership = await Membership(id, ct); var token = await users.GeneratePasswordResetTokenAsync(membership.User!);
        var result = await users.ResetPasswordAsync(membership.User!, token, request.NewPassword); Ensure(result);
        await sessions.RevokeAll(membership.UserId, null, "Administrator reset password", ct);
        audit.Add("User.PasswordReset", nameof(PlatformUser), membership.UserId); await database.SaveChangesAsync(ct);
    }

    public async Task<TenantUserDto> SetAccessAssignments(Guid id, MembershipAccessInput request, ClaimsPrincipal actor, CancellationToken ct)
    {
        var membership = await Membership(id, ct); Version(membership.Version, request.Version);
        var roles = await database.TenantRoles.Include(x => x.Permissions).ThenInclude(x => x.Permission)
            .Where(x => request.Assignments.Select(a => a.RoleId).Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
        if (roles.Count != request.Assignments.Select(x => x.RoleId).Distinct().Count()) throw new ValidationException("Every role must be active and belong to this tenant.");
        var actorPermissions = TenantWidePermissions(actor);
        var assignedPermissions = roles.Values.SelectMany(x => x.Permissions).Where(x => x.Permission is not null).Select(x => x.Permission!.Code).Distinct();
        if (assignedPermissions.Any(x => !actorPermissions.Contains(x))) throw new UnauthorizedAccessException("You cannot grant a permission you do not hold tenant-wide.");
        foreach (var assignment in request.Assignments)
        {
            if (assignment.ScopeKind is not (AccessScopeKind.Tenant or AccessScopeKind.Site)) throw new ValidationException("Only tenant and site scopes are supported.");
            if (assignment.ScopeKind == AccessScopeKind.Tenant && assignment.ScopeId is not null) throw new ValidationException("Tenant scope cannot include a site ID.");
            if (assignment.ScopeKind == AccessScopeKind.Site && (assignment.ScopeId is null ||
                !await database.Sites.AnyAsync(x => x.Id == assignment.ScopeId && x.IsActive, ct))) throw new ValidationException("The scoped site must be active and belong to this tenant.");
        }
        var unique = request.Assignments.Select(x => (x.RoleId, x.ScopeKind, x.ScopeId)).ToHashSet();
        if (unique.Count != request.Assignments.Count) throw new ConflictException("Duplicate role assignments are not allowed.");
        var wasAdmin = await IsAdministrator(membership.Id, ct);
        database.MembershipRoles.RemoveRange(membership.Roles);
        membership.Roles.Clear();
        foreach (var x in request.Assignments)
        {
            var assignment = new MembershipRole { Id = Guid.NewGuid(), TenantId = TenantId, MembershipId = membership.Id,
                RoleId = x.RoleId, ScopeKind = x.ScopeKind, ScopeId = x.ScopeId, Role = roles[x.RoleId], Membership = membership };
            database.MembershipRoles.Add(assignment);
        }
        if (wasAdmin && !WillBeAdministrator(request.Assignments, roles)) await EnsureOtherAdministrator(membership.Id, ct);
        Touch(membership); audit.Add("Membership.AccessChanged", nameof(TenantMembership), membership.Id,
            new { Assignments = request.Assignments.Select(x => new { x.RoleId, Scope = x.ScopeKind.ToString(), x.ScopeId }) });
        await database.SaveChangesAsync(ct); return Map(membership);
    }

    public async Task<IReadOnlyList<TenantRoleDto>> Roles(CancellationToken ct) => await database.TenantRoles.AsNoTracking()
        .Include(x => x.Permissions).ThenInclude(x => x.Permission).OrderBy(x => x.Name).Select(x => MapRole(x)).ToListAsync(ct);

    public async Task<TenantRoleDto> CreateRole(RoleInput request, ClaimsPrincipal actor, CancellationToken ct)
    {
        await ValidateRole(request, actor, null, ct); var role = new TenantRole { Id = Guid.NewGuid(), TenantId = TenantId,
            Name = request.Name.Trim(), Description = Clean(request.Description) }; database.TenantRoles.Add(role);
        await SetPermissions(role, request.Permissions, ct); audit.Add("Role.Created", nameof(TenantRole), role.Id); await database.SaveChangesAsync(ct); return MapRole(role);
    }

    public async Task<TenantRoleDto> UpdateRole(Guid id, RoleInput request, ClaimsPrincipal actor, CancellationToken ct)
    {
        var role = await Role(id, ct); Version(role.Version, request.Version); await ValidateRole(request, actor, id, ct);
        if (role.IsSystem) throw new ValidationException("Required system roles cannot be modified.");
        role.Name = request.Name.Trim(); role.Description = Clean(request.Description); role.Version = Guid.NewGuid();
        database.RolePermissions.RemoveRange(role.Permissions); role.Permissions.Clear(); await SetPermissions(role, request.Permissions, ct);
        await BumpMembershipsForRole(id, ct); audit.Add("Role.Updated", nameof(TenantRole), id); await database.SaveChangesAsync(ct); return MapRole(role);
    }

    public async Task<TenantRoleDto> SetRoleStatus(Guid id, RoleStatusRequest request, ClaimsPrincipal actor, CancellationToken ct)
    {
        var role = await Role(id, ct); Version(role.Version, request.Version);
        if (role.IsSystem && !request.IsActive) throw new ValidationException("Required system roles cannot be deactivated.");
        if (!request.IsActive && RoleIsAdministrator(role)) await EnsureAdministratorWithoutRole(id, ct);
        role.IsActive = request.IsActive; role.Version = Guid.NewGuid(); await BumpMembershipsForRole(id, ct);
        audit.Add(request.IsActive ? "Role.Activated" : "Role.Deactivated", nameof(TenantRole), id); await database.SaveChangesAsync(ct); return MapRole(role);
    }

    public async Task<IReadOnlyList<object>> PermissionCatalog(CancellationToken ct) => await database.Permissions.AsNoTracking()
        .OrderBy(x => x.Module).ThenBy(x => x.Code).Select(x => (object)new { x.Code, x.Module, x.Description }).ToListAsync(ct);

    private async Task<TenantMembership> Membership(Guid userId, CancellationToken ct) => await database.TenantMemberships
        .Include(x => x.User).Include(x => x.Roles).ThenInclude(x => x.Role).SingleOrDefaultAsync(x => x.UserId == userId, ct)
        ?? throw new ResourceNotFoundException("Tenant user was not found.");
    private async Task<TenantRole> Role(Guid id, CancellationToken ct) => await database.TenantRoles.Include(x => x.Permissions)
        .ThenInclude(x => x.Permission).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Role was not found.");
    private async Task ValidateRole(RoleInput x, ClaimsPrincipal actor, Guid? id, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(x.Name)) throw new ValidationException("Role name is required.");
        if (await database.TenantRoles.AnyAsync(r => r.Id != id && r.Name == x.Name.Trim(), ct)) throw new ConflictException("Role name already exists.");
        var allowed = TenantWidePermissions(actor); if (x.Permissions.Any(p => !allowed.Contains(p))) throw new UnauthorizedAccessException("You cannot assign permissions you do not hold tenant-wide.");
        var valid = await database.Permissions.CountAsync(p => x.Permissions.Contains(p.Code), ct);
        if (valid != x.Permissions.Distinct().Count()) throw new ValidationException("Roles may only use platform-defined permissions.");
    }
    private async Task SetPermissions(TenantRole role, IEnumerable<string> codes, CancellationToken ct)
    {
        var definitions = await database.Permissions.Where(x => codes.Contains(x.Code)).ToListAsync(ct);
        foreach (var p in definitions) role.Permissions.Add(new RolePermissionGrant { TenantId = TenantId, RoleId = role.Id, PermissionId = p.Id, Role = role, Permission = p });
    }
    private async Task BumpMembershipsForRole(Guid roleId, CancellationToken ct)
    { foreach (var m in await database.MembershipRoles.Where(x => x.RoleId == roleId).Select(x => x.Membership!).Distinct().ToListAsync(ct)) Touch(m); }
    private async Task<bool> IsAdministrator(Guid membershipId, CancellationToken ct) => await database.MembershipRoles
        .Where(x => x.MembershipId == membershipId && x.ScopeKind == AccessScopeKind.Tenant && x.Role!.IsActive)
        .SelectMany(x => x.Role!.Permissions).AnyAsync(x => x.Permission!.Code == TenantPermissions.UsersManage, ct)
        && await database.MembershipRoles.Where(x => x.MembershipId == membershipId && x.ScopeKind == AccessScopeKind.Tenant && x.Role!.IsActive)
            .SelectMany(x => x.Role!.Permissions).AnyAsync(x => x.Permission!.Code == TenantPermissions.RolesManage, ct);
    private async Task EnsureOtherAdministrator(Guid excluded, CancellationToken ct)
    {
        var candidates = await database.TenantMemberships.Where(x => x.Id != excluded && x.IsActive).Select(x => x.Id).ToListAsync(ct);
        foreach (var id in candidates) if (await IsAdministrator(id, ct)) return;
        throw new ConflictException("This operation would remove the final active tenant administrator.");
    }
    private async Task EnsureAdministratorWithoutRole(Guid excludedRole, CancellationToken ct)
    {
        var memberships = await database.TenantMemberships.Where(x => x.IsActive).Include(x => x.Roles)
            .ThenInclude(x => x.Role).ThenInclude(x => x!.Permissions).ThenInclude(x => x.Permission).ToListAsync(ct);
        if (memberships.Any(m => m.Roles.Where(r => r.RoleId != excludedRole && r.ScopeKind == AccessScopeKind.Tenant && r.Role?.IsActive == true)
            .SelectMany(r => r.Role!.Permissions).Select(p => p.Permission?.Code).ToHashSet()
            is var permissions && permissions.Contains(TenantPermissions.UsersManage) && permissions.Contains(TenantPermissions.RolesManage))) return;
        throw new ConflictException("This operation would remove the final active tenant administrator.");
    }
    private static bool RoleIsAdministrator(TenantRole role)
    { var permissions = role.Permissions.Select(x => x.Permission?.Code).ToHashSet(); return permissions.Contains(TenantPermissions.UsersManage) && permissions.Contains(TenantPermissions.RolesManage); }
    private static bool WillBeAdministrator(IEnumerable<RoleAssignmentInput> assignments, Dictionary<Guid, TenantRole> roles)
    { var permissions = assignments.Where(x => x.ScopeKind == AccessScopeKind.Tenant).SelectMany(x => roles[x.RoleId].Permissions).Select(x => x.Permission?.Code).ToHashSet(); return permissions.Contains(TenantPermissions.UsersManage) && permissions.Contains(TenantPermissions.RolesManage); }
    private static HashSet<string> TenantWidePermissions(ClaimsPrincipal actor) => actor.FindAll(TenantClaims.Grant).Select(x => x.Value.Split('|'))
        .Where(x => x.Length == 3 && x[1] == nameof(AccessScopeKind.Tenant)).Select(x => x[0]).ToHashSet(StringComparer.Ordinal);
    private static TenantUserDto Map(TenantMembership x) => new(x.UserId, x.User!.Email ?? "", x.User.DisplayName, x.User.IsActive,
        x.User.Version, x.Id, x.IsActive, x.Version, x.User.CreatedAtUtc, x.Roles.Select(r => new RoleAssignmentDto(r.Id, r.RoleId, r.Role?.Name ?? "", r.ScopeKind, r.ScopeId)).ToArray());
    private static TenantRoleDto MapRole(TenantRole x) => new(x.Id, x.Name, x.Description, x.IsSystem, x.IsActive, x.Version,
        x.Permissions.Where(p => p.Permission is not null).Select(p => p.Permission!.Code).Order().ToArray());
    private static void Touch(TenantMembership x) { x.Version = Guid.NewGuid(); x.AuthorizationVersion = Guid.NewGuid(); }
    private static void Version(Guid current, Guid supplied) { if (supplied == Guid.Empty || current != supplied) throw new ConflictException("The record changed. Refresh and try again."); }
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Ensure(IdentityResult result) { if (!result.Succeeded) throw new ValidationException(string.Join(" ", result.Errors.Select(x => x.Description))); }
}
