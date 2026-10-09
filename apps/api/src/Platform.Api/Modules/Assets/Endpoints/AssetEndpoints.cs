using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaaSPlatform.Api.Modules.Assets.Application;
using SaaSPlatform.Api.Modules.Assets.Domain;
using SaaSPlatform.Api.Modules.Identity.Application;

namespace SaaSPlatform.Api.Modules.Assets.Endpoints;

public static class AssetEndpoints
{
    public static IEndpointRouteBuilder MapAssetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var assets = endpoints.MapGroup("/api/v1/assets").WithTags("Assets");
        assets.MapGet("/", async (string? search, Guid? categoryId, Guid? siteId, Guid? locationId,
            AssetOperationalStatus? status, AssetLifecycleState? lifecycle, int? page, int? pageSize,
            IAssetRegistryService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var result = await service.List(user, new AssetListQuery(search, categoryId, siteId, locationId, status, lifecycle, page ?? 1, pageSize ?? 25), ct);
            return Results.Ok(new { Items = result.Items.Select(MapAsset), result.Page, result.PageSize, result.Total });
        }).RequireAuthorization(TenantPermissions.AssetsView);
        assets.MapGet("/dashboard", async (IAssetRegistryService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var result = await service.Dashboard(user, ct);
            return Results.Ok(new { result.Total, result.ByStatus, result.ByCategory, result.BySite, RecentActivity = result.RecentActivity.Select(MapActivity) });
        }).RequireAuthorization(TenantPermissions.AssetsView);
        assets.MapGet("/{id:guid}", async (Guid id, IAssetRegistryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(MapAsset(await service.GetAsset(user, id, ct))))
            .RequireAuthorization(TenantPermissions.AssetsView);
        assets.MapGet("/{id:guid}/history", async (Guid id, IAssetRegistryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok((await service.History(user, id, ct)).Select(MapActivity)))
            .RequireAuthorization(TenantPermissions.AssetsView);
        assets.MapPost("/", async ([FromBody] AssetCreateInput input, IAssetRegistryService service, ClaimsPrincipal user, CancellationToken ct) =>
        {
            var created = await service.Create(user, input, ct); return Results.Created($"/api/v1/assets/{created.Id}", MapAsset(created));
        }).RequireAuthorization(TenantPermissions.AssetsCreate);
        assets.MapPut("/{id:guid}", async (Guid id, [FromBody] AssetUpdateInput input, IAssetRegistryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(MapAsset(await service.Update(user, id, input, ct))))
            .RequireAuthorization(TenantPermissions.AssetsUpdate);
        assets.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] AssetStatusInput input, IAssetRegistryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(MapAsset(await service.ChangeStatus(user, id, input, ct))))
            .RequireAuthorization(TenantPermissions.AssetsChangeStatus);
        assets.MapPatch("/{id:guid}/relocation", async (Guid id, [FromBody] AssetRelocationInput input, IAssetRegistryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(MapAsset(await service.Relocate(user, id, input, ct))))
            .RequireAuthorization(TenantPermissions.AssetsRelocate);
        assets.MapPatch("/{id:guid}/lifecycle", async (Guid id, [FromBody] AssetLifecycleInput input, IAssetRegistryService service, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(MapAsset(await service.SetLifecycle(user, id, input, ct))))
            .RequireAuthorization(TenantPermissions.AssetsRetire);

        var categories = endpoints.MapGroup("/api/v1/asset-categories").WithTags("Asset master data");
        categories.MapGet("/", async (IAssetRegistryService service, CancellationToken ct) => Results.Ok((await service.Categories(ct)).Select(MapCategory))).RequireAuthorization(TenantPermissions.AssetsView);
        categories.MapPost("/", async ([FromBody] CategoryInput input, IAssetRegistryService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) => scopes.HasTenantAccess(user, TenantPermissions.AssetsManageMasterData) ? Results.Created("/api/v1/asset-categories", MapCategory(await service.CreateCategory(input, ct))) : Results.Forbid()).RequireAuthorization(TenantPermissions.AssetsManageMasterData);
        categories.MapPut("/{id:guid}", async (Guid id, [FromBody] CategoryInput input, IAssetRegistryService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) => scopes.HasTenantAccess(user, TenantPermissions.AssetsManageMasterData) ? Results.Ok(MapCategory(await service.UpdateCategory(id, input, ct))) : Results.Forbid()).RequireAuthorization(TenantPermissions.AssetsManageMasterData);
        categories.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] MasterDataStatusInput input, IAssetRegistryService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) => scopes.HasTenantAccess(user, TenantPermissions.AssetsManageMasterData) ? Results.Ok(MapCategory(await service.SetCategoryStatus(id, input, ct))) : Results.Forbid()).RequireAuthorization(TenantPermissions.AssetsManageMasterData);

