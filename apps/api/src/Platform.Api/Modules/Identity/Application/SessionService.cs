using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;
using SaaSPlatform.Api.Modules.Tenancy.Domain;

namespace SaaSPlatform.Api.Modules.Identity.Application;

public sealed record SessionToken(string AccessToken, DateTimeOffset ExpiresAtUtc, string RefreshSecret);
public sealed record SessionSummary(Guid Id, AuthenticationContext Context, Guid? TenantId, DateTimeOffset CreatedAtUtc,
    DateTimeOffset LastUsedAtUtc, DateTimeOffset ExpiresAtUtc, string? UserAgent, string? IpAddress, bool IsCurrent);

public interface ISessionService
{
    Task<SessionToken> CreateTenant(PlatformUser user, TenantMembership membership, IReadOnlyCollection<PermissionGrant> grants, HttpContext http, CancellationToken ct);
    Task<SessionToken> CreatePlatform(PlatformUser user, HttpContext http, CancellationToken ct);
    Task<SessionToken> Refresh(string cookie, HttpContext http, CancellationToken ct);
    Task<bool> ValidatePrincipal(ClaimsPrincipal principal, CancellationToken ct);
    Task<IReadOnlyList<SessionSummary>> ListOwn(ClaimsPrincipal principal, CancellationToken ct);
    Task RevokeOwn(ClaimsPrincipal principal, Guid sessionId, string reason, CancellationToken ct);
    Task RevokeAll(Guid userId, Guid? exceptSessionId, string reason, CancellationToken ct);
}

