using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.BuildingBlocks.Application;
using SaaSPlatform.Api.BuildingBlocks.Infrastructure;
using SaaSPlatform.Api.Modules.Analytics.Application;
using SaaSPlatform.Api.Modules.Assets.Domain;
using SaaSPlatform.Api.Modules.Identity.Application;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Assets.Application;

public sealed record CategoryInput(string Code, string NameEn, string? NameAr, string? Description, Guid Version);
public sealed record ManufacturerInput(string Name, string? Code, string? Country, Guid Version);
public sealed record AssetModelInput(string Name, Guid ManufacturerId, Guid? CategoryId, string? TechnicalDescription, Guid Version);
public sealed record MasterDataStatusInput(bool IsActive, Guid Version);
public sealed record AssetCreateInput(string Code, string NameEn, string? NameAr, Guid CategoryId, Guid SiteId,
    Guid? LocationId, Guid? ManufacturerId, Guid? ModelId, string? SerialNumber, string? Barcode,
    string? Description, DateOnly? PurchaseDate, decimal? PurchaseCost, string? Currency,
    DateOnly? InstallationDate, DateOnly? WarrantyStartDate, DateOnly? WarrantyExpiryDate,
    string? SupplierName, string? SupplierReference, Guid? ResponsibleUserId, string? Notes,
    AssetOperationalStatus OperationalStatus);
public sealed record AssetUpdateInput(string Code, string NameEn, string? NameAr, Guid CategoryId,
    Guid? ManufacturerId, Guid? ModelId, string? SerialNumber, string? Barcode, string? Description,
    DateOnly? PurchaseDate, decimal? PurchaseCost, string? Currency, DateOnly? InstallationDate,
    DateOnly? WarrantyStartDate, DateOnly? WarrantyExpiryDate, string? SupplierName,
    string? SupplierReference, Guid? ResponsibleUserId, string? Notes, Guid Version);
public sealed record AssetStatusInput(AssetOperationalStatus Status, Guid Version);
public sealed record AssetRelocationInput(Guid SiteId, Guid? LocationId, Guid Version);
public sealed record AssetLifecycleInput(bool Retired, Guid Version);
public sealed record AssetListQuery(string? Search, Guid? CategoryId, Guid? SiteId, Guid? LocationId,
    AssetOperationalStatus? Status, AssetLifecycleState? Lifecycle, int Page, int PageSize);
public sealed record AssetPage(IReadOnlyList<Asset> Items, int Page, int PageSize, int Total);
public sealed record AssetDashboard(int Total, IReadOnlyDictionary<string, int> ByStatus,
    IReadOnlyDictionary<string, int> ByCategory, IReadOnlyDictionary<string, int> BySite,
    IReadOnlyList<AssetActivity> RecentActivity);

