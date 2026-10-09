using System.Security.Claims;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;

namespace SaaSPlatform.Api.Modules.Identity.Application;

public static class PlatformPermissions
{
    public const string TenantView = "Tenant.View";
    public const string TenantCreate = "Tenant.Create";
    public const string TenantEdit = "Tenant.Edit";
    public const string TenantActivate = "Tenant.Activate";
    public const string TenantDeactivate = "Tenant.Deactivate";

    public static readonly string[] All = [TenantView, TenantCreate, TenantEdit, TenantActivate, TenantDeactivate];
}

public static class TenantPermissions
{
    public const string CompanyView = "Company.View";
    public const string CompanyEdit = "Company.Edit";
    public const string SiteView = "Site.View";
    public const string SiteCreate = "Site.Create";
    public const string SiteEdit = "Site.Edit";
    public const string SiteActivate = "Site.Activate";
    public const string SiteDeactivate = "Site.Deactivate";
    public const string LocationTypeView = "LocationType.View";
    public const string LocationTypeCreate = "LocationType.Create";
    public const string LocationTypeEdit = "LocationType.Edit";
    public const string LocationTypeActivate = "LocationType.Activate";
    public const string LocationTypeDeactivate = "LocationType.Deactivate";
    public const string LocationView = "Location.View";
    public const string LocationCreate = "Location.Create";
    public const string LocationEdit = "Location.Edit";
    public const string LocationMove = "Location.Move";
    public const string LocationActivate = "Location.Activate";
    public const string LocationDeactivate = "Location.Deactivate";
    public const string UsersView = "administration.users.view";
    public const string UsersManage = "administration.users.manage";
    public const string RolesView = "administration.roles.view";
    public const string RolesManage = "administration.roles.manage";
    public const string MembershipsManage = "administration.memberships.manage";
    public const string SessionsView = "administration.sessions.view";
    public const string SessionsRevoke = "administration.sessions.revoke";
    public const string AssetsView = "assets.view";
    public const string AssetsCreate = "assets.create";
    public const string AssetsUpdate = "assets.update";
    public const string AssetsManageMasterData = "assets.manage-master-data";
    public const string AssetsChangeStatus = "assets.change-status";
    public const string AssetsRelocate = "assets.relocate";
    public const string AssetsRetire = "assets.retire";
    public const string MaintenanceBreakdownsView = "maintenance.breakdowns.view";
    public const string MaintenanceBreakdownsCreate = "maintenance.breakdowns.create";
    public const string MaintenanceBreakdownsReview = "maintenance.breakdowns.review";
    public const string MaintenanceWorkOrdersView = "maintenance.workorders.view";
    public const string MaintenanceWorkOrdersCreate = "maintenance.workorders.create";
    public const string MaintenanceWorkOrdersAssign = "maintenance.workorders.assign";
    public const string MaintenanceWorkOrdersExecute = "maintenance.workorders.execute";
    public const string MaintenanceWorkOrdersVerify = "maintenance.workorders.verify";
    public const string MaintenanceWorkOrdersCancel = "maintenance.workorders.cancel";
    public const string MaintenancePmView = "maintenance.pm.view";
    public const string MaintenancePmCreate = "maintenance.pm.create";
    public const string MaintenancePmUpdate = "maintenance.pm.update";
    public const string MaintenancePmManage = "maintenance.pm.manage";
    public const string MaintenancePmGenerate = "maintenance.pm.generate";
    public const string MaintenancePmExecute = "maintenance.pm.execute";
    public const string MaintenancePmVerify = "maintenance.pm.verify";
    public const string InventoryView = "inventory.view";
    public const string InventoryPartsManage = "inventory.parts.manage";
    public const string InventoryWarehousesManage = "inventory.warehouses.manage";
    public const string InventoryReceive = "inventory.receive";
    public const string InventoryIssue = "inventory.issue";
    public const string InventoryTransfer = "inventory.transfer";
    public const string InventoryAdjust = "inventory.adjust";
    public const string InventoryReserve = "inventory.reserve";
    public const string InventoryAudit = "inventory.audit";
    public const string ProcurementView = "procurement.view";
    public const string ProcurementSuppliersManage = "procurement.suppliers.manage";
    public const string ProcurementRequisitionsCreate = "procurement.requisitions.create";
    public const string ProcurementRequisitionsReview = "procurement.requisitions.review";
    public const string ProcurementPurchaseOrdersCreate = "procurement.purchase-orders.create";
    public const string ProcurementPurchaseOrdersManage = "procurement.purchase-orders.manage";
    public const string ProcurementReceivingCreate = "procurement.receiving.create";
    public const string ProcurementReceivingView = "procurement.receiving.view";
    public const string ProcurementDashboardView = "procurement.dashboard.view";
    public const string ServiceRequestsView = "service_requests.view";
    public const string ServiceRequestsCreate = "service_requests.create";
    public const string ServiceRequestsViewOwn = "service_requests.view_own";
    public const string ServiceRequestsTriage = "service_requests.triage";
    public const string ServiceRequestsAssign = "service_requests.assign";
    public const string ServiceRequestsExecute = "service_requests.execute";
    public const string ServiceRequestsResolve = "service_requests.resolve";
    public const string ServiceRequestsClose = "service_requests.close";
    public const string ServiceRequestsConvertToMaintenance = "service_requests.convert_to_maintenance";
    public const string ServiceRequestCategoriesManage = "service_request_categories.manage";