public sealed class SessionService(PlatformDbContext database, ITokenService tokens,
    IOptions<AuthenticationOptions> options, TimeProvider timeProvider) : ISessionService
{
    public async Task<SessionToken> CreateTenant(PlatformUser user, TenantMembership membership,
        IReadOnlyCollection<PermissionGrant> grants, HttpContext http, CancellationToken ct)
    {
        var (session, secret) = NewSession(user.Id, AuthenticationContext.Tenant, membership.TenantId, membership.Id, http);
        database.UserSessions.Add(session); await database.SaveChangesAsync(ct);
        var (token, expires) = tokens.Create(user, membership, grants, session.Id);
        return new SessionToken(token, expires, CookieValue(session.Id, secret));
    }

    public async Task<SessionToken> CreatePlatform(PlatformUser user, HttpContext http, CancellationToken ct)
    {
        var (session, secret) = NewSession(user.Id, AuthenticationContext.Platform, null, null, http);
        database.UserSessions.Add(session); await database.SaveChangesAsync(ct);
        var (token, expires) = tokens.CreatePlatform(user, session.Id);
        return new SessionToken(token, expires, CookieValue(session.Id, secret));
    }

    public async Task<SessionToken> Refresh(string cookie, HttpContext http, CancellationToken ct)
    {
        var (id, secret) = Parse(cookie); var now = timeProvider.GetUtcNow();
        var session = await database.UserSessions.Include(x => x.User).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (session?.User is null || session.RevokedAtUtc is not null || session.ExpiresAtUtc <= now || !session.User.IsActive ||
            !CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(session.RefreshTokenHash), Hash(secret)))
            throw new UnauthorizedAccessException("The session is invalid or expired.");
        var newSecret = RandomNumberGenerator.GetBytes(32); session.RefreshTokenHash = Convert.ToBase64String(Hash(newSecret));
        session.LastUsedAtUtc = now; session.Version = Guid.NewGuid();
        if (session.Context == AuthenticationContext.Platform)
        {
            if (!session.User.IsPlatformAdmin) throw new UnauthorizedAccessException();
            await database.SaveChangesAsync(ct); var platformToken = tokens.CreatePlatform(session.User, session.Id);
            return new SessionToken(platformToken.Token, platformToken.ExpiresAtUtc, CookieValue(session.Id, newSecret));
        }
        var membership = await Membership(session, ct); var grants = Grants(membership);
        await database.SaveChangesAsync(ct); var tenantToken = tokens.Create(session.User, membership, grants, session.Id);
        return new SessionToken(tenantToken.Token, tenantToken.ExpiresAtUtc, CookieValue(session.Id, newSecret));
    }

    public async Task<bool> ValidatePrincipal(ClaimsPrincipal principal, CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(TenantClaims.SessionId), out var sessionId) ||
            !Guid.TryParse(principal.FindFirstValue("sub"), out var userId)) return false;
        var now = timeProvider.GetUtcNow();
        var session = await database.UserSessions.AsNoTracking().Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == userId, ct);
        if (session?.User is null || !session.User.IsActive || session.RevokedAtUtc is not null || session.ExpiresAtUtc <= now) return false;
        if (session.Context == AuthenticationContext.Platform)
            return session.User.IsPlatformAdmin && principal.HasClaim(TenantClaims.PlatformAdmin, bool.TrueString);
        if (!Guid.TryParse(principal.FindFirstValue(TenantClaims.AuthorizationVersion), out var version)) return false;
        return await database.TenantMemberships.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(x => x.Id == session.MembershipId && x.UserId == userId && x.TenantId == session.TenantId &&
                x.IsActive && x.AuthorizationVersion == version && x.Tenant!.IsActive, ct);
    }

    public async Task<IReadOnlyList<SessionSummary>> ListOwn(ClaimsPrincipal principal, CancellationToken ct)
    {
        var userId = UserId(principal); var current = Guid.TryParse(principal.FindFirstValue(TenantClaims.SessionId), out var id) ? id : Guid.Empty;
        var now = timeProvider.GetUtcNow();
        return await database.UserSessions.AsNoTracking().Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .OrderByDescending(x => x.LastUsedAtUtc).Select(x => new SessionSummary(x.Id, x.Context, x.TenantId, x.CreatedAtUtc,
                x.LastUsedAtUtc, x.ExpiresAtUtc, x.UserAgent, x.IpAddress, x.Id == current)).ToListAsync(ct);
    }

    public async Task RevokeOwn(ClaimsPrincipal principal, Guid sessionId, string reason, CancellationToken ct)
    {
        var session = await database.UserSessions.SingleOrDefaultAsync(x => x.Id == sessionId && x.UserId == UserId(principal), ct)
            ?? throw new ResourceNotFoundException("Session was not found.");
        Revoke(session, reason); await database.SaveChangesAsync(ct);
    }

    public async Task RevokeAll(Guid userId, Guid? exceptSessionId, string reason, CancellationToken ct)
    {
        var sessions = await database.UserSessions.Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.Id != exceptSessionId).ToListAsync(ct);
        foreach (var session in sessions) Revoke(session, reason);
        await database.SaveChangesAsync(ct);
    }

    private async Task<TenantMembership> Membership(UserSession session, CancellationToken ct) =>
        await database.TenantMemberships.IgnoreQueryFilters().Include(x => x.Tenant).Include(x => x.Roles)
            .ThenInclude(x => x.Role).ThenInclude(x => x!.Permissions).ThenInclude(x => x.Permission)
            .SingleOrDefaultAsync(x => x.Id == session.MembershipId && x.TenantId == session.TenantId && x.IsActive && x.Tenant!.IsActive, ct)
        ?? throw new UnauthorizedAccessException("The membership is inactive.");
    private static PermissionGrant[] Grants(TenantMembership m) => m.Roles.Where(x => x.Role?.IsActive == true)
        .SelectMany(x => x.Role!.Permissions.Where(p => p.Permission is not null).Select(p => new PermissionGrant(p.Permission!.Code, x.ScopeKind, x.ScopeId))).Distinct().ToArray();
    private (UserSession Session, byte[] Secret) NewSession(Guid userId, AuthenticationContext context, Guid? tenantId, Guid? membershipId, HttpContext http)
    {
        var now = timeProvider.GetUtcNow(); var secret = RandomNumberGenerator.GetBytes(32);
        return (new UserSession { Id = Guid.NewGuid(), UserId = userId, Context = context, TenantId = tenantId,
            MembershipId = membershipId, RefreshTokenHash = Convert.ToBase64String(Hash(secret)), CreatedAtUtc = now,
            LastUsedAtUtc = now, ExpiresAtUtc = now.AddDays(options.Value.SessionDays),
            UserAgent = http.Request.Headers.UserAgent.ToString()[..Math.Min(500, http.Request.Headers.UserAgent.ToString().Length)],
            IpAddress = http.Connection.RemoteIpAddress?.ToString() }, secret);
    }
    private void Revoke(UserSession session, string reason) { if (session.RevokedAtUtc is null) { session.RevokedAtUtc = timeProvider.GetUtcNow(); session.RevocationReason = reason; session.Version = Guid.NewGuid(); } }
    private static byte[] Hash(byte[] secret) => SHA256.HashData(secret);
    private static string CookieValue(Guid id, byte[] secret) => $"{id:N}.{Convert.ToBase64String(secret).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";
    private static (Guid Id, byte[] Secret) Parse(string cookie)
    {
        var parts = cookie.Split('.', 2); if (parts.Length != 2 || !Guid.TryParseExact(parts[0], "N", out var id)) throw new UnauthorizedAccessException();
        var value = parts[1].Replace('-', '+').Replace('_', '/'); value += new string('=', (4 - value.Length % 4) % 4);
        try { return (id, Convert.FromBase64String(value)); } catch (FormatException) { throw new UnauthorizedAccessException(); }
    }
    private static Guid UserId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue("sub"), out var id) ? id : throw new UnauthorizedAccessException();
}