public interface IAssetRegistryService
{
    Task<IReadOnlyList<AssetCategory>> Categories(CancellationToken ct);
    Task<AssetCategory> CreateCategory(CategoryInput x, CancellationToken ct);
    Task<AssetCategory> UpdateCategory(Guid id, CategoryInput x, CancellationToken ct);
    Task<AssetCategory> SetCategoryStatus(Guid id, MasterDataStatusInput x, CancellationToken ct);
    Task<IReadOnlyList<Manufacturer>> Manufacturers(CancellationToken ct);
    Task<Manufacturer> CreateManufacturer(ManufacturerInput x, CancellationToken ct);
    Task<Manufacturer> UpdateManufacturer(Guid id, ManufacturerInput x, CancellationToken ct);
    Task<Manufacturer> SetManufacturerStatus(Guid id, MasterDataStatusInput x, CancellationToken ct);
    Task<IReadOnlyList<AssetModel>> Models(CancellationToken ct);
    Task<AssetModel> CreateModel(AssetModelInput x, CancellationToken ct);
    Task<AssetModel> UpdateModel(Guid id, AssetModelInput x, CancellationToken ct);
    Task<AssetModel> SetModelStatus(Guid id, MasterDataStatusInput x, CancellationToken ct);
    Task<AssetPage> List(ClaimsPrincipal user, AssetListQuery input, CancellationToken ct);
    Task<AssetDashboard> Dashboard(ClaimsPrincipal user, CancellationToken ct);
    Task<Asset> GetAsset(ClaimsPrincipal user, Guid id, CancellationToken ct);
    Task<Asset> Create(ClaimsPrincipal user, AssetCreateInput x, CancellationToken ct);
    Task<Asset> Update(ClaimsPrincipal user, Guid id, AssetUpdateInput x, CancellationToken ct);
    Task<Asset> ChangeStatus(ClaimsPrincipal user, Guid id, AssetStatusInput x, CancellationToken ct);
    Task<Asset> Relocate(ClaimsPrincipal user, Guid id, AssetRelocationInput x, CancellationToken ct);
    Task<Asset> SetLifecycle(ClaimsPrincipal user, Guid id, AssetLifecycleInput x, CancellationToken ct);
    Task<IReadOnlyList<AssetActivity>> History(ClaimsPrincipal user, Guid id, CancellationToken ct);
}

