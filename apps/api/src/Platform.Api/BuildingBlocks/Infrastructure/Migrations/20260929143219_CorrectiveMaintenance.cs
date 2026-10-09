using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSPlatform.Api.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CorrectiveMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, module, description) VALUES
                ('50000000-0000-0000-0000-000000000001', 'maintenance.breakdowns.view', 'Maintenance', 'View breakdown reports'),
                ('50000000-0000-0000-0000-000000000002', 'maintenance.breakdowns.create', 'Maintenance', 'Report asset breakdowns'),
                ('50000000-0000-0000-0000-000000000003', 'maintenance.breakdowns.review', 'Maintenance', 'Approve or reject breakdown reports'),
                ('50000000-0000-0000-0000-000000000004', 'maintenance.workorders.view', 'Maintenance', 'View corrective work orders'),
                ('50000000-0000-0000-0000-000000000005', 'maintenance.workorders.create', 'Maintenance', 'Create corrective work orders'),
                ('50000000-0000-0000-0000-000000000006', 'maintenance.workorders.assign', 'Maintenance', 'Assign corrective work orders'),
                ('50000000-0000-0000-0000-000000000007', 'maintenance.workorders.execute', 'Maintenance', 'Execute assigned corrective work'),
                ('50000000-0000-0000-0000-000000000008', 'maintenance.workorders.verify', 'Maintenance', 'Verify maintenance reports and close work orders'),
                ('50000000-0000-0000-0000-000000000009', 'maintenance.workorders.cancel', 'Maintenance', 'Cancel corrective work orders')
                ON CONFLICT (code) DO NOTHING;

                INSERT INTO role_permissions (tenant_id, role_id, permission_id)
                SELECT r.tenant_id, r.id, p.id
                FROM tenant_roles r CROSS JOIN permissions p
                WHERE r.is_system = TRUE AND r.name = 'Tenant Administrator' AND p.code LIKE 'maintenance.%'
                ON CONFLICT DO NOTHING;

                UPDATE tenant_memberships m SET authorization_version = gen_random_uuid()
                WHERE EXISTS (
                    SELECT 1 FROM membership_roles mr
                    JOIN tenant_roles r ON r.tenant_id = mr.tenant_id AND r.id = mr.role_id
                    WHERE mr.tenant_id = m.tenant_id AND mr.membership_id = m.id
                      AND r.is_system = TRUE AND r.name = 'Tenant Administrator');
                """);

            migrationBuilder.CreateTable(
                name: "breakdown_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_snapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    reported_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reported_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    observed_symptoms = table.Column<string>(type: "text", nullable: true),
                    downtime_started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_breakdown_reports", x => x.id);
                    table.UniqueConstraint("ak_breakdown_reports_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_breakdown_reports_asp_net_users_reported_by_id",
                        column: x => x.reported_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_breakdown_reports_assets_tenant_id_asset_id",
                        columns: x => new { x.tenant_id, x.asset_id },
                        principalTable: "assets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_breakdown_reports_sites_tenant_id_site_id",
                        columns: x => new { x.tenant_id, x.site_id },
                        principalTable: "sites",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "breakdown_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    breakdown_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    approved = table.Column<bool>(type: "boolean", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_breakdown_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_breakdown_reviews_asp_net_users_reviewer_id",
                        column: x => x.reviewer_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_breakdown_reviews_breakdown_reports_tenant_id_breakdown_rep~",
                        columns: x => new { x.tenant_id, x.breakdown_report_id },
                        principalTable: "breakdown_reports",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "corrective_work_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    breakdown_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_snapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    assigned_technician_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    planned_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_completion_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_corrective_work_orders", x => x.id);
                    table.UniqueConstraint("ak_corrective_work_orders_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_corrective_work_orders_asp_net_users_assigned_by_id",
                        column: x => x.assigned_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_corrective_work_orders_asp_net_users_assigned_technician_id",
                        column: x => x.assigned_technician_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_corrective_work_orders_assets_tenant_id_asset_id",
                        columns: x => new { x.tenant_id, x.asset_id },
                        principalTable: "assets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_corrective_work_orders_breakdown_reports_tenant_id_breakdow~",
                        columns: x => new { x.tenant_id, x.breakdown_report_id },
                        principalTable: "breakdown_reports",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_corrective_work_orders_sites_tenant_id_site_id",
                        columns: x => new { x.tenant_id, x.site_id },
                        principalTable: "sites",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_reports",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    technician_id = table.Column<Guid>(type: "uuid", nullable: false),
                    diagnosis = table.Column<string>(type: "text", nullable: true),
                    work_performed = table.Column<string>(type: "text", nullable: true),
                    repair_outcome = table.Column<int>(type: "integer", nullable: true),
                    actual_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    labor_minutes = table.Column<int>(type: "integer", nullable: true),
                    additional_notes = table.Column<string>(type: "text", nullable: true),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_reports", x => x.id);
                    table.UniqueConstraint("ak_maintenance_reports_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_maintenance_reports_asp_net_users_technician_id",
                        column: x => x.technician_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_reports_assets_tenant_id_asset_id",
                        columns: x => new { x.tenant_id, x.asset_id },
                        principalTable: "assets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_reports_corrective_work_orders_tenant_id_work_o~",
                        columns: x => new { x.tenant_id, x.work_order_id },
                        principalTable: "corrective_work_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    details = table.Column<string>(type: "text", nullable: true),
                    metadata_json = table.Column<string>(type: "text", nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_order_activities", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_order_activities_asp_net_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_activities_corrective_work_orders_tenant_id_work~",
                        columns: x => new { x.tenant_id, x.work_order_id },
                        principalTable: "corrective_work_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    technician_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_order_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_order_assignments_asp_net_users_assigned_by_id",
                        column: x => x.assigned_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_assignments_asp_net_users_technician_id",
                        column: x => x.technician_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_assignments_corrective_work_orders_tenant_id_wor~",
                        columns: x => new { x.tenant_id, x.work_order_id },
                        principalTable: "corrective_work_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_report_reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    maintenance_report_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    verified = table.Column<bool>(type: "boolean", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_maintenance_report_reviews", x => x.id);
                    table.ForeignKey(
                        name: "FK_maintenance_report_reviews_asp_net_users_reviewer_id",
                        column: x => x.reviewer_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_maintenance_report_reviews_maintenance_reports_tenant_id_ma~",
                        columns: x => new { x.tenant_id, x.maintenance_report_id },
                        principalTable: "maintenance_reports",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_reports_reported_by_id",
                table: "breakdown_reports",
                column: "reported_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_reports_tenant_id_asset_id_reported_at_utc",
                table: "breakdown_reports",
                columns: new[] { "tenant_id", "asset_id", "reported_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_reports_tenant_id_number",
                table: "breakdown_reports",
                columns: new[] { "tenant_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_reports_tenant_id_site_id_status_priority",
                table: "breakdown_reports",
                columns: new[] { "tenant_id", "site_id", "status", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_reviews_reviewer_id",
                table: "breakdown_reviews",
                column: "reviewer_id");

            migrationBuilder.CreateIndex(
                name: "ix_breakdown_reviews_tenant_id_breakdown_report_id_reviewed_at~",
                table: "breakdown_reviews",
                columns: new[] { "tenant_id", "breakdown_report_id", "reviewed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_corrective_work_orders_assigned_by_id",
                table: "corrective_work_orders",
                column: "assigned_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_corrective_work_orders_assigned_technician_id",
                table: "corrective_work_orders",
                column: "assigned_technician_id");

            migrationBuilder.CreateIndex(
                name: "ix_corrective_work_orders_tenant_id_asset_id_created_at_utc",
                table: "corrective_work_orders",
                columns: new[] { "tenant_id", "asset_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_corrective_work_orders_tenant_id_assigned_technician_id_sta~",
                table: "corrective_work_orders",
                columns: new[] { "tenant_id", "assigned_technician_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_corrective_work_orders_tenant_id_breakdown_report_id",
                table: "corrective_work_orders",
                columns: new[] { "tenant_id", "breakdown_report_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_corrective_work_orders_tenant_id_number",
                table: "corrective_work_orders",
                columns: new[] { "tenant_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_corrective_work_orders_tenant_id_site_id_status_priority",
                table: "corrective_work_orders",
                columns: new[] { "tenant_id", "site_id", "status", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_report_reviews_reviewer_id",
                table: "maintenance_report_reviews",
                column: "reviewer_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_report_reviews_tenant_id_maintenance_report_id_~",
                table: "maintenance_report_reviews",
                columns: new[] { "tenant_id", "maintenance_report_id", "reviewed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_reports_technician_id",
                table: "maintenance_reports",
                column: "technician_id");

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_reports_tenant_id_asset_id",
                table: "maintenance_reports",
                columns: new[] { "tenant_id", "asset_id" });

            migrationBuilder.CreateIndex(
                name: "ix_maintenance_reports_tenant_id_work_order_id",
                table: "maintenance_reports",
                columns: new[] { "tenant_id", "work_order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_order_activities_actor_user_id",
                table: "work_order_activities",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_order_activities_tenant_id_work_order_id_occurred_at_u~",
                table: "work_order_activities",
                columns: new[] { "tenant_id", "work_order_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_work_order_assignments_assigned_by_id",
                table: "work_order_assignments",
                column: "assigned_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_order_assignments_technician_id",
                table: "work_order_assignments",
                column: "technician_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_order_assignments_tenant_id_work_order_id_assigned_at_~",
                table: "work_order_assignments",
                columns: new[] { "tenant_id", "work_order_id", "assigned_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "breakdown_reviews");

            migrationBuilder.DropTable(
                name: "maintenance_report_reviews");

            migrationBuilder.DropTable(
                name: "work_order_activities");

            migrationBuilder.DropTable(
                name: "work_order_assignments");

            migrationBuilder.DropTable(
                name: "maintenance_reports");

            migrationBuilder.DropTable(
                name: "corrective_work_orders");

            migrationBuilder.DropTable(
                name: "breakdown_reports");

            migrationBuilder.Sql("""
                DELETE FROM role_permissions WHERE permission_id IN
                    (SELECT id FROM permissions WHERE code LIKE 'maintenance.%');
                DELETE FROM permissions WHERE code IN
                    ('maintenance.breakdowns.view', 'maintenance.breakdowns.create', 'maintenance.breakdowns.review',
                     'maintenance.workorders.view', 'maintenance.workorders.create', 'maintenance.workorders.assign',
                     'maintenance.workorders.execute', 'maintenance.workorders.verify', 'maintenance.workorders.cancel');
                """);
        }
    }
}
