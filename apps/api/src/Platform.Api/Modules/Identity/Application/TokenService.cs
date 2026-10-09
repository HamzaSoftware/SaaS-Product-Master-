using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;
using SaaSPlatform.Api.Modules.Tenancy.Domain;

namespace SaaSPlatform.Api.Modules.Identity.Application;

public interface ITokenService
{
    (string Token, DateTimeOffset ExpiresAtUtc) Create(
        PlatformUser user,
        TenantMembership membership,
        IReadOnlyCollection<PermissionGrant> grants,
        Guid sessionId);
    (string Token, DateTimeOffset ExpiresAtUtc) CreatePlatform(PlatformUser user, Guid sessionId);
}

public sealed class TokenService(IOptions<AuthenticationOptions> options, TimeProvider timeProvider) : ITokenService
{
    public (string Token, DateTimeOffset ExpiresAtUtc) Create(
        PlatformUser user,
        TenantMembership membership,
        IReadOnlyCollection<PermissionGrant> grants,
        Guid sessionId)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(settings.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(TenantClaims.TenantId, membership.TenantId.ToString()),
            new(TenantClaims.MembershipId, membership.Id.ToString()),
            new(TenantClaims.AuthorizationVersion, membership.AuthorizationVersion.ToString()),
            new(TenantClaims.SessionId, sessionId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(grants.Select(x => x.Permission).Distinct(StringComparer.Ordinal)
            .Select(permission => new Claim(TenantClaims.Permission, permission)));
        claims.AddRange(grants.Select(grant => new Claim(TenantClaims.Grant, grant.ToClaimValue())));

        return WriteToken(settings, now, expires, claims);
    }

    public (string Token, DateTimeOffset ExpiresAtUtc) CreatePlatform(PlatformUser user, Guid sessionId)
    {
        var settings = options.Value;
        var now = timeProvider.GetUtcNow();
        var expires = now.AddMinutes(settings.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(TenantClaims.PlatformAdmin, bool.TrueString),
            new(TenantClaims.SessionId, sessionId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(PlatformPermissions.All.Select(permission => new Claim(TenantClaims.Permission, permission)));
        return WriteToken(settings, now, expires, claims);
    }

    private static (string Token, DateTimeOffset ExpiresAtUtc) WriteToken(
        AuthenticationOptions settings,
        DateTimeOffset now,
        DateTimeOffset expires,
        IEnumerable<Claim> claims)
    {

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, now.UtcDateTime, expires.UtcDateTime, credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
