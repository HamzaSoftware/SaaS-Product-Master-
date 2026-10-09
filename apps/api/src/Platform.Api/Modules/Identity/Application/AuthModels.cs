namespace SaaSPlatform.Api.Modules.Identity.Application;

public sealed record LoginRequest(string Email, string Password, Guid TenantId);
public sealed record PlatformLoginRequest(string Email, string Password);

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    UserSummary User,
    TenantSummary Tenant,
    IReadOnlyCollection<string> Permissions);

public sealed record RefreshResponse(string AccessToken, DateTimeOffset ExpiresAtUtc);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UserSummary(Guid Id, string DisplayName, string Email);

public sealed record TenantSummary(Guid Id, string Name, string Slug);

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";
    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string SigningKey { get; init; }
    public int AccessTokenMinutes { get; init; } = 10;
    public int SessionDays { get; init; } = 7;
}
