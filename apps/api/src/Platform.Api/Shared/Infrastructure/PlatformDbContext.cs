using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SaaSPlatform.Api.Modules.Identity.Domain;
using SaaSPlatform.Api.Modules.Tenancy.Application;
using SaaSPlatform.Api.Modules.Tenancy.Domain;
using SaaSPlatform.Api.Modules.Organization.Domain;
using SaaSPlatform.Api.Modules.Locations.Domain;
using SaaSPlatform.Api.Modules.Analytics.Domain;
using SaaSPlatform.Api.Modules.Assets.Domain;
using SaaSPlatform.Api.Modules.Maintenance.Domain;
using SaaSPlatform.Api.Modules.Inventory.Domain;
using SaaSPlatform.Api.Modules.Procurement.Domain;
using SaaSPlatform.Api.Modules.ServiceRequests.Domain;
using SaaSPlatform.Api.BuildingBlocks.Domain;

namespace SaaSPlatform.Api.BuildingBlocks.Infrastructure;

public sealed class PlatformDbContext(
    DbContextOptions<PlatformDbContext> options,
    ICurrentTenant currentTenant,
    TimeProvider timeProvider)
    : IdentityDbContext<PlatformUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<TenantRole> TenantRoles => Set<TenantRole>();
    public DbSet<PermissionDefinition> Permissions => Set<PermissionDefinition>();
    public DbSet<RolePermissionGrant> RolePermissions => Set<RolePermissionGrant>();
    public DbSet<MembershipRole> MembershipRoles => Set<MembershipRole>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<CompanyProfile> CompanyProfiles => Set<CompanyProfile>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<LocationType> LocationTypes => Set<LocationType>();
    public DbSet<LocationNode> LocationNodes => Set<LocationNode>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<Manufacturer> Manufacturers => Set<Manufacturer>();
    public DbSet<AssetModel> AssetModels => Set<AssetModel>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetActivity> AssetActivities => Set<AssetActivity>();
    public DbSet<BreakdownReport> BreakdownReports => Set<BreakdownReport>();
    public DbSet<BreakdownReview> BreakdownReviews => Set<BreakdownReview>();
    public DbSet<CorrectiveWorkOrder> CorrectiveWorkOrders => Set<CorrectiveWorkOrder>();
    public DbSet<WorkOrderAssignment> WorkOrderAssignments => Set<WorkOrderAssignment>();
    public DbSet<WorkOrderActivity> WorkOrderActivities => Set<WorkOrderActivity>();
    public DbSet<MaintenanceReport> MaintenanceReports => Set<MaintenanceReport>();
    public DbSet<MaintenanceReportReview> MaintenanceReportReviews => Set<MaintenanceReportReview>();
    public DbSet<PreventiveMaintenancePlan> PreventiveMaintenancePlans => Set<PreventiveMaintenancePlan>();
    public DbSet<PreventiveMaintenanceChecklistItem> PreventiveMaintenanceChecklistItems => Set<PreventiveMaintenanceChecklistItem>();
    public DbSet<PreventiveMaintenanceOccurrence> PreventiveMaintenanceOccurrences => Set<PreventiveMaintenanceOccurrence>();
    public DbSet<PreventiveWorkOrderChecklistResponse> PreventiveWorkOrderChecklistResponses => Set<PreventiveWorkOrderChecklistResponse>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStockLocation> WarehouseStockLocations => Set<WarehouseStockLocation>();
    public DbSet<SparePart> SpareParts => Set<SparePart>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<StockReceipt> StockReceipts => Set<StockReceipt>();
    public DbSet<StockIssue> StockIssues => Set<StockIssue>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<WorkOrderPartConsumption> WorkOrderPartConsumptions => Set<WorkOrderPartConsumption>();
    public DbSet<ProcurementDocumentSequence> ProcurementDocumentSequences => Set<ProcurementDocumentSequence>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierPart> SupplierParts => Set<SupplierPart>();
    public DbSet<PurchaseRequisition> PurchaseRequisitions => Set<PurchaseRequisition>();
    public DbSet<PurchaseRequisitionLine> PurchaseRequisitionLines => Set<PurchaseRequisitionLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<PurchaseReceiptLine> PurchaseReceiptLines => Set<PurchaseReceiptLine>();
    public DbSet<ProcurementActivity> ProcurementActivities => Set<ProcurementActivity>();
    public DbSet<ServiceRequestCategory> ServiceRequestCategories => Set<ServiceRequestCategory>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ServiceRequestAssignment> ServiceRequestAssignments => Set<ServiceRequestAssignment>();
    public DbSet<ServiceRequestNote> ServiceRequestNotes => Set<ServiceRequestNote>();
    public DbSet<ServiceRequestActivity> ServiceRequestActivities => Set<ServiceRequestActivity>();

    private Guid? CurrentTenantId => currentTenant.Id;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Tenant>(entity =>
        {
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(200);
            entity.Property(x => x.Slug).HasMaxLength(100);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        builder.Entity<PlatformUser>(entity =>
        {
            entity.Property(x => x.DisplayName).HasMaxLength(200);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        builder.Entity<UserSession>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.RevokedAtUtc, x.ExpiresAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.UserId });
            entity.Property(x => x.RefreshTokenHash).HasMaxLength(100);
            entity.Property(x => x.RevocationReason).HasMaxLength(200);
            entity.Property(x => x.UserAgent).HasMaxLength(500);
            entity.Property(x => x.IpAddress).HasMaxLength(64);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CompanyProfile>(entity =>
        {
            entity.HasIndex(x => x.TenantId).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.NameEn).HasMaxLength(200);
            entity.Property(x => x.NameAr).HasMaxLength(200);
            entity.Property(x => x.TimeZone).HasMaxLength(100);
            entity.Property(x => x.DefaultLanguage).HasMaxLength(5);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<Tenant>().WithOne().HasForeignKey<CompanyProfile>(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Site>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.NameEn).HasMaxLength(200);
            entity.Property(x => x.NameAr).HasMaxLength(200);
            entity.Property(x => x.Latitude).HasPrecision(9, 6);
            entity.Property(x => x.Longitude).HasPrecision(9, 6);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LocationType>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.NameEn).HasMaxLength(120);
            entity.Property(x => x.NameAr).HasMaxLength(120);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LocationNode>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.SiteId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.SiteId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.SiteId, x.ParentLocationId, x.SortOrder });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(80);
            entity.Property(x => x.NameEn).HasMaxLength(200);
            entity.Property(x => x.NameAr).HasMaxLength(200);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Site).WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LocationType).WithMany().HasForeignKey(x => new { x.TenantId, x.LocationTypeId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Parent).WithMany(x => x.Children)
                .HasForeignKey(x => new { x.TenantId, x.SiteId, x.ParentLocationId })
                .HasPrincipalKey(x => new { x.TenantId, x.SiteId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditEvent>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.OccurredAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Action).HasMaxLength(100);
            entity.Property(x => x.EntityType).HasMaxLength(100);
        });

        builder.Entity<AssetCategory>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.NameEn).HasMaxLength(160);
            entity.Property(x => x.NameAr).HasMaxLength(160);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        builder.Entity<Manufacturer>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("code IS NOT NULL");
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.NormalizedName).HasMaxLength(160);
            entity.Property(x => x.Code).HasMaxLength(50);
            entity.Property(x => x.Country).HasMaxLength(100);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        builder.Entity<AssetModel>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.ManufacturerId, x.NormalizedName }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Name).HasMaxLength(160);
            entity.Property(x => x.NormalizedName).HasMaxLength(160);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Manufacturer).WithMany().HasForeignKey(x => new { x.TenantId, x.ManufacturerId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => new { x.TenantId, x.CategoryId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Asset>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.SiteId, x.OperationalStatus, x.LifecycleState });
            entity.HasIndex(x => new { x.TenantId, x.CategoryId });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(80);
            entity.Property(x => x.NameEn).HasMaxLength(200);
            entity.Property(x => x.NameAr).HasMaxLength(200);
            entity.Property(x => x.SerialNumber).HasMaxLength(160);
            entity.Property(x => x.Barcode).HasMaxLength(160);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.PurchaseCost).HasPrecision(18, 2);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => new { x.TenantId, x.CategoryId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Site).WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Location).WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId, x.LocationId })
                .HasPrincipalKey(x => new { x.TenantId, x.SiteId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Manufacturer).WithMany().HasForeignKey(x => new { x.TenantId, x.ManufacturerId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Model).WithMany().HasForeignKey(x => new { x.TenantId, x.ModelId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResponsibleUser).WithMany().HasForeignKey(x => x.ResponsibleUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AssetActivity>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.AssetId, x.OccurredAtUtc });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Action).HasMaxLength(100);
            entity.HasOne(x => x.Asset).WithMany().HasForeignKey(x => new { x.TenantId, x.AssetId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BreakdownReport>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.SiteId, x.Status, x.Priority });
            entity.HasIndex(x => new { x.TenantId, x.AssetId, x.ReportedAtUtc });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.LocationSnapshot).HasMaxLength(300);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Asset).WithMany().HasForeignKey(x => new { x.TenantId, x.AssetId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SaaSPlatform.Api.Modules.Locations.Domain.Site>().WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ReportedBy).WithMany().HasForeignKey(x => x.ReportedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BreakdownReview>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.BreakdownReportId, x.ReviewedAtUtc });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.HasOne(x => x.BreakdownReport).WithMany(x => x.Reviews).HasForeignKey(x => new { x.TenantId, x.BreakdownReportId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<CorrectiveWorkOrder>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.BreakdownReportId }).IsUnique().HasFilter("breakdown_report_id IS NOT NULL");
            entity.HasIndex(x => new { x.TenantId, x.PreventiveMaintenanceOccurrenceId }).IsUnique().HasFilter("preventive_maintenance_occurrence_id IS NOT NULL");
            entity.HasIndex(x => new { x.TenantId, x.SiteId, x.Status, x.Priority });
            entity.HasIndex(x => new { x.TenantId, x.AssignedTechnicianId, x.Status });
            entity.HasIndex(x => new { x.TenantId, x.AssetId, x.CreatedAtUtc });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Number).HasMaxLength(32);
            entity.Property(x => x.LocationSnapshot).HasMaxLength(300);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.BreakdownReport).WithOne(x => x.WorkOrder).HasForeignKey<CorrectiveWorkOrder>(x => new { x.TenantId, x.BreakdownReportId })
                .HasPrincipalKey<BreakdownReport>(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Asset).WithMany().HasForeignKey(x => new { x.TenantId, x.AssetId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SaaSPlatform.Api.Modules.Locations.Domain.Site>().WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedTechnician).WithMany().HasForeignKey(x => x.AssignedTechnicianId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedBy).WithMany().HasForeignKey(x => x.AssignedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PreventiveMaintenancePlan).WithMany().HasForeignKey(x => new { x.TenantId, x.PreventiveMaintenancePlanId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkOrderAssignment>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.WorkOrderId, x.AssignedAtUtc });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.HasOne(x => x.WorkOrder).WithMany(x => x.Assignments).HasForeignKey(x => new { x.TenantId, x.WorkOrderId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Technician).WithMany().HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedBy).WithMany().HasForeignKey(x => x.AssignedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkOrderActivity>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.WorkOrderId, x.OccurredAtUtc });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Action).HasMaxLength(100);
            entity.HasOne(x => x.WorkOrder).WithMany(x => x.Activities).HasForeignKey(x => new { x.TenantId, x.WorkOrderId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MaintenanceReport>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.WorkOrderId }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.WorkOrder).WithOne(x => x.MaintenanceReport).HasForeignKey<MaintenanceReport>(x => new { x.TenantId, x.WorkOrderId })
                .HasPrincipalKey<CorrectiveWorkOrder>(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Asset).WithMany().HasForeignKey(x => new { x.TenantId, x.AssetId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Technician).WithMany().HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MaintenanceReportReview>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.MaintenanceReportId, x.ReviewedAtUtc });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.HasOne(x => x.MaintenanceReport).WithMany(x => x.Reviews).HasForeignKey(x => new { x.TenantId, x.MaintenanceReportId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Reviewer).WithMany().HasForeignKey(x => x.ReviewerId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PreventiveMaintenancePlan>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.SiteId, x.State, x.NextDueAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.AssetId, x.State });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(80);
            entity.Property(x => x.NameEn).HasMaxLength(200);
            entity.Property(x => x.NameAr).HasMaxLength(200);
            entity.Property(x => x.TimeZoneId).HasMaxLength(100);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Asset).WithMany().HasForeignKey(x => new { x.TenantId, x.AssetId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SaaSPlatform.Api.Modules.Locations.Domain.Site>().WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.DefaultTechnician).WithMany().HasForeignKey(x => x.DefaultTechnicianId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PreventiveMaintenanceChecklistItem>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PlanId, x.Sequence }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.InstructionEn).HasMaxLength(500);
            entity.Property(x => x.InstructionAr).HasMaxLength(500);
            entity.Property(x => x.Guidance).HasMaxLength(500);
            entity.HasOne(x => x.Plan).WithMany(x => x.ChecklistItems).HasForeignKey(x => new { x.TenantId, x.PlanId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PreventiveMaintenanceOccurrence>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.PlanId, x.ScheduledDueAtUtc }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.ScheduledDueAtUtc, x.Status });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Plan).WithMany(x => x.Occurrences).HasForeignKey(x => new { x.TenantId, x.PlanId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.WorkOrder).WithOne(x => x.PreventiveMaintenanceOccurrence)
                .HasForeignKey<CorrectiveWorkOrder>(x => new { x.TenantId, x.PreventiveMaintenanceOccurrenceId })
                .HasPrincipalKey<PreventiveMaintenanceOccurrence>(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PreventiveWorkOrderChecklistResponse>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.WorkOrderId, x.Sequence }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.InstructionEn).HasMaxLength(500);
            entity.Property(x => x.InstructionAr).HasMaxLength(500);
            entity.Property(x => x.Guidance).HasMaxLength(500);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne(x => x.WorkOrder).WithMany(x => x.ChecklistResponses).HasForeignKey(x => new { x.TenantId, x.WorkOrderId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.RecordedBy).WithMany().HasForeignKey(x => x.RecordedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Warehouse>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.SiteId, x.IsActive });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(50); entity.Property(x => x.NameEn).HasMaxLength(200); entity.Property(x => x.NameAr).HasMaxLength(200); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Site).WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WarehouseStockLocation>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.WarehouseId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.WarehouseId, x.Code }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.NameEn).HasMaxLength(200); entity.Property(x => x.NameAr).HasMaxLength(200); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Warehouse).WithMany(x => x.Locations).HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => new { x.TenantId, x.WarehouseId, x.ParentLocationId }).HasPrincipalKey(x => new { x.TenantId, x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<SparePart>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.Barcode }).IsUnique().HasFilter("barcode IS NOT NULL");
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Sku).HasMaxLength(80); entity.Property(x => x.NameEn).HasMaxLength(200); entity.Property(x => x.NameAr).HasMaxLength(200); entity.Property(x => x.Category).HasMaxLength(120); entity.Property(x => x.Manufacturer).HasMaxLength(160); entity.Property(x => x.ManufacturerPartNumber).HasMaxLength(120); entity.Property(x => x.Barcode).HasMaxLength(160); entity.Property(x => x.PreferredSupplier).HasMaxLength(200); entity.Property(x => x.MinimumStock).HasPrecision(18, 4); entity.Property(x => x.ReorderPoint).HasPrecision(18, 4); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.ToTable(t => t.HasCheckConstraint("ck_spare_parts_thresholds_nonnegative", "minimum_stock >= 0 AND reorder_point >= 0"));
        });

        builder.Entity<InventoryBalance>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.WarehouseId, x.StockLocationId, x.PartId }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.PartId, x.WarehouseId });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.QuantityOnHand).HasPrecision(18, 4); entity.Property(x => x.QuantityReserved).HasPrecision(18, 4); entity.Property(x => x.AverageUnitCost).HasPrecision(18, 4); entity.Property(x => x.Version).IsConcurrencyToken(); entity.Ignore(x => x.QuantityAvailable); entity.Ignore(x => x.InventoryValue);
            entity.ToTable(t => t.HasCheckConstraint("ck_inventory_balances_nonnegative", "quantity_on_hand >= 0 AND quantity_reserved >= 0 AND quantity_reserved <= quantity_on_hand AND average_unit_cost >= 0"));
            entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.StockLocation).WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId, x.StockLocationId }).HasPrincipalKey(x => new { x.TenantId, x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Part).WithMany().HasForeignKey(x => new { x.TenantId, x.PartId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<InventoryMovement>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PartId, x.OccurredAtUtc }); entity.HasIndex(x => new { x.TenantId, x.WarehouseId, x.OccurredAtUtc }); entity.HasIndex(x => new { x.TenantId, x.ReferenceType, x.ReferenceId });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.Quantity).HasPrecision(18, 4); entity.Property(x => x.UnitCost).HasPrecision(18, 4); entity.Property(x => x.TotalValue).HasPrecision(18, 2); entity.Property(x => x.ReferenceType).HasMaxLength(80); entity.Property(x => x.ReferenceNumber).HasMaxLength(100);
            entity.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.StockLocation).WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId, x.StockLocationId }).HasPrincipalKey(x => new { x.TenantId, x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Part).WithMany().HasForeignKey(x => new { x.TenantId, x.PartId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StockReceipt>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id }); entity.HasIndex(x => new { x.TenantId, x.RequestId }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.ReferenceNumber }); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.Quantity).HasPrecision(18, 4); entity.Property(x => x.UnitCost).HasPrecision(18, 4); entity.Property(x => x.TotalCost).HasPrecision(18, 2); entity.Property(x => x.ReferenceNumber).HasMaxLength(100); entity.ToTable(t => t.HasCheckConstraint("ck_stock_receipts_positive", "quantity > 0 AND unit_cost >= 0 AND total_cost >= 0"));
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<WarehouseStockLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId, x.StockLocationId }).HasPrincipalKey(x => new { x.TenantId, x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<SparePart>().WithMany().HasForeignKey(x => new { x.TenantId, x.PartId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.ReceivedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StockIssue>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.RequestId }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.WorkOrderId }); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.Quantity).HasPrecision(18, 4); entity.Property(x => x.UnitCost).HasPrecision(18, 4); entity.Property(x => x.TotalCost).HasPrecision(18, 2); entity.Property(x => x.ReferenceNumber).HasMaxLength(100); entity.ToTable(t => t.HasCheckConstraint("ck_stock_issues_positive", "quantity > 0 AND unit_cost >= 0 AND total_cost >= 0"));
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<WarehouseStockLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId, x.StockLocationId }).HasPrincipalKey(x => new { x.TenantId, x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<SparePart>().WithMany().HasForeignKey(x => new { x.TenantId, x.PartId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<CorrectiveWorkOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.WorkOrderId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<StockReservation>().WithMany().HasForeignKey(x => new { x.TenantId, x.ReservationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.IssuedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StockTransfer>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.RequestId }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.ReferenceNumber }); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.Quantity).HasPrecision(18, 4); entity.Property(x => x.UnitCost).HasPrecision(18, 4); entity.Property(x => x.TotalValue).HasPrecision(18, 2); entity.Property(x => x.Version).IsConcurrencyToken(); entity.ToTable(t => t.HasCheckConstraint("ck_stock_transfers_positive", "quantity > 0 AND unit_cost >= 0 AND total_value >= 0"));
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourceWarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.DestinationWarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<WarehouseStockLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.SourceWarehouseId, x.SourceLocationId }).HasPrincipalKey(x => new { x.TenantId, x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<WarehouseStockLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.DestinationWarehouseId, x.DestinationLocationId }).HasPrincipalKey(x => new { x.TenantId, x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<SparePart>().WithMany().HasForeignKey(x => new { x.TenantId, x.PartId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.PerformedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StockAdjustment>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.RequestId }).IsUnique(); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.PreviousQuantity).HasPrecision(18, 4); entity.Property(x => x.AdjustmentQuantity).HasPrecision(18, 4); entity.Property(x => x.NewQuantity).HasPrecision(18, 4); entity.ToTable(t => t.HasCheckConstraint("ck_stock_adjustments_nonnegative", "new_quantity >= 0 AND adjustment_quantity <> 0"));
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<WarehouseStockLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId, x.StockLocationId }).HasPrincipalKey(x => new { x.TenantId, x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<SparePart>().WithMany().HasForeignKey(x => new { x.TenantId, x.PartId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.PerformedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<StockReservation>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id }); entity.HasIndex(x => new { x.TenantId, x.RequestId }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.WorkOrderId, x.State }); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.ReservedQuantity).HasPrecision(18, 4); entity.Property(x => x.ConsumedQuantity).HasPrecision(18, 4); entity.Property(x => x.Version).IsConcurrencyToken(); entity.ToTable(t => t.HasCheckConstraint("ck_stock_reservations_quantities", "reserved_quantity > 0 AND consumed_quantity >= 0 AND consumed_quantity <= reserved_quantity")); entity.HasOne(x => x.WorkOrder).WithMany().HasForeignKey(x => new { x.TenantId, x.WorkOrderId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Part).WithMany().HasForeignKey(x => new { x.TenantId, x.PartId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<WarehouseStockLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId, x.StockLocationId }).HasPrincipalKey(x => new { x.TenantId, x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<WorkOrderPartConsumption>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.RequestId }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.WorkOrderId, x.ConsumedAtUtc }); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.Quantity).HasPrecision(18, 4); entity.Property(x => x.UnitCost).HasPrecision(18, 4); entity.Property(x => x.TotalCost).HasPrecision(18, 2); entity.ToTable(t => t.HasCheckConstraint("ck_work_order_consumptions_positive", "quantity > 0 AND unit_cost >= 0 AND total_cost >= 0")); entity.HasOne(x => x.WorkOrder).WithMany().HasForeignKey(x => new { x.TenantId, x.WorkOrderId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Part).WithMany().HasForeignKey(x => new { x.TenantId, x.PartId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<StockReservation>().WithMany().HasForeignKey(x => new { x.TenantId, x.ReservationId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<WarehouseStockLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId, x.StockLocationId }).HasPrincipalKey(x => new { x.TenantId, x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.ConsumedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProcurementDocumentSequence>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.DocumentType, x.Year }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.DocumentType).HasMaxLength(10); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.ToTable(t => t.HasCheckConstraint("ck_procurement_sequences_positive", "next_value > 0"));
        });

        builder.Entity<Supplier>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id }); entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.Property(x => x.Code).HasMaxLength(80); entity.Property(x => x.NameEn).HasMaxLength(200); entity.Property(x => x.NameAr).HasMaxLength(200); entity.Property(x => x.Email).HasMaxLength(320); entity.Property(x => x.Phone).HasMaxLength(50); entity.Property(x => x.AlternatePhone).HasMaxLength(50); entity.Property(x => x.TaxNumber).HasMaxLength(80); entity.Property(x => x.CommercialRegistrationNumber).HasMaxLength(80); entity.Property(x => x.City).HasMaxLength(120); entity.Property(x => x.Country).HasMaxLength(120); entity.Property(x => x.Website).HasMaxLength(300); entity.Property(x => x.DefaultCurrency).HasMaxLength(3);
        });

        builder.Entity<SupplierPart>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id }); entity.HasIndex(x => new { x.TenantId, x.SupplierId, x.PartId }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.PartId }).IsUnique().HasFilter("is_preferred");
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.Version).IsConcurrencyToken(); entity.Property(x => x.LastUnitPrice).HasPrecision(18, 4); entity.Property(x => x.MinimumOrderQuantity).HasPrecision(18, 4); entity.Property(x => x.Currency).HasMaxLength(3); entity.Property(x => x.SupplierPartNumber).HasMaxLength(120);
            entity.ToTable(t => t.HasCheckConstraint("ck_supplier_parts_values", "last_unit_price IS NULL OR last_unit_price >= 0 AND minimum_order_quantity > 0 AND lead_time_days >= 0"));
            entity.HasOne(x => x.Supplier).WithMany(x => x.Parts).HasForeignKey(x => new { x.TenantId, x.SupplierId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Part).WithMany().HasForeignKey(x => new { x.TenantId, x.PartId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseRequisition>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id }); entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.SiteId, x.Status }); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.Number).HasMaxLength(30); entity.Property(x => x.Version).IsConcurrencyToken(); entity.Property(x => x.BusinessJustification).HasMaxLength(1000);
            entity.HasOne(x => x.Site).WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.TargetWarehouse).WithMany().HasForeignKey(x => new { x.TenantId, x.TargetWarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.RequestedBy).WithMany().HasForeignKey(x => x.RequestedById).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseRequisitionLine>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id }); entity.HasIndex(x => new { x.TenantId, x.RequisitionId, x.PartId }).IsUnique(); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.RequestedQuantity).HasPrecision(18, 4); entity.Property(x => x.ConvertedQuantity).HasPrecision(18, 4); entity.Property(x => x.EstimatedUnitPrice).HasPrecision(18, 4); entity.Property(x => x.EstimatedTotal).HasPrecision(18, 2); entity.ToTable(t => t.HasCheckConstraint("ck_requisition_lines_quantities", "requested_quantity > 0 AND converted_quantity >= 0 AND converted_quantity <= requested_quantity AND (estimated_unit_price IS NULL OR estimated_unit_price >= 0) AND estimated_total >= 0"));
            entity.HasOne(x => x.Requisition).WithMany(x => x.Lines).HasForeignKey(x => new { x.TenantId, x.RequisitionId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Part).WithMany().HasForeignKey(x => new { x.TenantId, x.PartId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.SuggestedSupplier).WithMany().HasForeignKey(x => new { x.TenantId, x.SuggestedSupplierId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id }); entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.SiteId, x.Status }); entity.HasIndex(x => new { x.TenantId, x.SupplierId, x.Status }); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.Number).HasMaxLength(30); entity.Property(x => x.Currency).HasMaxLength(3); entity.Property(x => x.Subtotal).HasPrecision(18, 2); entity.Property(x => x.Version).IsConcurrencyToken(); entity.ToTable(t => t.HasCheckConstraint("ck_purchase_orders_subtotal", "subtotal >= 0"));
            entity.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => new { x.TenantId, x.SupplierId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Site).WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.DestinationWarehouse).WithMany().HasForeignKey(x => new { x.TenantId, x.DestinationWarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict); entity.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.IssuedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id }); entity.HasIndex(x => new { x.TenantId, x.PurchaseOrderId }); entity.HasIndex(x => new { x.TenantId, x.SourceRequisitionLineId }); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.OrderedQuantity).HasPrecision(18, 4); entity.Property(x => x.ReceivedQuantity).HasPrecision(18, 4); entity.Property(x => x.UnitPrice).HasPrecision(18, 4); entity.Property(x => x.LineSubtotal).HasPrecision(18, 2); entity.ToTable(t => t.HasCheckConstraint("ck_purchase_order_lines_quantities", "ordered_quantity > 0 AND received_quantity >= 0 AND received_quantity <= ordered_quantity AND unit_price >= 0 AND line_subtotal >= 0"));
            entity.HasOne(x => x.PurchaseOrder).WithMany(x => x.Lines).HasForeignKey(x => new { x.TenantId, x.PurchaseOrderId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.Part).WithMany().HasForeignKey(x => new { x.TenantId, x.PartId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.SourceRequisitionLine).WithMany().HasForeignKey(x => new { x.TenantId, x.SourceRequisitionLineId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseReceipt>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id }); entity.HasIndex(x => new { x.TenantId, x.RequestId }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.ReceiptNumber }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.PurchaseOrderId, x.ReceiptDate }); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.ReceiptNumber).HasMaxLength(100); entity.Property(x => x.TotalCost).HasPrecision(18, 2); entity.ToTable(t => t.HasCheckConstraint("ck_purchase_receipts_total", "total_cost >= 0"));
            entity.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => new { x.TenantId, x.PurchaseOrderId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<WarehouseStockLocation>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId, x.StockLocationId }).HasPrincipalKey(x => new { x.TenantId, x.WarehouseId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<PlatformUser>().WithMany().HasForeignKey(x => x.ReceivedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseReceiptLine>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.PurchaseReceiptId, x.PurchaseOrderLineId }).IsUnique(); entity.HasIndex(x => new { x.TenantId, x.StockReceiptId }).IsUnique(); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.Quantity).HasPrecision(18, 4); entity.Property(x => x.UnitCost).HasPrecision(18, 4); entity.Property(x => x.TotalCost).HasPrecision(18, 2); entity.ToTable(t => t.HasCheckConstraint("ck_purchase_receipt_lines_values", "quantity > 0 AND unit_cost >= 0 AND total_cost >= 0"));
            entity.HasOne(x => x.PurchaseReceipt).WithMany(x => x.Lines).HasForeignKey(x => new { x.TenantId, x.PurchaseReceiptId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne(x => x.PurchaseOrderLine).WithMany().HasForeignKey(x => new { x.TenantId, x.PurchaseOrderLineId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict); entity.HasOne<StockReceipt>().WithMany().HasForeignKey(x => new { x.TenantId, x.StockReceiptId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProcurementActivity>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.EntityType, x.EntityId, x.OccurredAtUtc }); entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId); entity.Property(x => x.EntityType).HasMaxLength(80); entity.Property(x => x.Action).HasMaxLength(100); entity.Property(x => x.FromStatus).HasMaxLength(50); entity.Property(x => x.ToStatus).HasMaxLength(50); entity.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ServiceRequestCategory>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.IsActive, x.SortOrder });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(50); entity.Property(x => x.NameEn).HasMaxLength(160);
            entity.Property(x => x.NameAr).HasMaxLength(160); entity.Property(x => x.Version).IsConcurrencyToken();
        });

        builder.Entity<ServiceRequest>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.SiteId, x.Status, x.Priority });
            entity.HasIndex(x => new { x.TenantId, x.RequesterId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.AssignedToId, x.Status });
            entity.HasIndex(x => new { x.TenantId, x.AssetId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.LinkedBreakdownReportId }).IsUnique().HasFilter("linked_breakdown_report_id IS NOT NULL");
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Number).HasMaxLength(30); entity.Property(x => x.Title).HasMaxLength(200);
            entity.Property(x => x.RequesterContact).HasMaxLength(320); entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasOne(x => x.Site).WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Location).WithMany().HasForeignKey(x => new { x.TenantId, x.SiteId, x.LocationId }).HasPrincipalKey(x => new { x.TenantId, x.SiteId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Category).WithMany().HasForeignKey(x => new { x.TenantId, x.CategoryId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Asset).WithMany().HasForeignKey(x => new { x.TenantId, x.AssetId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.LinkedBreakdownReport).WithOne(x => x.OriginatingServiceRequest).HasForeignKey<ServiceRequest>(x => new { x.TenantId, x.LinkedBreakdownReportId }).HasPrincipalKey<BreakdownReport>(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Requester).WithMany().HasForeignKey(x => x.RequesterId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedTo).WithMany().HasForeignKey(x => x.AssignedToId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedBy).WithMany().HasForeignKey(x => x.AssignedById).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ResolvedBy).WithMany().HasForeignKey(x => x.ResolvedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ServiceRequestAssignment>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.ServiceRequestId, x.AssignedAtUtc });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.HasOne(x => x.ServiceRequest).WithMany(x => x.Assignments).HasForeignKey(x => new { x.TenantId, x.ServiceRequestId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.PreviousAssignee).WithMany().HasForeignKey(x => x.PreviousAssigneeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.AssignedBy).WithMany().HasForeignKey(x => x.AssignedById).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ServiceRequestNote>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.ServiceRequestId, x.CreatedAtUtc });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.HasOne(x => x.ServiceRequest).WithMany(x => x.Notes).HasForeignKey(x => new { x.TenantId, x.ServiceRequestId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ServiceRequestActivity>(entity =>
        {
            entity.HasIndex(x => new { x.TenantId, x.ServiceRequestId, x.OccurredAtUtc });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Action).HasMaxLength(100); entity.Property(x => x.FromStatus).HasMaxLength(50); entity.Property(x => x.ToStatus).HasMaxLength(50);
            entity.HasOne(x => x.ServiceRequest).WithMany(x => x.Activities).HasForeignKey(x => new { x.TenantId, x.ServiceRequestId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TenantMembership>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Version).IsConcurrencyToken();
        });

        builder.Entity<TenantRole>(entity =>
        {
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.Version).IsConcurrencyToken();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
        });

        builder.Entity<PermissionDefinition>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(150);
            entity.Property(x => x.Module).HasMaxLength(80);
        });

        builder.Entity<RolePermissionGrant>(entity =>
        {
            entity.HasKey(x => new { x.TenantId, x.RoleId, x.PermissionId });
            entity.HasIndex(x => new { x.TenantId, x.RoleId });
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.HasOne(x => x.Role)
                .WithMany(x => x.Permissions)
                .HasForeignKey(x => new { x.TenantId, x.RoleId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id });
        });

        builder.Entity<MembershipRole>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.MembershipId, x.RoleId, x.ScopeKind, x.ScopeId }).IsUnique();
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.HasOne(x => x.Membership)
                .WithMany(x => x.Roles)
                .HasForeignKey(x => new { x.TenantId, x.MembershipId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id });
            entity.HasOne(x => x.Role)
                .WithMany()
                .HasForeignKey(x => new { x.TenantId, x.RoleId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id });
        });

        builder.UseSnakeCaseNames();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyConventions();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        ApplyConventions();
        return base.SaveChanges();
    }

    private void ApplyConventions()
    {
        var now = timeProvider.GetUtcNow();
        foreach (var entry in ChangeTracker.Entries<IHasTimestamps>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAtUtc = now;
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ITenantOwned>()
                     .Where(x => x.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (CurrentTenantId is null)
            {
                throw new InvalidOperationException("A tenant context is required to change tenant-owned data.");
            }

            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = CurrentTenantId.Value;
            }

            if (entry.Entity.TenantId != CurrentTenantId)
            {
                throw new InvalidOperationException("Cross-tenant data changes are not allowed.");
            }
        }
    }
}
