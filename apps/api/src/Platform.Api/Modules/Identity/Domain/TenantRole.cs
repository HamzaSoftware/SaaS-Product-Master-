using SaaSPlatform.Api.BuildingBlocks.Domain;

namespace SaaSPlatform.Api.Modules.Identity.Domain;

public sealed class TenantRole : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public ICollection<RolePermissionGrant> Permissions { get; set; } = [];
}

public enum AuthenticationContext
{
    Tenant = 0,
    Platform = 1
}

public sealed class UserSession
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? MembershipId { get; set; }
    public AuthenticationContext Context { get; set; }
    public required string RefreshTokenHash { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset LastUsedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevocationReason { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public PlatformUser? User { get; set; }
}

public sealed class PermissionDefinition
{
    public Guid Id { get; set; }
    public required string Code { get; set; }
    public required string Module { get; set; }
    public string? Description { get; set; }
}

public sealed class RolePermissionGrant : ITenantOwned
{
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public TenantRole? Role { get; set; }
    public PermissionDefinition? Permission { get; set; }
}

public enum AccessScopeKind
{
    Tenant = 0,
    Site = 1,
    Department = 2,
    Custom = 3
}

public sealed class MembershipRole : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid MembershipId { get; set; }
    public Guid RoleId { get; set; }
    public AccessScopeKind ScopeKind { get; set; } = AccessScopeKind.Tenant;
    public Guid? ScopeId { get; set; }
    public SaaSPlatform.Api.Modules.Tenancy.Domain.TenantMembership? Membership { get; set; }
    public TenantRole? Role { get; set; }
}
