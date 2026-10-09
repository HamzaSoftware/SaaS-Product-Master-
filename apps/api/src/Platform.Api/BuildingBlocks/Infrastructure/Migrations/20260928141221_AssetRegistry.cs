using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSPlatform.Api.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssetRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, module, description) VALUES
                ('40000000-0000-0000-0000-000000000001', 'assets.view', 'Assets', 'View assets and asset history'),
                ('40000000-0000-0000-0000-000000000002', 'assets.create', 'Assets', 'Register assets'),
                ('40000000-0000-0000-0000-000000000003', 'assets.update', 'Assets', 'Update asset details'),
                ('40000000-0000-0000-0000-000000000004', 'assets.manage-master-data', 'Assets', 'Manage asset categories, manufacturers, and models'),
                ('40000000-0000-0000-0000-000000000005', 'assets.change-status', 'Assets', 'Change asset operational status'),
                ('40000000-0000-0000-0000-000000000006', 'assets.relocate', 'Assets', 'Relocate assets between authorized sites and locations'),
                ('40000000-0000-0000-0000-000000000007', 'assets.retire', 'Assets', 'Retire and reactivate assets')
                ON CONFLICT (code) DO NOTHING;

                INSERT INTO role_permissions (tenant_id, role_id, permission_id)
                SELECT r.tenant_id, r.id, p.id
                FROM tenant_roles r
                CROSS JOIN permissions p
                WHERE r.is_system = TRUE
                  AND r.name = 'Tenant Administrator'
                  AND p.code LIKE 'assets.%'
                ON CONFLICT DO NOTHING;

                UPDATE tenant_memberships m
                SET authorization_version = gen_random_uuid()
                WHERE EXISTS (
                    SELECT 1 FROM membership_roles mr
                    JOIN tenant_roles r ON r.tenant_id = mr.tenant_id AND r.id = mr.role_id
                    WHERE mr.tenant_id = m.tenant_id AND mr.membership_id = m.id
                      AND r.is_system = TRUE AND r.name = 'Tenant Administrator');
                """);

            migrationBuilder.CreateTable(
                name: "asset_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_en = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_categories", x => x.id);
                    table.UniqueConstraint("ak_asset_categories_tenant_id_id", x => new { x.tenant_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "manufacturers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_manufacturers", x => x.id);
                    table.UniqueConstraint("ak_manufacturers_tenant_id_id", x => new { x.tenant_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "asset_models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    technical_description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_models", x => x.id);
                    table.UniqueConstraint("ak_asset_models_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_asset_models_asset_categories_tenant_id_category_id",
                        columns: x => new { x.tenant_id, x.category_id },
                        principalTable: "asset_categories",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_models_manufacturers_tenant_id_manufacturer_id",
                        columns: x => new { x.tenant_id, x.manufacturer_id },
                        principalTable: "manufacturers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    manufacturer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    model_id = table.Column<Guid>(type: "uuid", nullable: true),
                    serial_number = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    barcode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    purchase_date = table.Column<DateOnly>(type: "date", nullable: true),
                    purchase_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    installation_date = table.Column<DateOnly>(type: "date", nullable: true),
                    warranty_start_date = table.Column<DateOnly>(type: "date", nullable: true),
                    warranty_expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    supplier_name = table.Column<string>(type: "text", nullable: true),
                    supplier_reference = table.Column<string>(type: "text", nullable: true),
                    responsible_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    operational_status = table.Column<int>(type: "integer", nullable: false),
                    lifecycle_state = table.Column<int>(type: "integer", nullable: false),
                    retired_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assets", x => x.id);
                    table.UniqueConstraint("ak_assets_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_assets_asp_net_users_responsible_user_id",
                        column: x => x.responsible_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assets_asset_categories_tenant_id_category_id",
                        columns: x => new { x.tenant_id, x.category_id },
                        principalTable: "asset_categories",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assets_asset_models_tenant_id_model_id",
                        columns: x => new { x.tenant_id, x.model_id },
                        principalTable: "asset_models",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assets_location_nodes_tenant_id_site_id_location_id",
                        columns: x => new { x.tenant_id, x.site_id, x.location_id },
                        principalTable: "location_nodes",
                        principalColumns: new[] { "tenant_id", "site_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assets_manufacturers_tenant_id_manufacturer_id",
                        columns: x => new { x.tenant_id, x.manufacturer_id },
                        principalTable: "manufacturers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_assets_sites_tenant_id_site_id",
                        columns: x => new { x.tenant_id, x.site_id },
                        principalTable: "sites",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asset_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    changes_json = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<string>(type: "text", nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_activities", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_activities_asp_net_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asset_activities_assets_tenant_id_asset_id",
                        columns: x => new { x.tenant_id, x.asset_id },
                        principalTable: "assets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_activities_actor_user_id",
                table: "asset_activities",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asset_activities_tenant_id_asset_id_occurred_at_utc",
                table: "asset_activities",
                columns: new[] { "tenant_id", "asset_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_categories_tenant_id_code",
                table: "asset_categories",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_models_tenant_id_category_id",
                table: "asset_models",
                columns: new[] { "tenant_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_models_tenant_id_manufacturer_id_normalized_name",
                table: "asset_models",
                columns: new[] { "tenant_id", "manufacturer_id", "normalized_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assets_responsible_user_id",
                table: "assets",
                column: "responsible_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_category_id",
                table: "assets",
                columns: new[] { "tenant_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_code",
                table: "assets",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_manufacturer_id",
                table: "assets",
                columns: new[] { "tenant_id", "manufacturer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_model_id",
                table: "assets",
                columns: new[] { "tenant_id", "model_id" });

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_site_id_location_id",
                table: "assets",
                columns: new[] { "tenant_id", "site_id", "location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_assets_tenant_id_site_id_operational_status_lifecycle_state",
                table: "assets",
                columns: new[] { "tenant_id", "site_id", "operational_status", "lifecycle_state" });

            migrationBuilder.CreateIndex(
                name: "ix_manufacturers_tenant_id_code",
                table: "manufacturers",
                columns: new[] { "tenant_id", "code" },
                unique: true,
                filter: "code IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_manufacturers_tenant_id_normalized_name",
                table: "manufacturers",
                columns: new[] { "tenant_id", "normalized_name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_activities");

            migrationBuilder.DropTable(
                name: "assets");

            migrationBuilder.DropTable(
                name: "asset_models");

            migrationBuilder.DropTable(
                name: "asset_categories");

            migrationBuilder.DropTable(
                name: "manufacturers");

            migrationBuilder.Sql("""
                DELETE FROM role_permissions WHERE permission_id IN
                    (SELECT id FROM permissions WHERE code LIKE 'assets.%');
                DELETE FROM permissions WHERE code IN
                    ('assets.view', 'assets.create', 'assets.update', 'assets.manage-master-data',
                     'assets.change-status', 'assets.relocate', 'assets.retire');
                """);
        }
    }
}
