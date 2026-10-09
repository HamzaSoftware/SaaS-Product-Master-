using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Locations.Domain;
using SaaSPlatform.Api.Modules.Organization.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Locations.Application;

public sealed record CompanyInput(string NameEn, string? NameAr, string? LegalNameEn, string? LegalNameAr,
    string? LogoReference, string? Email, string? Phone, string? Website, string? Country, string? City,
    string? Address, string TimeZone, string DefaultLanguage, Guid Version);
public sealed record SiteInput(string Code, string NameEn, string? NameAr, string? Description, string? Address,
    string? City, string? Country, string TimeZone, decimal? Latitude, decimal? Longitude, Guid Version);
public sealed record LocationTypeInput(string Code, string NameEn, string? NameAr, string? Description,
    int SortOrder, Guid Version);
public sealed record LocationInput(Guid SiteId, Guid? ParentLocationId, Guid LocationTypeId, string Code,
    string NameEn, string? NameAr, string? Description, int SortOrder, Guid Version);
public sealed record MoveLocationInput(Guid? ParentLocationId, int SortOrder, Guid Version);
public sealed record StatusInput(bool IsActive, Guid Version);
public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);

public interface ITenantAdministrationService
{
    Task<CompanyProfile> GetCompany(CancellationToken ct);
    Task<CompanyProfile> UpdateCompany(CompanyInput x, CancellationToken ct);
    Task<PageResult<Site>> GetSites(ClaimsPrincipal user, string? search, int page, int pageSize, CancellationToken ct);
    Task<Site> CreateSite(ClaimsPrincipal user, SiteInput x, CancellationToken ct);
    Task<Site> UpdateSite(ClaimsPrincipal user, Guid id, SiteInput x, CancellationToken ct);
    Task<Site> SetSiteStatus(ClaimsPrincipal user, Guid id, StatusInput x, CancellationToken ct);
    Task<IReadOnlyList<LocationType>> GetLocationTypes(CancellationToken ct);
    Task<LocationType> CreateLocationType(LocationTypeInput x, CancellationToken ct);
    Task<LocationType> UpdateLocationType(Guid id, LocationTypeInput x, CancellationToken ct);
    Task<LocationType> SetLocationTypeStatus(Guid id, StatusInput x, CancellationToken ct);
    Task<IReadOnlyList<LocationNode>> GetLocations(ClaimsPrincipal user, Guid siteId, CancellationToken ct);
    Task<LocationNode> CreateLocation(ClaimsPrincipal user, LocationInput x, CancellationToken ct);
    Task<LocationNode> UpdateLocation(ClaimsPrincipal user, Guid id, LocationInput x, CancellationToken ct);
    Task<LocationNode> MoveLocation(ClaimsPrincipal user, Guid id, MoveLocationInput x, CancellationToken ct);
    Task<LocationNode> SetLocationStatus(ClaimsPrincipal user, Guid id, StatusInput x, CancellationToken ct);
}