    public static readonly string[] All =
    [
        CompanyView, CompanyEdit, SiteView, SiteCreate, SiteEdit, SiteActivate, SiteDeactivate,
        LocationTypeView, LocationTypeCreate, LocationTypeEdit, LocationTypeActivate, LocationTypeDeactivate,
        LocationView, LocationCreate, LocationEdit, LocationMove, LocationActivate, LocationDeactivate,
        UsersView, UsersManage, RolesView, RolesManage, MembershipsManage, SessionsView, SessionsRevoke,
        AssetsView, AssetsCreate, AssetsUpdate, AssetsManageMasterData, AssetsChangeStatus, AssetsRelocate, AssetsRetire,
        MaintenanceBreakdownsView, MaintenanceBreakdownsCreate, MaintenanceBreakdownsReview,
        MaintenanceWorkOrdersView, MaintenanceWorkOrdersCreate, MaintenanceWorkOrdersAssign,
        MaintenanceWorkOrdersExecute, MaintenanceWorkOrdersVerify, MaintenanceWorkOrdersCancel,
        MaintenancePmView, MaintenancePmCreate, MaintenancePmUpdate, MaintenancePmManage,
        MaintenancePmGenerate, MaintenancePmExecute, MaintenancePmVerify,
        InventoryView, InventoryPartsManage, InventoryWarehousesManage, InventoryReceive,
        InventoryIssue, InventoryTransfer, InventoryAdjust, InventoryReserve, InventoryAudit,
        ProcurementView, ProcurementSuppliersManage, ProcurementRequisitionsCreate,
        ProcurementRequisitionsReview, ProcurementPurchaseOrdersCreate, ProcurementPurchaseOrdersManage,
        ProcurementReceivingCreate, ProcurementReceivingView, ProcurementDashboardView,
        ServiceRequestsView, ServiceRequestsCreate, ServiceRequestsViewOwn, ServiceRequestsTriage,
        ServiceRequestsAssign, ServiceRequestsExecute, ServiceRequestsResolve, ServiceRequestsClose,
        ServiceRequestsConvertToMaintenance, ServiceRequestCategoriesManage
    ];
}

public sealed record PermissionGrant(string Permission, AccessScopeKind ScopeKind, Guid? ScopeId)
{
    public string ToClaimValue() => $"{Permission}|{ScopeKind}|{ScopeId?.ToString() ?? string.Empty}";
}

public interface ISiteScopeAuthorizer
{
    bool HasTenantAccess(ClaimsPrincipal user, string permission);
    bool HasAccess(ClaimsPrincipal user, string permission, Guid siteId);
}

public sealed class SiteScopeAuthorizer : ISiteScopeAuthorizer
{
    public bool HasTenantAccess(ClaimsPrincipal user, string permission) =>
        Grants(user, permission).Any(x => x.Kind == AccessScopeKind.Tenant);

    public bool HasAccess(ClaimsPrincipal user, string permission, Guid siteId)
    {
        foreach (var grant in Grants(user, permission))
        {
            if (grant.Kind == AccessScopeKind.Tenant)
            {
                return true;
            }

            if (grant.Kind == AccessScopeKind.Site && grant.ScopeId == siteId)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<(AccessScopeKind Kind, Guid? ScopeId)> Grants(ClaimsPrincipal user, string permission)
    {
        foreach (var claim in user.FindAll(TenantClaims.Grant))
        {
            var parts = claim.Value.Split('|');
            if (parts.Length == 3 && string.Equals(parts[0], permission, StringComparison.Ordinal) &&
                Enum.TryParse<AccessScopeKind>(parts[1], out var kind))
            {
                yield return (kind, Guid.TryParse(parts[2], out var id) ? id : null);
            }
        }
    }
}
