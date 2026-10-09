using SaaSPlatform.Api.BuildingBlocks.Domain;

namespace SaaSPlatform.Api.Modules.Organization.Domain;

public sealed class CompanyProfile : ITenantOwned, IHasTimestamps
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string? NameAr { get; set; }
    public required string NameEn { get; set; }
    public string? LegalNameAr { get; set; }
    public string? LegalNameEn { get; set; }
    public string? LogoReference { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Country { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
    public required string TimeZone { get; set; } = "UTC";
    public required string DefaultLanguage { get; set; } = "en";
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
