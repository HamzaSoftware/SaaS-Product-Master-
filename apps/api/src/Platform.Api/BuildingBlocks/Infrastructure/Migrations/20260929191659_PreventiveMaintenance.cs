using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSPlatform.Api.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreventiveMaintenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, module, description) VALUES
                ('60000000-0000-0000-0000-000000000001', 'maintenance.pm.view', 'Maintenance', 'View preventive maintenance plans and calendar'),
                ('60000000-0000-0000-0000-000000000002', 'maintenance.pm.create', 'Maintenance', 'Create preventive maintenance plans'),
                ('60000000-0000-0000-0000-000000000003', 'maintenance.pm.update', 'Maintenance', 'Update preventive maintenance plans'),
                ('60000000-0000-0000-0000-000000000004', 'maintenance.pm.manage', 'Maintenance', 'Manage preventive plan state and assignments'),
                ('60000000-0000-0000-0000-000000000005', 'maintenance.pm.generate', 'Maintenance', 'Generate preventive work orders'),
                ('60000000-0000-0000-0000-000000000006', 'maintenance.pm.execute', 'Maintenance', 'Execute assigned preventive work'),
                ('60000000-0000-0000-0000-000000000007', 'maintenance.pm.verify', 'Maintenance', 'Verify preventive maintenance reports')
                ON CONFLICT (code) DO NOTHING;

                INSERT INTO role_permissions (tenant_id, role_id, permission_id)
                SELECT r.tenant_id, r.id, p.id
                FROM tenant_roles r CROSS JOIN permissions p
                WHERE r.is_system = TRUE AND r.name = 'Tenant Administrator' AND p.code LIKE 'maintenance.pm.%'
                ON CONFLICT DO NOTHING;

                UPDATE tenant_memberships m SET authorization_version = gen_random_uuid()
                WHERE EXISTS (
                    SELECT 1 FROM membership_roles mr
                    JOIN tenant_roles r ON r.tenant_id = mr.tenant_id AND r.id = mr.role_id
                    WHERE mr.tenant_id = m.tenant_id AND mr.membership_id = m.id
                      AND r.is_system = TRUE AND r.name = 'Tenant Administrator');
                """);
            migrationBuilder.DropIndex(
                name: "ix_corrective_work_orders_tenant_id_breakdown_report_id",
                table: "corrective_work_orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "breakdown_report_id",
                table: "corrective_work_orders",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "preventive_maintenance_occurrence_id",
                table: "corrective_work_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "preventive_maintenance_plan_id",
                table: "corrective_work_orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "scheduled_due_at_utc",
                table: "corrective_work_orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "type",
                table: "corrective_work_orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "preventive_maintenance_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    start_local_date = table.Column<DateOnly>(type: "date", nullable: false),
                    scheduled_local_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_zone_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    recurrence_type = table.Column<int>(type: "integer", nullable: false),
                    recurrence_interval = table.Column<int>(type: "integer", nullable: false),
                    next_due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_generated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    state = table.Column<int>(type: "integer", nullable: false),
                    estimated_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    default_technician_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preventive_maintenance_plans", x => x.id);
                    table.UniqueConstraint("ak_preventive_maintenance_plans_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_preventive_maintenance_plans_asp_net_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_preventive_maintenance_plans_asp_net_users_default_technici~",
                        column: x => x.default_technician_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_preventive_maintenance_plans_assets_tenant_id_asset_id",
                        columns: x => new { x.tenant_id, x.asset_id },
                        principalTable: "assets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_preventive_maintenance_plans_sites_tenant_id_site_id",
                        columns: x => new { x.tenant_id, x.site_id },
                        principalTable: "sites",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "preventive_work_order_checklist_responses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_checklist_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    instruction_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    instruction_ar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    guidance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    result = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    recorded_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preventive_work_order_checklist_responses", x => x.id);
                    table.ForeignKey(
                        name: "FK_preventive_work_order_checklist_responses_asp_net_users_rec~",
                        column: x => x.recorded_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_preventive_work_order_checklist_responses_corrective_work_o~",
                        columns: x => new { x.tenant_id, x.work_order_id },
                        principalTable: "corrective_work_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "preventive_maintenance_checklist_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    instruction_en = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    instruction_ar = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    guidance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preventive_maintenance_checklist_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_preventive_maintenance_checklist_items_preventive_maintenan~",
                        columns: x => new { x.tenant_id, x.plan_id },
                        principalTable: "preventive_maintenance_plans",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "preventive_maintenance_occurrences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_due_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    generated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_preventive_maintenance_occurrences", x => x.id);
                    table.UniqueConstraint("ak_preventive_maintenance_occurrences_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_preventive_maintenance_occurrences_preventive_maintenance_p~",
                        columns: x => new { x.tenant_id, x.plan_id },
                        principalTable: "preventive_maintenance_plans",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_corrective_work_orders_tenant_id_breakdown_report_id",
                table: "corrective_work_orders",
                columns: new[] { "tenant_id", "breakdown_report_id" },
                unique: true,
                filter: "breakdown_report_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_corrective_work_orders_tenant_id_preventive_maintenance_occ~",
                table: "corrective_work_orders",
                columns: new[] { "tenant_id", "preventive_maintenance_occurrence_id" },
                unique: true,
                filter: "preventive_maintenance_occurrence_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_corrective_work_orders_tenant_id_preventive_maintenance_pla~",
                table: "corrective_work_orders",
                columns: new[] { "tenant_id", "preventive_maintenance_plan_id" });

            migrationBuilder.CreateIndex(
                name: "ix_preventive_maintenance_checklist_items_tenant_id_plan_id_se~",
                table: "preventive_maintenance_checklist_items",
                columns: new[] { "tenant_id", "plan_id", "sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_preventive_maintenance_occurrences_tenant_id_plan_id_schedu~",
                table: "preventive_maintenance_occurrences",
                columns: new[] { "tenant_id", "plan_id", "scheduled_due_at_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_preventive_maintenance_occurrences_tenant_id_scheduled_due_~",
                table: "preventive_maintenance_occurrences",
                columns: new[] { "tenant_id", "scheduled_due_at_utc", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_preventive_maintenance_plans_created_by_id",
                table: "preventive_maintenance_plans",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_preventive_maintenance_plans_default_technician_id",
                table: "preventive_maintenance_plans",
                column: "default_technician_id");

            migrationBuilder.CreateIndex(
                name: "ix_preventive_maintenance_plans_tenant_id_asset_id_state",
                table: "preventive_maintenance_plans",
                columns: new[] { "tenant_id", "asset_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_preventive_maintenance_plans_tenant_id_code",
                table: "preventive_maintenance_plans",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_preventive_maintenance_plans_tenant_id_site_id_state_next_d~",
                table: "preventive_maintenance_plans",
                columns: new[] { "tenant_id", "site_id", "state", "next_due_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_preventive_work_order_checklist_responses_recorded_by_id",
                table: "preventive_work_order_checklist_responses",
                column: "recorded_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_preventive_work_order_checklist_responses_tenant_id_work_or~",
                table: "preventive_work_order_checklist_responses",
                columns: new[] { "tenant_id", "work_order_id", "sequence" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_corrective_work_orders_preventive_maintenance_occurrences_t~",
                table: "corrective_work_orders",
                columns: new[] { "tenant_id", "preventive_maintenance_occurrence_id" },
                principalTable: "preventive_maintenance_occurrences",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_corrective_work_orders_preventive_maintenance_plans_tenant_~",
                table: "corrective_work_orders",
                columns: new[] { "tenant_id", "preventive_maintenance_plan_id" },
                principalTable: "preventive_maintenance_plans",
                principalColumns: new[] { "tenant_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions WHERE permission_id IN
                    (SELECT id FROM permissions WHERE code LIKE 'maintenance.pm.%');
                DELETE FROM permissions WHERE code IN
                    ('maintenance.pm.view', 'maintenance.pm.create', 'maintenance.pm.update', 'maintenance.pm.manage',
                     'maintenance.pm.generate', 'maintenance.pm.execute', 'maintenance.pm.verify');
                """);
            migrationBuilder.DropForeignKey(
                name: "FK_corrective_work_orders_preventive_maintenance_occurrences_t~",
                table: "corrective_work_orders");

            migrationBuilder.DropForeignKey(
                name: "FK_corrective_work_orders_preventive_maintenance_plans_tenant_~",
                table: "corrective_work_orders");

            migrationBuilder.DropTable(
                name: "preventive_maintenance_checklist_items");

            migrationBuilder.DropTable(
                name: "preventive_maintenance_occurrences");

            migrationBuilder.DropTable(
                name: "preventive_work_order_checklist_responses");

            migrationBuilder.DropTable(
                name: "preventive_maintenance_plans");

            migrationBuilder.DropIndex(
                name: "ix_corrective_work_orders_tenant_id_breakdown_report_id",
                table: "corrective_work_orders");

            migrationBuilder.DropIndex(
                name: "ix_corrective_work_orders_tenant_id_preventive_maintenance_occ~",
                table: "corrective_work_orders");

            migrationBuilder.DropIndex(
                name: "ix_corrective_work_orders_tenant_id_preventive_maintenance_pla~",
                table: "corrective_work_orders");

            migrationBuilder.DropColumn(
                name: "preventive_maintenance_occurrence_id",
                table: "corrective_work_orders");

            migrationBuilder.DropColumn(
                name: "preventive_maintenance_plan_id",
                table: "corrective_work_orders");

            migrationBuilder.DropColumn(
                name: "scheduled_due_at_utc",
                table: "corrective_work_orders");

            migrationBuilder.DropColumn(
                name: "type",
                table: "corrective_work_orders");

            migrationBuilder.AlterColumn<Guid>(
                name: "breakdown_report_id",
                table: "corrective_work_orders",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_corrective_work_orders_tenant_id_breakdown_report_id",
                table: "corrective_work_orders",
                columns: new[] { "tenant_id", "breakdown_report_id" },
                unique: true);
        }
    }
}
