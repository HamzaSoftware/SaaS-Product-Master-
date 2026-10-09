using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSPlatform.Api.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ProcurementAndPurchasing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, module, description) VALUES
                ('80000000-0000-0000-0000-000000000001', 'procurement.view', 'Procurement', 'View suppliers, requisitions, and purchase orders'),
                ('80000000-0000-0000-0000-000000000002', 'procurement.suppliers.manage', 'Procurement', 'Manage supplier master data and supplier-part pricing'),
                ('80000000-0000-0000-0000-000000000003', 'procurement.requisitions.create', 'Procurement', 'Create and submit purchase requisitions'),
                ('80000000-0000-0000-0000-000000000004', 'procurement.requisitions.review', 'Procurement', 'Approve and reject submitted purchase requisitions'),
                ('80000000-0000-0000-0000-000000000005', 'procurement.purchase-orders.create', 'Procurement', 'Create purchase orders from approved requisitions'),
                ('80000000-0000-0000-0000-000000000006', 'procurement.purchase-orders.manage', 'Procurement', 'Edit, issue, cancel, and close purchase orders'),
                ('80000000-0000-0000-0000-000000000007', 'procurement.receiving.create', 'Procurement', 'Post goods receipts against issued purchase orders'),
                ('80000000-0000-0000-0000-000000000008', 'procurement.receiving.view', 'Procurement', 'View procurement receipt history'),
                ('80000000-0000-0000-0000-000000000009', 'procurement.dashboard.view', 'Procurement', 'View procurement dashboard metrics')
                ON CONFLICT (code) DO NOTHING;
                INSERT INTO role_permissions (tenant_id, role_id, permission_id)
                SELECT r.tenant_id, r.id, p.id FROM tenant_roles r CROSS JOIN permissions p
                WHERE r.is_system = TRUE AND r.name = 'Tenant Administrator' AND p.code LIKE 'procurement.%'
                ON CONFLICT DO NOTHING;
                UPDATE tenant_memberships m SET authorization_version = gen_random_uuid()
                WHERE EXISTS (SELECT 1 FROM membership_roles mr JOIN tenant_roles r ON r.tenant_id = mr.tenant_id AND r.id = mr.role_id
                    WHERE mr.tenant_id = m.tenant_id AND mr.membership_id = m.id AND r.is_system = TRUE AND r.name = 'Tenant Administrator');
                """);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_stock_receipts_tenant_id_id",
                table: "stock_receipts",
                columns: new[] { "tenant_id", "id" });

            migrationBuilder.CreateTable(
                name: "procurement_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    from_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    to_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procurement_activities", x => x.id);
                    table.ForeignKey(
                        name: "FK_procurement_activities_asp_net_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "procurement_document_sequences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    next_value = table.Column<long>(type: "bigint", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_procurement_document_sequences", x => x.id);
                    table.CheckConstraint("ck_procurement_sequences_positive", "next_value > 0");
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_date = table.Column<DateOnly>(type: "date", nullable: false),
                    need_by_date = table.Column<DateOnly>(type: "date", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    business_justification = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reviewed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_comment = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_requisitions", x => x.id);
                    table.UniqueConstraint("ak_purchase_requisitions_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_purchase_requisitions_asp_net_users_requested_by_id",
                        column: x => x.requested_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requisitions_asp_net_users_reviewed_by_id",
                        column: x => x.reviewed_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requisitions_sites_tenant_id_site_id",
                        columns: x => new { x.tenant_id, x.site_id },
                        principalTable: "sites",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requisitions_warehouses_tenant_id_target_warehouse~",
                        columns: x => new { x.tenant_id, x.target_warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    contact_person = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    alternate_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tax_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    commercial_registration_number = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    country = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    website = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    payment_terms = table.Column<string>(type: "text", nullable: true),
                    default_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_suppliers", x => x.id);
                    table.UniqueConstraint("ak_suppliers_tenant_id_id", x => new { x.tenant_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_date = table.Column<DateOnly>(type: "date", nullable: false),
                    expected_delivery_date = table.Column<DateOnly>(type: "date", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    payment_terms = table.Column<string>(type: "text", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    issued_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_orders", x => x.id);
                    table.UniqueConstraint("ak_purchase_orders_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_purchase_orders_subtotal", "subtotal >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_orders_asp_net_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_asp_net_users_issued_by_id",
                        column: x => x.issued_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_sites_tenant_id_site_id",
                        columns: x => new { x.tenant_id, x.site_id },
                        principalTable: "sites",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_suppliers_tenant_id_supplier_id",
                        columns: x => new { x.tenant_id, x.supplier_id },
                        principalTable: "suppliers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_warehouses_tenant_id_destination_warehouse_~",
                        columns: x => new { x.tenant_id, x.destination_warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_requisition_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requisition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure = table.Column<int>(type: "integer", nullable: false),
                    suggested_supplier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    estimated_unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    estimated_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    converted_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_requisition_lines", x => x.id);
                    table.UniqueConstraint("ak_purchase_requisition_lines_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_requisition_lines_quantities", "requested_quantity > 0 AND converted_quantity >= 0 AND converted_quantity <= requested_quantity AND (estimated_unit_price IS NULL OR estimated_unit_price >= 0) AND estimated_total >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_requisition_lines_purchase_requisitions_tenant_id_~",
                        columns: x => new { x.tenant_id, x.requisition_id },
                        principalTable: "purchase_requisitions",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requisition_lines_spare_parts_tenant_id_part_id",
                        columns: x => new { x.tenant_id, x.part_id },
                        principalTable: "spare_parts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_requisition_lines_suppliers_tenant_id_suggested_su~",
                        columns: x => new { x.tenant_id, x.suggested_supplier_id },
                        principalTable: "suppliers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "supplier_parts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    supplier_part_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    last_unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    minimum_order_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    lead_time_days = table.Column<int>(type: "integer", nullable: false),
                    is_preferred = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_supplier_parts", x => x.id);
                    table.UniqueConstraint("ak_supplier_parts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_supplier_parts_values", "last_unit_price IS NULL OR last_unit_price >= 0 AND minimum_order_quantity > 0 AND lead_time_days >= 0");
                    table.ForeignKey(
                        name: "FK_supplier_parts_spare_parts_tenant_id_part_id",
                        columns: x => new { x.tenant_id, x.part_id },
                        principalTable: "spare_parts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_parts_suppliers_tenant_id_supplier_id",
                        columns: x => new { x.tenant_id, x.supplier_id },
                        principalTable: "suppliers",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    supplier_reference = table.Column<string>(type: "text", nullable: true),
                    receipt_date = table.Column<DateOnly>(type: "date", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    total_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_receipts", x => x.id);
                    table.UniqueConstraint("ak_purchase_receipts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_purchase_receipts_total", "total_cost >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_receipts_asp_net_users_received_by_id",
                        column: x => x.received_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_receipts_purchase_orders_tenant_id_purchase_order_~",
                        columns: x => new { x.tenant_id, x.purchase_order_id },
                        principalTable: "purchase_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_receipts_warehouse_stock_locations_tenant_id_wareh~",
                        columns: x => new { x.tenant_id, x.warehouse_id, x.stock_location_id },
                        principalTable: "warehouse_stock_locations",
                        principalColumns: new[] { "tenant_id", "warehouse_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_receipts_warehouses_tenant_id_warehouse_id",
                        columns: x => new { x.tenant_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_requisition_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supplier_part_number = table.Column<string>(type: "text", nullable: true),
                    ordered_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_of_measure = table.Column<int>(type: "integer", nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    line_subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    received_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_order_lines", x => x.id);
                    table.UniqueConstraint("ak_purchase_order_lines_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_purchase_order_lines_quantities", "ordered_quantity > 0 AND received_quantity >= 0 AND received_quantity <= ordered_quantity AND unit_price >= 0 AND line_subtotal >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_orders_tenant_id_purchase_ord~",
                        columns: x => new { x.tenant_id, x.purchase_order_id },
                        principalTable: "purchase_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_requisition_lines_tenant_id_s~",
                        columns: x => new { x.tenant_id, x.source_requisition_line_id },
                        principalTable: "purchase_requisition_lines",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_spare_parts_tenant_id_part_id",
                        columns: x => new { x.tenant_id, x.part_id },
                        principalTable: "spare_parts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_receipt_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchase_order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_purchase_receipt_lines", x => x.id);
                    table.CheckConstraint("ck_purchase_receipt_lines_values", "quantity > 0 AND unit_cost >= 0 AND total_cost >= 0");
                    table.ForeignKey(
                        name: "FK_purchase_receipt_lines_purchase_order_lines_tenant_id_purch~",
                        columns: x => new { x.tenant_id, x.purchase_order_line_id },
                        principalTable: "purchase_order_lines",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_receipt_lines_purchase_receipts_tenant_id_purchase~",
                        columns: x => new { x.tenant_id, x.purchase_receipt_id },
                        principalTable: "purchase_receipts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_receipt_lines_stock_receipts_tenant_id_stock_recei~",
                        columns: x => new { x.tenant_id, x.stock_receipt_id },
                        principalTable: "stock_receipts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_procurement_activities_actor_user_id",
                table: "procurement_activities",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_procurement_activities_tenant_id_entity_type_entity_id_occu~",
                table: "procurement_activities",
                columns: new[] { "tenant_id", "entity_type", "entity_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_procurement_document_sequences_tenant_id_document_type_year",
                table: "procurement_document_sequences",
                columns: new[] { "tenant_id", "document_type", "year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_tenant_id_part_id",
                table: "purchase_order_lines",
                columns: new[] { "tenant_id", "part_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_tenant_id_purchase_order_id",
                table: "purchase_order_lines",
                columns: new[] { "tenant_id", "purchase_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_tenant_id_source_requisition_line_id",
                table: "purchase_order_lines",
                columns: new[] { "tenant_id", "source_requisition_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_created_by_id",
                table: "purchase_orders",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_issued_by_id",
                table: "purchase_orders",
                column: "issued_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_tenant_id_destination_warehouse_id",
                table: "purchase_orders",
                columns: new[] { "tenant_id", "destination_warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_tenant_id_number",
                table: "purchase_orders",
                columns: new[] { "tenant_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_tenant_id_site_id_status",
                table: "purchase_orders",
                columns: new[] { "tenant_id", "site_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_tenant_id_supplier_id_status",
                table: "purchase_orders",
                columns: new[] { "tenant_id", "supplier_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_receipt_lines_tenant_id_purchase_order_line_id",
                table: "purchase_receipt_lines",
                columns: new[] { "tenant_id", "purchase_order_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_receipt_lines_tenant_id_purchase_receipt_id_purcha~",
                table: "purchase_receipt_lines",
                columns: new[] { "tenant_id", "purchase_receipt_id", "purchase_order_line_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_receipt_lines_tenant_id_stock_receipt_id",
                table: "purchase_receipt_lines",
                columns: new[] { "tenant_id", "stock_receipt_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_receipts_received_by_id",
                table: "purchase_receipts",
                column: "received_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_receipts_tenant_id_purchase_order_id_receipt_date",
                table: "purchase_receipts",
                columns: new[] { "tenant_id", "purchase_order_id", "receipt_date" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_receipts_tenant_id_receipt_number",
                table: "purchase_receipts",
                columns: new[] { "tenant_id", "receipt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_receipts_tenant_id_request_id",
                table: "purchase_receipts",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_receipts_tenant_id_warehouse_id_stock_location_id",
                table: "purchase_receipts",
                columns: new[] { "tenant_id", "warehouse_id", "stock_location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisition_lines_tenant_id_part_id",
                table: "purchase_requisition_lines",
                columns: new[] { "tenant_id", "part_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisition_lines_tenant_id_requisition_id_part_id",
                table: "purchase_requisition_lines",
                columns: new[] { "tenant_id", "requisition_id", "part_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisition_lines_tenant_id_suggested_supplier_id",
                table: "purchase_requisition_lines",
                columns: new[] { "tenant_id", "suggested_supplier_id" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisitions_requested_by_id",
                table: "purchase_requisitions",
                column: "requested_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisitions_reviewed_by_id",
                table: "purchase_requisitions",
                column: "reviewed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisitions_tenant_id_number",
                table: "purchase_requisitions",
                columns: new[] { "tenant_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisitions_tenant_id_site_id_status",
                table: "purchase_requisitions",
                columns: new[] { "tenant_id", "site_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_requisitions_tenant_id_target_warehouse_id",
                table: "purchase_requisitions",
                columns: new[] { "tenant_id", "target_warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "ix_supplier_parts_tenant_id_part_id",
                table: "supplier_parts",
                columns: new[] { "tenant_id", "part_id" },
                unique: true,
                filter: "is_preferred");

            migrationBuilder.CreateIndex(
                name: "ix_supplier_parts_tenant_id_supplier_id_part_id",
                table: "supplier_parts",
                columns: new[] { "tenant_id", "supplier_id", "part_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_suppliers_tenant_id_code",
                table: "suppliers",
                columns: new[] { "tenant_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions WHERE permission_id IN (SELECT id FROM permissions WHERE code LIKE 'procurement.%');
                DELETE FROM permissions WHERE code IN ('procurement.view', 'procurement.suppliers.manage', 'procurement.requisitions.create', 'procurement.requisitions.review', 'procurement.purchase-orders.create', 'procurement.purchase-orders.manage', 'procurement.receiving.create', 'procurement.receiving.view', 'procurement.dashboard.view');
                """);

            migrationBuilder.DropTable(
                name: "procurement_activities");

            migrationBuilder.DropTable(
                name: "procurement_document_sequences");

            migrationBuilder.DropTable(
                name: "purchase_receipt_lines");

            migrationBuilder.DropTable(
                name: "supplier_parts");

            migrationBuilder.DropTable(
                name: "purchase_order_lines");

            migrationBuilder.DropTable(
                name: "purchase_receipts");

            migrationBuilder.DropTable(
                name: "purchase_requisition_lines");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropTable(
                name: "purchase_requisitions");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_stock_receipts_tenant_id_id",
                table: "stock_receipts");
        }
    }
}