        var manufacturers = endpoints.MapGroup("/api/v1/manufacturers").WithTags("Asset master data");
        manufacturers.MapGet("/", async (IAssetRegistryService service, CancellationToken ct) => Results.Ok((await service.Manufacturers(ct)).Select(MapManufacturer))).RequireAuthorization(TenantPermissions.AssetsView);
        manufacturers.MapPost("/", async ([FromBody] ManufacturerInput input, IAssetRegistryService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) => scopes.HasTenantAccess(user, TenantPermissions.AssetsManageMasterData) ? Results.Created("/api/v1/manufacturers", MapManufacturer(await service.CreateManufacturer(input, ct))) : Results.Forbid()).RequireAuthorization(TenantPermissions.AssetsManageMasterData);
        manufacturers.MapPut("/{id:guid}", async (Guid id, [FromBody] ManufacturerInput input, IAssetRegistryService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) => scopes.HasTenantAccess(user, TenantPermissions.AssetsManageMasterData) ? Results.Ok(MapManufacturer(await service.UpdateManufacturer(id, input, ct))) : Results.Forbid()).RequireAuthorization(TenantPermissions.AssetsManageMasterData);
        manufacturers.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] MasterDataStatusInput input, IAssetRegistryService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) => scopes.HasTenantAccess(user, TenantPermissions.AssetsManageMasterData) ? Results.Ok(MapManufacturer(await service.SetManufacturerStatus(id, input, ct))) : Results.Forbid()).RequireAuthorization(TenantPermissions.AssetsManageMasterData);

        var models = endpoints.MapGroup("/api/v1/asset-models").WithTags("Asset master data");
        models.MapGet("/", async (IAssetRegistryService service, CancellationToken ct) => Results.Ok((await service.Models(ct)).Select(MapModel))).RequireAuthorization(TenantPermissions.AssetsView);
        models.MapPost("/", async ([FromBody] AssetModelInput input, IAssetRegistryService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) => scopes.HasTenantAccess(user, TenantPermissions.AssetsManageMasterData) ? Results.Created("/api/v1/asset-models", MapModel(await service.CreateModel(input, ct))) : Results.Forbid()).RequireAuthorization(TenantPermissions.AssetsManageMasterData);
        models.MapPut("/{id:guid}", async (Guid id, [FromBody] AssetModelInput input, IAssetRegistryService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) => scopes.HasTenantAccess(user, TenantPermissions.AssetsManageMasterData) ? Results.Ok(MapModel(await service.UpdateModel(id, input, ct))) : Results.Forbid()).RequireAuthorization(TenantPermissions.AssetsManageMasterData);
        models.MapPatch("/{id:guid}/status", async (Guid id, [FromBody] MasterDataStatusInput input, IAssetRegistryService service, ISiteScopeAuthorizer scopes, ClaimsPrincipal user, CancellationToken ct) => scopes.HasTenantAccess(user, TenantPermissions.AssetsManageMasterData) ? Results.Ok(MapModel(await service.SetModelStatus(id, input, ct))) : Results.Forbid()).RequireAuthorization(TenantPermissions.AssetsManageMasterData);
        return endpoints;
    }

    private static object MapCategory(AssetCategory x) => new { x.Id, x.Code, x.NameEn, x.NameAr, x.Description, x.IsActive, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc };
    private static object MapManufacturer(Manufacturer x) => new { x.Id, x.Name, x.Code, x.Country, x.IsActive, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc };
    private static object MapModel(AssetModel x) => new { x.Id, x.Name, x.ManufacturerId, ManufacturerName = x.Manufacturer?.Name, x.CategoryId, CategoryNameEn = x.Category?.NameEn, CategoryNameAr = x.Category?.NameAr, x.TechnicalDescription, x.IsActive, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc };
    private static object MapAsset(Asset x) => new { x.Id, x.Code, x.NameEn, x.NameAr, x.CategoryId, CategoryNameEn = x.Category?.NameEn, CategoryNameAr = x.Category?.NameAr, x.SiteId, SiteNameEn = x.Site?.NameEn, SiteNameAr = x.Site?.NameAr, x.LocationId, LocationNameEn = x.Location?.NameEn, LocationNameAr = x.Location?.NameAr, x.ManufacturerId, ManufacturerName = x.Manufacturer?.Name, x.ModelId, ModelName = x.Model?.Name, x.SerialNumber, x.Barcode, x.Description, x.PurchaseDate, x.PurchaseCost, x.Currency, x.InstallationDate, x.WarrantyStartDate, x.WarrantyExpiryDate, x.SupplierName, x.SupplierReference, x.ResponsibleUserId, ResponsibleUserName = x.ResponsibleUser?.DisplayName, x.Notes, x.OperationalStatus, x.LifecycleState, x.RetiredAtUtc, x.Version, x.CreatedAtUtc, x.UpdatedAtUtc };
    private static object MapActivity(AssetActivity x) => new { x.Id, x.AssetId, x.ActorUserId, x.Action, x.ChangesJson, x.CorrelationId, x.OccurredAtUtc };
}