public sealed class TenantAdministrationService(
    PlatformDbContext database,
    ICurrentTenant currentTenant,
    ISiteScopeAuthorizer scopes,
    IAuditWriter audit) : ITenantAdministrationService
{
    private Guid TenantId => currentTenant.Id ?? throw new InvalidOperationException("Tenant context is required.");

    public async Task<CompanyProfile> GetCompany(CancellationToken ct) =>
        await database.CompanyProfiles.AsNoTracking().SingleOrDefaultAsync(ct)
        ?? throw new ResourceNotFoundException("Company profile was not found.");

    public async Task<CompanyProfile> UpdateCompany(CompanyInput x, CancellationToken ct)
    {
        Required(x.NameEn, "English company name");
        var entity = await database.CompanyProfiles.SingleOrDefaultAsync(ct)
            ?? throw new ResourceNotFoundException("Company profile was not found.");
        Version(entity.Version, x.Version);
        entity.NameEn = x.NameEn.Trim(); entity.NameAr = Clean(x.NameAr);
        entity.LegalNameEn = Clean(x.LegalNameEn); entity.LegalNameAr = Clean(x.LegalNameAr);
        entity.LogoReference = Clean(x.LogoReference); entity.Email = Clean(x.Email); entity.Phone = Clean(x.Phone);
        entity.Website = Clean(x.Website); entity.Country = Clean(x.Country); entity.City = Clean(x.City);
        entity.Address = Clean(x.Address); entity.TimeZone = Required(x.TimeZone, "Time zone");
        entity.DefaultLanguage = Required(x.DefaultLanguage, "Default language").ToLowerInvariant();
        entity.Version = Guid.NewGuid();
        audit.Add("Company.Updated", nameof(CompanyProfile), entity.Id);
        await Save(ct); return entity;
    }

    public async Task<PageResult<Site>> GetSites(ClaimsPrincipal user, string? search, int page, int pageSize, CancellationToken ct)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 1, 100);
        var accessible = await database.Sites.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            accessible = accessible.Where(x => x.Code.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                x.NameEn.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (x.NameAr?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
        }
        accessible = accessible.Where(x => scopes.HasAccess(user, TenantPermissions.SiteView, x.Id)).ToList();
        return new PageResult<Site>(accessible.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, accessible.Count);
    }

    public async Task<Site> CreateSite(ClaimsPrincipal user, SiteInput x, CancellationToken ct)
    {
        RequireTenant(user, TenantPermissions.SiteCreate);
        ValidateSite(x); var code = Code(x.Code);
        if (await database.Sites.AnyAsync(s => s.Code == code, ct)) throw new ConflictException("Site code already exists.");
        var entity = new Site { Id = Guid.NewGuid(), TenantId = TenantId, Code = code, NameEn = x.NameEn.Trim(),
            NameAr = Clean(x.NameAr), Description = Clean(x.Description), Address = Clean(x.Address), City = Clean(x.City),
            Country = Clean(x.Country), TimeZone = x.TimeZone.Trim(), Latitude = x.Latitude, Longitude = x.Longitude };
        database.Sites.Add(entity); audit.Add("Site.Created", nameof(Site), entity.Id, new { entity.Code });
        await Save(ct); return entity;
    }

    public async Task<Site> UpdateSite(ClaimsPrincipal user, Guid id, SiteInput x, CancellationToken ct)
    {
        var entity = await Site(id, ct); RequireSite(user, TenantPermissions.SiteEdit, entity.Id); Version(entity.Version, x.Version);
        ValidateSite(x); var code = Code(x.Code);
        if (await database.Sites.AnyAsync(s => s.Id != id && s.Code == code, ct)) throw new ConflictException("Site code already exists.");
        entity.Code = code; entity.NameEn = x.NameEn.Trim(); entity.NameAr = Clean(x.NameAr); entity.Description = Clean(x.Description);
        entity.Address = Clean(x.Address); entity.City = Clean(x.City); entity.Country = Clean(x.Country); entity.TimeZone = x.TimeZone.Trim();
        entity.Latitude = x.Latitude; entity.Longitude = x.Longitude; entity.Version = Guid.NewGuid();
        audit.Add("Site.Updated", nameof(Site), id); await Save(ct); return entity;
    }

    public async Task<Site> SetSiteStatus(ClaimsPrincipal user, Guid id, StatusInput x, CancellationToken ct)
    {
        var entity = await Site(id, ct); RequireSite(user, x.IsActive ? TenantPermissions.SiteActivate : TenantPermissions.SiteDeactivate, id);
        Version(entity.Version, x.Version); entity.IsActive = x.IsActive; entity.Version = Guid.NewGuid();
        audit.Add(x.IsActive ? "Site.Activated" : "Site.Deactivated", nameof(Site), id); await Save(ct); return entity;
    }

    public async Task<IReadOnlyList<LocationType>> GetLocationTypes(CancellationToken ct) =>
        await database.LocationTypes.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToListAsync(ct);

    public async Task<LocationType> CreateLocationType(LocationTypeInput x, CancellationToken ct)
    {
        ValidateType(x); var code = Code(x.Code);
        if (await database.LocationTypes.AnyAsync(t => t.Code == code, ct)) throw new ConflictException("Location type code already exists.");
        var entity = new LocationType { Id = Guid.NewGuid(), TenantId = TenantId, Code = code, NameEn = x.NameEn.Trim(),
            NameAr = Clean(x.NameAr), Description = Clean(x.Description), SortOrder = x.SortOrder };
        database.LocationTypes.Add(entity); audit.Add("LocationType.Created", nameof(LocationType), entity.Id, new { entity.Code });
        await Save(ct); return entity;
    }

    public async Task<LocationType> UpdateLocationType(Guid id, LocationTypeInput x, CancellationToken ct)
    {
        var entity = await database.LocationTypes.SingleOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new ResourceNotFoundException("Location type was not found.");
        Version(entity.Version, x.Version); ValidateType(x); var code = Code(x.Code);
        if (await database.LocationTypes.AnyAsync(t => t.Id != id && t.Code == code, ct)) throw new ConflictException("Location type code already exists.");
        entity.Code = code; entity.NameEn = x.NameEn.Trim(); entity.NameAr = Clean(x.NameAr);
        entity.Description = Clean(x.Description); entity.SortOrder = x.SortOrder; entity.Version = Guid.NewGuid();
        audit.Add("LocationType.Updated", nameof(LocationType), id); await Save(ct); return entity;
    }

    public async Task<LocationType> SetLocationTypeStatus(Guid id, StatusInput x, CancellationToken ct)
    {
        var entity = await database.LocationTypes.SingleOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new ResourceNotFoundException("Location type was not found.");
        Version(entity.Version, x.Version);
        if (!x.IsActive && await database.LocationNodes.AnyAsync(n => n.LocationTypeId == id && n.IsActive, ct))
            throw new ConflictException("An active location uses this location type.");
        entity.IsActive = x.IsActive; entity.Version = Guid.NewGuid();
        audit.Add(x.IsActive ? "LocationType.Activated" : "LocationType.Deactivated", nameof(LocationType), id);
        await Save(ct); return entity;
    }

    public async Task<IReadOnlyList<LocationNode>> GetLocations(ClaimsPrincipal user, Guid siteId, CancellationToken ct)
    {
        RequireSite(user, TenantPermissions.LocationView, siteId);
        _ = await Site(siteId, ct);
        return await database.LocationNodes.AsNoTracking().Where(x => x.SiteId == siteId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Code).ToListAsync(ct);
    }

    public async Task<LocationNode> CreateLocation(ClaimsPrincipal user, LocationInput x, CancellationToken ct)
    {
        RequireSite(user, TenantPermissions.LocationCreate, x.SiteId); await ValidateLocation(x, null, ct);
        var entity = new LocationNode { Id = Guid.NewGuid(), TenantId = TenantId, SiteId = x.SiteId,
            ParentLocationId = x.ParentLocationId, LocationTypeId = x.LocationTypeId, Code = Code(x.Code),
            NameEn = x.NameEn.Trim(), NameAr = Clean(x.NameAr), Description = Clean(x.Description), SortOrder = x.SortOrder };
        database.LocationNodes.Add(entity); audit.Add("Location.Created", nameof(LocationNode), entity.Id, new { entity.Code, entity.SiteId });
        await Save(ct); return entity;
    }

    public async Task<LocationNode> UpdateLocation(ClaimsPrincipal user, Guid id, LocationInput x, CancellationToken ct)
    {
        var entity = await Location(id, ct); RequireSite(user, TenantPermissions.LocationEdit, entity.SiteId);
        if (x.SiteId != entity.SiteId) throw new ConflictException("A location cannot be moved to another site.");
        Version(entity.Version, x.Version); await ValidateLocation(x, id, ct); await EnsureNoCycle(id, x.ParentLocationId, entity.SiteId, ct);
        entity.ParentLocationId = x.ParentLocationId; entity.LocationTypeId = x.LocationTypeId; entity.Code = Code(x.Code);
        entity.NameEn = x.NameEn.Trim(); entity.NameAr = Clean(x.NameAr); entity.Description = Clean(x.Description);
        entity.SortOrder = x.SortOrder; entity.Version = Guid.NewGuid();
        audit.Add("Location.Updated", nameof(LocationNode), id); await Save(ct); return entity;
    }

    public async Task<LocationNode> MoveLocation(ClaimsPrincipal user, Guid id, MoveLocationInput x, CancellationToken ct)
    {
        var entity = await Location(id, ct); RequireSite(user, TenantPermissions.LocationMove, entity.SiteId);
        Version(entity.Version, x.Version); await EnsureParent(entity.SiteId, x.ParentLocationId, ct); await EnsureNoCycle(id, x.ParentLocationId, entity.SiteId, ct);
        entity.ParentLocationId = x.ParentLocationId; entity.SortOrder = x.SortOrder; entity.Version = Guid.NewGuid();
        audit.Add("Location.Moved", nameof(LocationNode), id, new { x.ParentLocationId, x.SortOrder }); await Save(ct); return entity;
    }

    public async Task<LocationNode> SetLocationStatus(ClaimsPrincipal user, Guid id, StatusInput x, CancellationToken ct)
    {
        var entity = await Location(id, ct); RequireSite(user, x.IsActive ? TenantPermissions.LocationActivate : TenantPermissions.LocationDeactivate, entity.SiteId);
        Version(entity.Version, x.Version);
        if (!x.IsActive && await HasActiveDescendant(id, entity.SiteId, ct))
            throw new ConflictException("Deactivate active descendants before deactivating this location.");
        entity.IsActive = x.IsActive; entity.Version = Guid.NewGuid();
        audit.Add(x.IsActive ? "Location.Activated" : "Location.Deactivated", nameof(LocationNode), id); await Save(ct); return entity;
    }

    private async Task ValidateLocation(LocationInput x, Guid? id, CancellationToken ct)
    {
        Required(x.NameEn, "English location name"); var code = Code(x.Code); _ = await Site(x.SiteId, ct);
        if (!await database.LocationTypes.AnyAsync(t => t.Id == x.LocationTypeId && t.IsActive, ct))
            throw new ValidationException("An active location type in this tenant is required.");
        await EnsureParent(x.SiteId, x.ParentLocationId, ct);
        if (await database.LocationNodes.AnyAsync(n => n.SiteId == x.SiteId && n.Code == code && n.Id != id, ct))
            throw new ConflictException("Location code already exists in this site.");
    }

    private async Task EnsureParent(Guid siteId, Guid? parentId, CancellationToken ct)
    {
        if (parentId is null) return;
        if (!await database.LocationNodes.AnyAsync(n => n.Id == parentId && n.SiteId == siteId, ct))
            throw new ValidationException("Parent location must belong to the same tenant and site.");
    }

    private async Task EnsureNoCycle(Guid id, Guid? parentId, Guid siteId, CancellationToken ct)
    {
        if (parentId == id) throw new ValidationException("A location cannot be its own parent.");
        var nodes = await database.LocationNodes.AsNoTracking().Where(n => n.SiteId == siteId)
            .Select(n => new { n.Id, n.ParentLocationId }).ToListAsync(ct);
        var parents = nodes.ToDictionary(n => n.Id, n => n.ParentLocationId);
        var cursor = parentId;
        while (cursor is not null)
        {
            if (cursor == id) throw new ValidationException("The move would create a location cycle.");
            cursor = parents.GetValueOrDefault(cursor.Value);
        }
    }

    private async Task<bool> HasActiveDescendant(Guid id, Guid siteId, CancellationToken ct)
    {
        var nodes = await database.LocationNodes.AsNoTracking().Where(n => n.SiteId == siteId)
            .Select(n => new { n.Id, n.ParentLocationId, n.IsActive }).ToListAsync(ct);
        var descendants = new HashSet<Guid> { id };
        var changed = true;
        while (changed)
        {
            changed = false;
            foreach (var node in nodes.Where(n => n.ParentLocationId is not null && descendants.Contains(n.ParentLocationId.Value)))
                changed |= descendants.Add(node.Id);
        }
        return nodes.Any(n => n.Id != id && descendants.Contains(n.Id) && n.IsActive);
    }

    private async Task<Site> Site(Guid id, CancellationToken ct) => await database.Sites.SingleOrDefaultAsync(x => x.Id == id, ct)
        ?? throw new ResourceNotFoundException("Site was not found.");
    private async Task<LocationNode> Location(Guid id, CancellationToken ct) => await database.LocationNodes.SingleOrDefaultAsync(x => x.Id == id, ct)
        ?? throw new ResourceNotFoundException("Location was not found.");
    private void RequireTenant(ClaimsPrincipal user, string permission) { if (!scopes.HasTenantAccess(user, permission)) throw new UnauthorizedAccessException(); }
    private void RequireSite(ClaimsPrincipal user, string permission, Guid siteId) { if (!scopes.HasAccess(user, permission, siteId)) throw new UnauthorizedAccessException(); }
    private static string Code(string value) => Required(value, "Code").ToUpperInvariant();
    private static string Required(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ValidationException($"{name} is required.") : value.Trim();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static void Version(Guid current, Guid supplied) { if (supplied == Guid.Empty || current != supplied) throw new ConflictException("The record was changed by another user. Refresh and try again."); }
    private static void ValidateSite(SiteInput x) { Required(x.Code, "Code"); Required(x.NameEn, "English site name"); Required(x.TimeZone, "Time zone");
        if (x.Latitude is < -90 or > 90 || x.Longitude is < -180 or > 180) throw new ValidationException("Coordinates are outside their valid range."); }
    private static void ValidateType(LocationTypeInput x) { Required(x.Code, "Code"); Required(x.NameEn, "English location type name"); }
    private async Task Save(CancellationToken ct) { try { await database.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new ConflictException("The record was changed by another user. Refresh and try again."); } }
}