public sealed class AssetRegistryService(
    PlatformDbContext database,
    ICurrentTenant currentTenant,
    ISiteScopeAuthorizer scopes,
    IAuditWriter audit,
    IHttpContextAccessor httpContext,
    TimeProvider timeProvider) : IAssetRegistryService
{
    private Guid TenantId => currentTenant.Id ?? throw new InvalidOperationException("Tenant context is required.");

    public async Task<IReadOnlyList<AssetCategory>> Categories(CancellationToken ct) =>
        await database.AssetCategories.AsNoTracking().OrderBy(x => x.Code).ToListAsync(ct);

    public async Task<AssetCategory> CreateCategory(CategoryInput x, CancellationToken ct)
    {
        var code = Code(x.Code); Required(x.NameEn, "English name");
        if (await database.AssetCategories.AnyAsync(v => v.Code == code, ct)) throw new ConflictException("Asset category code already exists.");
        var entity = new AssetCategory { Id = Guid.NewGuid(), TenantId = TenantId, Code = code, NameEn = x.NameEn.Trim(), NameAr = Clean(x.NameAr), Description = Clean(x.Description) };
        database.AssetCategories.Add(entity); audit.Add("AssetCategory.Created", nameof(AssetCategory), entity.Id, new { entity.Code }); await Save(ct); return entity;
    }

    public async Task<AssetCategory> UpdateCategory(Guid id, CategoryInput x, CancellationToken ct)
    {
        var entity = await database.AssetCategories.SingleOrDefaultAsync(v => v.Id == id, ct) ?? throw new ResourceNotFoundException("Asset category was not found.");
        Version(entity.Version, x.Version); var code = Code(x.Code); Required(x.NameEn, "English name");
        if (await database.AssetCategories.AnyAsync(v => v.Id != id && v.Code == code, ct)) throw new ConflictException("Asset category code already exists.");
        entity.Code = code; entity.NameEn = x.NameEn.Trim(); entity.NameAr = Clean(x.NameAr); entity.Description = Clean(x.Description); entity.Version = Guid.NewGuid();
        audit.Add("AssetCategory.Updated", nameof(AssetCategory), id); await Save(ct); return entity;
    }

    public async Task<AssetCategory> SetCategoryStatus(Guid id, MasterDataStatusInput x, CancellationToken ct)
    {
        var entity = await database.AssetCategories.SingleOrDefaultAsync(v => v.Id == id, ct) ?? throw new ResourceNotFoundException("Asset category was not found.");
        Version(entity.Version, x.Version);
        if (!x.IsActive && await database.Assets.AnyAsync(a => a.CategoryId == id && a.LifecycleState == AssetLifecycleState.Active, ct)) throw new ConflictException("An active asset uses this category.");
        entity.IsActive = x.IsActive; entity.Version = Guid.NewGuid(); audit.Add(x.IsActive ? "AssetCategory.Activated" : "AssetCategory.Deactivated", nameof(AssetCategory), id); await Save(ct); return entity;
    }

    public async Task<IReadOnlyList<Manufacturer>> Manufacturers(CancellationToken ct) =>
        await database.Manufacturers.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct);

    public async Task<Manufacturer> CreateManufacturer(ManufacturerInput x, CancellationToken ct)
    {
        var name = Required(x.Name, "Manufacturer name"); var normalized = Normalize(name); var code = OptionalCode(x.Code);
        if (await database.Manufacturers.AnyAsync(v => v.NormalizedName == normalized || (code != null && v.Code == code), ct)) throw new ConflictException("Manufacturer name or code already exists.");
        var entity = new Manufacturer { Id = Guid.NewGuid(), TenantId = TenantId, Name = name, NormalizedName = normalized, Code = code, Country = Clean(x.Country) };
        database.Manufacturers.Add(entity); audit.Add("Manufacturer.Created", nameof(Manufacturer), entity.Id, new { entity.Name }); await Save(ct); return entity;
    }

    public async Task<Manufacturer> UpdateManufacturer(Guid id, ManufacturerInput x, CancellationToken ct)
    {
        var entity = await database.Manufacturers.SingleOrDefaultAsync(v => v.Id == id, ct) ?? throw new ResourceNotFoundException("Manufacturer was not found.");
        Version(entity.Version, x.Version); var name = Required(x.Name, "Manufacturer name"); var normalized = Normalize(name); var code = OptionalCode(x.Code);
        if (await database.Manufacturers.AnyAsync(v => v.Id != id && (v.NormalizedName == normalized || (code != null && v.Code == code)), ct)) throw new ConflictException("Manufacturer name or code already exists.");
        entity.Name = name; entity.NormalizedName = normalized; entity.Code = code; entity.Country = Clean(x.Country); entity.Version = Guid.NewGuid(); audit.Add("Manufacturer.Updated", nameof(Manufacturer), id); await Save(ct); return entity;
    }

    public async Task<Manufacturer> SetManufacturerStatus(Guid id, MasterDataStatusInput x, CancellationToken ct)
    {
        var entity = await database.Manufacturers.SingleOrDefaultAsync(v => v.Id == id, ct) ?? throw new ResourceNotFoundException("Manufacturer was not found.");
        Version(entity.Version, x.Version);
        if (!x.IsActive && await database.Assets.AnyAsync(a => a.ManufacturerId == id && a.LifecycleState == AssetLifecycleState.Active, ct)) throw new ConflictException("An active asset uses this manufacturer.");
        entity.IsActive = x.IsActive; entity.Version = Guid.NewGuid(); audit.Add(x.IsActive ? "Manufacturer.Activated" : "Manufacturer.Deactivated", nameof(Manufacturer), id); await Save(ct); return entity;
    }

    public async Task<IReadOnlyList<AssetModel>> Models(CancellationToken ct) => await database.AssetModels.AsNoTracking()
        .Include(x => x.Manufacturer).Include(x => x.Category).OrderBy(x => x.Manufacturer!.Name).ThenBy(x => x.Name).ToListAsync(ct);

    public async Task<AssetModel> CreateModel(AssetModelInput x, CancellationToken ct)
    {
        var name = Required(x.Name, "Model name"); var normalized = Normalize(name); await ValidateModelReferences(x.ManufacturerId, x.CategoryId, ct);
        if (await database.AssetModels.AnyAsync(v => v.ManufacturerId == x.ManufacturerId && v.NormalizedName == normalized, ct)) throw new ConflictException("This model already exists for the manufacturer.");
        var entity = new AssetModel { Id = Guid.NewGuid(), TenantId = TenantId, Name = name, NormalizedName = normalized, ManufacturerId = x.ManufacturerId, CategoryId = x.CategoryId, TechnicalDescription = Clean(x.TechnicalDescription) };
        database.AssetModels.Add(entity); audit.Add("AssetModel.Created", nameof(AssetModel), entity.Id, new { entity.Name, entity.ManufacturerId }); await Save(ct); return entity;
    }

    public async Task<AssetModel> UpdateModel(Guid id, AssetModelInput x, CancellationToken ct)
    {
        var entity = await database.AssetModels.SingleOrDefaultAsync(v => v.Id == id, ct) ?? throw new ResourceNotFoundException("Asset model was not found.");
        Version(entity.Version, x.Version); var name = Required(x.Name, "Model name"); var normalized = Normalize(name); await ValidateModelReferences(x.ManufacturerId, x.CategoryId, ct);
        if (await database.AssetModels.AnyAsync(v => v.Id != id && v.ManufacturerId == x.ManufacturerId && v.NormalizedName == normalized, ct)) throw new ConflictException("This model already exists for the manufacturer.");
        entity.Name = name; entity.NormalizedName = normalized; entity.ManufacturerId = x.ManufacturerId; entity.CategoryId = x.CategoryId; entity.TechnicalDescription = Clean(x.TechnicalDescription); entity.Version = Guid.NewGuid();
        audit.Add("AssetModel.Updated", nameof(AssetModel), id); await Save(ct); return entity;
    }

    public async Task<AssetModel> SetModelStatus(Guid id, MasterDataStatusInput x, CancellationToken ct)
    {
        var entity = await database.AssetModels.SingleOrDefaultAsync(v => v.Id == id, ct) ?? throw new ResourceNotFoundException("Asset model was not found.");
        Version(entity.Version, x.Version);
        if (!x.IsActive && await database.Assets.AnyAsync(a => a.ModelId == id && a.LifecycleState == AssetLifecycleState.Active, ct)) throw new ConflictException("An active asset uses this model.");
        entity.IsActive = x.IsActive; entity.Version = Guid.NewGuid(); audit.Add(x.IsActive ? "AssetModel.Activated" : "AssetModel.Deactivated", nameof(AssetModel), id); await Save(ct); return entity;
    }

    public async Task<AssetPage> List(ClaimsPrincipal user, AssetListQuery input, CancellationToken ct)
    {
        var query = Accessible(user, TenantPermissions.AssetsView).AsNoTracking().IncludeAssetReferences();
        if (!string.IsNullOrWhiteSpace(input.Search)) { var term = $"%{input.Search.Trim()}%"; query = query.Where(x => EF.Functions.ILike(x.Code, term) || EF.Functions.ILike(x.NameEn, term) || (x.NameAr != null && EF.Functions.ILike(x.NameAr, term)) || (x.SerialNumber != null && EF.Functions.ILike(x.SerialNumber, term))); }
        if (input.CategoryId is Guid category) query = query.Where(x => x.CategoryId == category);
        if (input.SiteId is Guid site) query = query.Where(x => x.SiteId == site);
        if (input.LocationId is Guid location) query = query.Where(x => x.LocationId == location);
        if (input.Status is AssetOperationalStatus status) query = query.Where(x => x.OperationalStatus == status);
        if (input.Lifecycle is AssetLifecycleState lifecycle) query = query.Where(x => x.LifecycleState == lifecycle);
        var page = Math.Max(1, input.Page); var size = Math.Clamp(input.PageSize, 1, 100); var total = await query.CountAsync(ct);
        return new AssetPage(await query.OrderBy(x => x.Code).Skip((page - 1) * size).Take(size).ToListAsync(ct), page, size, total);
    }

    public async Task<AssetDashboard> Dashboard(ClaimsPrincipal user, CancellationToken ct)
    {
        var assets = await Accessible(user, TenantPermissions.AssetsView).AsNoTracking().IncludeAssetReferences().ToListAsync(ct);
        var ids = assets.Select(x => x.Id).ToArray();
        var recent = await database.AssetActivities.AsNoTracking().Where(x => ids.Contains(x.AssetId)).OrderByDescending(x => x.OccurredAtUtc).Take(10).ToListAsync(ct);
        return new AssetDashboard(assets.Count,
            assets.GroupBy(x => x.OperationalStatus.ToString()).ToDictionary(x => x.Key, x => x.Count()),
            assets.GroupBy(x => x.Category?.NameEn ?? "Unknown").ToDictionary(x => x.Key, x => x.Count()),
            assets.GroupBy(x => x.Site?.NameEn ?? "Unknown").ToDictionary(x => x.Key, x => x.Count()), recent);
    }

    public async Task<Asset> GetAsset(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var entity = await database.Assets.AsNoTracking().IncludeAssetReferences().SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Asset was not found.");
        RequireSite(user, TenantPermissions.AssetsView, entity.SiteId); return entity;
    }

    public async Task<Asset> Create(ClaimsPrincipal user, AssetCreateInput x, CancellationToken ct)
    {
        RequireSite(user, TenantPermissions.AssetsCreate, x.SiteId); ValidateFields(x.Code, x.NameEn, x.PurchaseCost, x.Currency, x.PurchaseDate, x.InstallationDate, x.WarrantyStartDate, x.WarrantyExpiryDate);
        await ValidateReferences(x.SiteId, x.LocationId, x.CategoryId, x.ManufacturerId, x.ModelId, x.ResponsibleUserId, true, ct);
        var code = Code(x.Code); if (await database.Assets.AnyAsync(a => a.Code == code, ct)) throw new ConflictException("Asset code already exists.");
        var entity = new Asset { Id = Guid.NewGuid(), TenantId = TenantId, Code = code, NameEn = x.NameEn.Trim(), NameAr = Clean(x.NameAr), CategoryId = x.CategoryId, SiteId = x.SiteId, LocationId = x.LocationId, ManufacturerId = x.ManufacturerId, ModelId = x.ModelId, SerialNumber = Clean(x.SerialNumber), Barcode = Clean(x.Barcode), Description = Clean(x.Description), PurchaseDate = x.PurchaseDate, PurchaseCost = x.PurchaseCost, Currency = Currency(x.Currency, x.PurchaseCost), InstallationDate = x.InstallationDate, WarrantyStartDate = x.WarrantyStartDate, WarrantyExpiryDate = x.WarrantyExpiryDate, SupplierName = Clean(x.SupplierName), SupplierReference = Clean(x.SupplierReference), ResponsibleUserId = x.ResponsibleUserId, Notes = Clean(x.Notes), OperationalStatus = x.OperationalStatus };
        database.Assets.Add(entity); Activity(entity, "Asset.Created", new { entity.Code, entity.SiteId, entity.LocationId, entity.OperationalStatus }); audit.Add("Asset.Created", nameof(Asset), entity.Id, new { entity.Code, entity.SiteId }); await Save(ct); return await Load(entity.Id, ct);
    }

    public async Task<Asset> Update(ClaimsPrincipal user, Guid id, AssetUpdateInput x, CancellationToken ct)
    {
        var entity = await Entity(id, ct); RequireSite(user, TenantPermissions.AssetsUpdate, entity.SiteId); EnsureActive(entity); Version(entity.Version, x.Version);
        ValidateFields(x.Code, x.NameEn, x.PurchaseCost, x.Currency, x.PurchaseDate, x.InstallationDate, x.WarrantyStartDate, x.WarrantyExpiryDate);
        await ValidateReferences(entity.SiteId, entity.LocationId, x.CategoryId, x.ManufacturerId, x.ModelId, x.ResponsibleUserId, true, ct);
        var code = Code(x.Code); if (await database.Assets.AnyAsync(a => a.Id != id && a.Code == code, ct)) throw new ConflictException("Asset code already exists.");
        var oldResponsible = entity.ResponsibleUserId;
        entity.Code = code; entity.NameEn = x.NameEn.Trim(); entity.NameAr = Clean(x.NameAr); entity.CategoryId = x.CategoryId; entity.ManufacturerId = x.ManufacturerId; entity.ModelId = x.ModelId; entity.SerialNumber = Clean(x.SerialNumber); entity.Barcode = Clean(x.Barcode); entity.Description = Clean(x.Description); entity.PurchaseDate = x.PurchaseDate; entity.PurchaseCost = x.PurchaseCost; entity.Currency = Currency(x.Currency, x.PurchaseCost); entity.InstallationDate = x.InstallationDate; entity.WarrantyStartDate = x.WarrantyStartDate; entity.WarrantyExpiryDate = x.WarrantyExpiryDate; entity.SupplierName = Clean(x.SupplierName); entity.SupplierReference = Clean(x.SupplierReference); entity.ResponsibleUserId = x.ResponsibleUserId; entity.Notes = Clean(x.Notes); entity.Version = Guid.NewGuid();
        Activity(entity, "Asset.Updated", new { entity.Code, entity.CategoryId, entity.ManufacturerId, entity.ModelId });
        if (oldResponsible != x.ResponsibleUserId) Activity(entity, "Asset.ResponsibilityChanged", new { OldUserId = oldResponsible, NewUserId = x.ResponsibleUserId });
        audit.Add("Asset.Updated", nameof(Asset), id); await Save(ct); return await Load(id, ct);
    }

    public async Task<Asset> ChangeStatus(ClaimsPrincipal user, Guid id, AssetStatusInput x, CancellationToken ct)
    {
        var entity = await Entity(id, ct); RequireSite(user, TenantPermissions.AssetsChangeStatus, entity.SiteId); EnsureActive(entity); Version(entity.Version, x.Version);
        if (!Enum.IsDefined(x.Status)) throw new ValidationException("Operational status is invalid.");
        if (!Allowed(entity.OperationalStatus, x.Status)) throw new ValidationException($"Status cannot change from {entity.OperationalStatus} to {x.Status}.");
        var old = entity.OperationalStatus; entity.OperationalStatus = x.Status; entity.Version = Guid.NewGuid(); Activity(entity, "Asset.StatusChanged", new { OldStatus = old, NewStatus = x.Status }); audit.Add("Asset.StatusChanged", nameof(Asset), id, new { OldStatus = old, NewStatus = x.Status }); await Save(ct); return await Load(id, ct);
    }

    public async Task<Asset> Relocate(ClaimsPrincipal user, Guid id, AssetRelocationInput x, CancellationToken ct)
    {
        var entity = await Entity(id, ct); EnsureActive(entity); Version(entity.Version, x.Version);
        RequireSite(user, TenantPermissions.AssetsRelocate, entity.SiteId); RequireSite(user, TenantPermissions.AssetsRelocate, x.SiteId);
        await ValidateSiteLocation(x.SiteId, x.LocationId, true, ct); var oldSite = entity.SiteId; var oldLocation = entity.LocationId;
        entity.SiteId = x.SiteId; entity.LocationId = x.LocationId; entity.Version = Guid.NewGuid(); Activity(entity, "Asset.Relocated", new { OldSiteId = oldSite, NewSiteId = x.SiteId, OldLocationId = oldLocation, NewLocationId = x.LocationId }); audit.Add("Asset.Relocated", nameof(Asset), id, new { OldSiteId = oldSite, NewSiteId = x.SiteId }); await Save(ct); return await Load(id, ct);
    }

    public async Task<Asset> SetLifecycle(ClaimsPrincipal user, Guid id, AssetLifecycleInput x, CancellationToken ct)
    {
        var entity = await Entity(id, ct); RequireSite(user, TenantPermissions.AssetsRetire, entity.SiteId); Version(entity.Version, x.Version);
        var target = x.Retired ? AssetLifecycleState.Retired : AssetLifecycleState.Active;
        if (entity.LifecycleState == target) throw new ValidationException("Asset is already in the requested lifecycle state.");
        if (!x.Retired) await ValidateReferences(entity.SiteId, entity.LocationId, entity.CategoryId, entity.ManufacturerId, entity.ModelId, entity.ResponsibleUserId, true, ct);
        entity.LifecycleState = target; entity.RetiredAtUtc = x.Retired ? timeProvider.GetUtcNow() : null; entity.Version = Guid.NewGuid(); Activity(entity, x.Retired ? "Asset.Retired" : "Asset.Reactivated"); audit.Add(x.Retired ? "Asset.Retired" : "Asset.Reactivated", nameof(Asset), id); await Save(ct); return await Load(id, ct);
    }

    public async Task<IReadOnlyList<AssetActivity>> History(ClaimsPrincipal user, Guid id, CancellationToken ct)
    {
        var entity = await Entity(id, ct); RequireSite(user, TenantPermissions.AssetsView, entity.SiteId);
        return await database.AssetActivities.AsNoTracking().Where(x => x.AssetId == id).OrderByDescending(x => x.OccurredAtUtc).ToListAsync(ct);
    }

    private IQueryable<Asset> Accessible(ClaimsPrincipal user, string permission)
    {
        if (scopes.HasTenantAccess(user, permission)) return database.Assets;
        var sites = user.FindAll(TenantClaims.Grant).Select(x => x.Value.Split('|')).Where(x => x.Length == 3 && x[0] == permission && x[1] == "Site" && Guid.TryParse(x[2], out _)).Select(x => Guid.Parse(x[2])).Distinct().ToArray();
        return database.Assets.Where(x => sites.Contains(x.SiteId));
    }

    private async Task ValidateReferences(Guid siteId, Guid? locationId, Guid categoryId, Guid? manufacturerId, Guid? modelId, Guid? responsibleUserId, bool active, CancellationToken ct)
    {
        await ValidateSiteLocation(siteId, locationId, active, ct);
        if (!await database.AssetCategories.AnyAsync(x => x.Id == categoryId && (!active || x.IsActive), ct)) throw new ValidationException("An active category in this tenant is required.");
        if (manufacturerId is Guid manufacturer && !await database.Manufacturers.AnyAsync(x => x.Id == manufacturer && (!active || x.IsActive), ct)) throw new ValidationException("The manufacturer must be active and belong to this tenant.");
        if (modelId is Guid model)
        {
            var match = await database.AssetModels.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model && (!active || x.IsActive), ct) ?? throw new ValidationException("The model must be active and belong to this tenant.");
            if (manufacturerId != match.ManufacturerId) throw new ValidationException("The model does not belong to the selected manufacturer.");
            if (match.CategoryId is Guid modelCategory && modelCategory != categoryId) throw new ValidationException("The model does not belong to the selected category.");
        }
        if (responsibleUserId is Guid userId && !await database.TenantMemberships.AsNoTracking().AnyAsync(x => x.UserId == userId && x.IsActive && x.User != null && x.User.IsActive, ct)) throw new ValidationException("Responsible user must have an active membership in this tenant.");
    }

    private async Task ValidateSiteLocation(Guid siteId, Guid? locationId, bool active, CancellationToken ct)
    {
        if (!await database.Sites.AnyAsync(x => x.Id == siteId && (!active || x.IsActive), ct)) throw new ValidationException("The site must be active and belong to this tenant.");
        if (locationId is Guid location && !await database.LocationNodes.AnyAsync(x => x.Id == location && x.SiteId == siteId && (!active || x.IsActive), ct)) throw new ValidationException("Location must be active and belong to the selected site.");
    }

    private async Task ValidateModelReferences(Guid manufacturerId, Guid? categoryId, CancellationToken ct)
    {
        if (!await database.Manufacturers.AnyAsync(x => x.Id == manufacturerId && x.IsActive, ct)) throw new ValidationException("An active manufacturer in this tenant is required.");
        if (categoryId is Guid category && !await database.AssetCategories.AnyAsync(x => x.Id == category && x.IsActive, ct)) throw new ValidationException("Category must be active and belong to this tenant.");
    }

    private void Activity(Asset asset, string action, object? changes = null) => database.AssetActivities.Add(new AssetActivity { Id = Guid.NewGuid(), TenantId = TenantId, AssetId = asset.Id, ActorUserId = Guid.TryParse(httpContext.HttpContext?.User.FindFirstValue("sub"), out var actor) ? actor : null, Action = action, ChangesJson = changes is null ? null : JsonSerializer.Serialize(changes), CorrelationId = httpContext.HttpContext?.TraceIdentifier, OccurredAtUtc = timeProvider.GetUtcNow() });
    private void RequireSite(ClaimsPrincipal user, string permission, Guid siteId) { if (!scopes.HasAccess(user, permission, siteId)) throw new UnauthorizedAccessException("The requested site is outside the granted scope."); }
    private static void EnsureActive(Asset asset) { if (asset.LifecycleState == AssetLifecycleState.Retired) throw new ConflictException("Retired assets must be reactivated before modification."); }
    private static bool Allowed(AssetOperationalStatus from, AssetOperationalStatus to) => from != to && (from, to) switch { (AssetOperationalStatus.UnderMaintenance, AssetOperationalStatus.Standby) => false, (AssetOperationalStatus.Standby, AssetOperationalStatus.UnderMaintenance) => false, _ => true };
    private async Task<Asset> Entity(Guid id, CancellationToken ct) => await database.Assets.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new ResourceNotFoundException("Asset was not found.");
    private async Task<Asset> Load(Guid id, CancellationToken ct) => await database.Assets.AsNoTracking().IncludeAssetReferences().SingleAsync(x => x.Id == id, ct);
    private static void ValidateFields(string code, string name, decimal? cost, string? currency, DateOnly? purchase, DateOnly? installation, DateOnly? warrantyStart, DateOnly? warrantyExpiry)
    {
        _ = Code(code); _ = Required(name, "English name"); if (cost < 0) throw new ValidationException("Purchase cost cannot be negative.");
        if (cost is not null && (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)) throw new ValidationException("A three-letter currency is required with purchase cost.");
        if (purchase is not null && installation is not null && installation < purchase) throw new ValidationException("Installation date cannot be before purchase date.");
        if (warrantyStart is not null && warrantyExpiry is not null && warrantyExpiry < warrantyStart) throw new ValidationException("Warranty expiry cannot be before warranty start.");
    }
    private static string? Currency(string? value, decimal? cost) => cost is null ? null : value!.Trim().ToUpperInvariant();
    private static string Code(string value) { var code = Required(value, "Code").ToUpperInvariant(); if (code.Length > 80) throw new ValidationException("Code is too long."); return code; }
    private static string? OptionalCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : Code(value);
    private static string Required(string value, string name) => string.IsNullOrWhiteSpace(value) ? throw new ValidationException($"{name} is required.") : value.Trim();
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    private static void Version(Guid current, Guid supplied) { if (supplied == Guid.Empty || current != supplied) throw new ConflictException("The record was changed by another user."); }
    private async Task Save(CancellationToken ct) { try { await database.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new ConflictException("The record was changed by another user."); } }
}

internal static class AssetQueryExtensions
{
    public static IQueryable<Asset> IncludeAssetReferences(this IQueryable<Asset> query) => query
        .Include(x => x.Category).Include(x => x.Site).Include(x => x.Location)
        .Include(x => x.Manufacturer).Include(x => x.Model).Include(x => x.ResponsibleUser);
}
