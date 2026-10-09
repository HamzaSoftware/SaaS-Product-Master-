using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSPlatform.Api.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ServiceRequestsAndRequestManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, module, description) VALUES
                ('90000000-0000-0000-0000-000000000001', 'service_requests.view', 'Service Requests', 'View service requests within granted scope'),
                ('90000000-0000-0000-0000-000000000002', 'service_requests.create', 'Service Requests', 'Create and submit service requests'),
                ('90000000-0000-0000-0000-000000000003', 'service_requests.view_own', 'Service Requests', 'View own service requests'),
                ('90000000-0000-0000-0000-000000000004', 'service_requests.triage', 'Service Requests', 'Triage, reject, and coordinate service requests'),
                ('90000000-0000-0000-0000-000000000005', 'service_requests.assign', 'Service Requests', 'Assign service requests to eligible users'),
                ('90000000-0000-0000-0000-000000000006', 'service_requests.execute', 'Service Requests', 'Execute assigned service requests'),
                ('90000000-0000-0000-0000-000000000007', 'service_requests.resolve', 'Service Requests', 'Resolve service requests'),
                ('90000000-0000-0000-0000-000000000008', 'service_requests.close', 'Service Requests', 'Close and reopen resolved service requests'),
                ('90000000-0000-0000-0000-000000000009', 'service_requests.convert_to_maintenance', 'Service Requests', 'Convert eligible service requests to corrective maintenance'),
                ('90000000-0000-0000-0000-000000000010', 'service_request_categories.manage', 'Service Requests', 'Manage service request categories')
                ON CONFLICT (code) DO NOTHING;
                INSERT INTO role_permissions (tenant_id, role_id, permission_id)
                SELECT r.tenant_id, r.id, p.id FROM tenant_roles r CROSS JOIN permissions p
                WHERE r.is_system = TRUE AND r.name = 'Tenant Administrator' AND (p.code LIKE 'service_requests.%' OR p.code = 'service_request_categories.manage')
                ON CONFLICT DO NOTHING;
                UPDATE tenant_memberships m SET authorization_version = gen_random_uuid()
                WHERE EXISTS (SELECT 1 FROM membership_roles mr JOIN tenant_roles r ON r.tenant_id = mr.tenant_id AND r.id = mr.role_id
                    WHERE mr.tenant_id = m.tenant_id AND mr.membership_id = m.id AND r.is_system = TRUE AND r.name = 'Tenant Administrator');
                """);
            migrationBuilder.CreateTable(
                name: "service_request_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_en = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    description_en = table.Column<string>(type: "text", nullable: true),
                    description_ar = table.Column<string>(type: "text", nullable: true),
                    default_priority = table.Column<int>(type: "integer", nullable: false),
                    asset_required = table.Column<bool>(type: "boolean", nullable: false),
                    location_required = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_request_categories", x => x.id);
                    table.UniqueConstraint("ak_service_request_categories_tenant_id_id", x => new { x.tenant_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_contact = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    assigned_to_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    first_response_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    work_started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_summary = table.Column<string>(type: "text", nullable: true),
                    terminal_reason = table.Column<string>(type: "text", nullable: true),
                    linked_breakdown_report_id = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_requests", x => x.id);
                    table.UniqueConstraint("ak_service_requests_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_service_requests_asp_net_users_assigned_by_id",
                        column: x => x.assigned_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_asp_net_users_assigned_to_id",
                        column: x => x.assigned_to_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_asp_net_users_requester_id",
                        column: x => x.requester_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_asp_net_users_resolved_by_id",
                        column: x => x.resolved_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_assets_tenant_id_asset_id",
                        columns: x => new { x.tenant_id, x.asset_id },
                        principalTable: "assets",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_breakdown_reports_tenant_id_linked_breakdo~",
                        columns: x => new { x.tenant_id, x.linked_breakdown_report_id },
                        principalTable: "breakdown_reports",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_location_nodes_tenant_id_site_id_location_~",
                        columns: x => new { x.tenant_id, x.site_id, x.location_id },
                        principalTable: "location_nodes",
                        principalColumns: new[] { "tenant_id", "site_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_service_request_categories_tenant_id_categ~",
                        columns: x => new { x.tenant_id, x.category_id },
                        principalTable: "service_request_categories",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_sites_tenant_id_site_id",
                        columns: x => new { x.tenant_id, x.site_id },
                        principalTable: "sites",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_request_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    from_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    to_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    details = table.Column<string>(type: "text", nullable: true),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_request_activities", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_request_activities_asp_net_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_activities_service_requests_tenant_id_servi~",
                        columns: x => new { x.tenant_id, x.service_request_id },
                        principalTable: "service_requests",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_request_assignments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_assignee_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assignee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_request_assignments", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_request_assignments_asp_net_users_assigned_by_id",
                        column: x => x.assigned_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_assignments_asp_net_users_assignee_id",
                        column: x => x.assignee_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_assignments_asp_net_users_previous_assignee~",
                        column: x => x.previous_assignee_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_assignments_service_requests_tenant_id_serv~",
                        columns: x => new { x.tenant_id, x.service_request_id },
                        principalTable: "service_requests",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_request_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    visibility = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_request_notes", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_request_notes_asp_net_users_author_id",
                        column: x => x.author_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_request_notes_service_requests_tenant_id_service_re~",
                        columns: x => new { x.tenant_id, x.service_request_id },
                        principalTable: "service_requests",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_service_request_activities_actor_user_id",
                table: "service_request_activities",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_request_activities_tenant_id_service_request_id_occ~",
                table: "service_request_activities",
                columns: new[] { "tenant_id", "service_request_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_service_request_assignments_assigned_by_id",
                table: "service_request_assignments",
                column: "assigned_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_request_assignments_assignee_id",
                table: "service_request_assignments",
                column: "assignee_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_request_assignments_previous_assignee_id",
                table: "service_request_assignments",
                column: "previous_assignee_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_request_assignments_tenant_id_service_request_id_as~",
                table: "service_request_assignments",
                columns: new[] { "tenant_id", "service_request_id", "assigned_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_service_request_categories_tenant_id_code",
                table: "service_request_categories",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_request_categories_tenant_id_is_active_sort_order",
                table: "service_request_categories",
                columns: new[] { "tenant_id", "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_service_request_notes_author_id",
                table: "service_request_notes",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_request_notes_tenant_id_service_request_id_created_~",
                table: "service_request_notes",
                columns: new[] { "tenant_id", "service_request_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_assigned_by_id",
                table: "service_requests",
                column: "assigned_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_assigned_to_id",
                table: "service_requests",
                column: "assigned_to_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_requester_id",
                table: "service_requests",
                column: "requester_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_resolved_by_id",
                table: "service_requests",
                column: "resolved_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_id_asset_id_created_at_utc",
                table: "service_requests",
                columns: new[] { "tenant_id", "asset_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_id_assigned_to_id_status",
                table: "service_requests",
                columns: new[] { "tenant_id", "assigned_to_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_id_category_id",
                table: "service_requests",
                columns: new[] { "tenant_id", "category_id" });

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_id_linked_breakdown_report_id",
                table: "service_requests",
                columns: new[] { "tenant_id", "linked_breakdown_report_id" },
                unique: true,
                filter: "linked_breakdown_report_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_id_number",
                table: "service_requests",
                columns: new[] { "tenant_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_id_requester_id_created_at_utc",
                table: "service_requests",
                columns: new[] { "tenant_id", "requester_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_id_site_id_location_id",
                table: "service_requests",
                columns: new[] { "tenant_id", "site_id", "location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_service_requests_tenant_id_site_id_status_priority",
                table: "service_requests",
                columns: new[] { "tenant_id", "site_id", "status", "priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions WHERE permission_id IN (SELECT id FROM permissions WHERE code LIKE 'service_requests.%' OR code = 'service_request_categories.manage');
                DELETE FROM permissions WHERE code LIKE 'service_requests.%' OR code = 'service_request_categories.manage';
                """);
            migrationBuilder.DropTable(
                name: "service_request_activities");

            migrationBuilder.DropTable(
                name: "service_request_assignments");

            migrationBuilder.DropTable(
                name: "service_request_notes");

            migrationBuilder.DropTable(
                name: "service_requests");

            migrationBuilder.DropTable(
                name: "service_request_categories");
        }
    }
}
