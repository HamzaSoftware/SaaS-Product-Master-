using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSPlatform.Api.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryAndSpareParts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, module, description) VALUES
                ('70000000-0000-0000-0000-000000000001', 'inventory.view', 'Inventory', 'View inventory, stock balances, and dashboard'),
                ('70000000-0000-0000-0000-000000000002', 'inventory.parts.manage', 'Inventory', 'Manage the spare-parts catalog'),
                ('70000000-0000-0000-0000-000000000003', 'inventory.warehouses.manage', 'Inventory', 'Manage warehouses and stock locations'),
                ('70000000-0000-0000-0000-000000000004', 'inventory.receive', 'Inventory', 'Receive inventory stock'),
                ('70000000-0000-0000-0000-000000000005', 'inventory.issue', 'Inventory', 'Issue and consume inventory stock'),
                ('70000000-0000-0000-0000-000000000006', 'inventory.transfer', 'Inventory', 'Transfer stock between locations and warehouses'),
                ('70000000-0000-0000-0000-000000000007', 'inventory.adjust', 'Inventory', 'Perform controlled stock adjustments'),
                ('70000000-0000-0000-0000-000000000008', 'inventory.reserve', 'Inventory', 'Reserve and release stock for work orders'),
                ('70000000-0000-0000-0000-000000000009', 'inventory.audit', 'Inventory', 'View the inventory movement ledger')
                ON CONFLICT (code) DO NOTHING;
                INSERT INTO role_permissions (tenant_id, role_id, permission_id)
                SELECT r.tenant_id, r.id, p.id FROM tenant_roles r CROSS JOIN permissions p
                WHERE r.is_system = TRUE AND r.name = 'Tenant Administrator' AND p.code LIKE 'inventory.%'
                ON CONFLICT DO NOTHING;
                UPDATE tenant_memberships m SET authorization_version = gen_random_uuid()
                WHERE EXISTS (SELECT 1 FROM membership_roles mr JOIN tenant_roles r ON r.tenant_id = mr.tenant_id AND r.id = mr.role_id
                    WHERE mr.tenant_id = m.tenant_id AND mr.membership_id = m.id AND r.is_system = TRUE AND r.name = 'Tenant Administrator');
                """);
            migrationBuilder.CreateTable(
                name: "spare_parts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    manufacturer = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    manufacturer_part_number = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    unit_of_measure = table.Column<int>(type: "integer", nullable: false),
                    barcode = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_stock = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reorder_point = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    preferred_supplier = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_spare_parts", x => x.id);
                    table.UniqueConstraint("ak_spare_parts_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_spare_parts_thresholds_nonnegative", "minimum_stock >= 0 AND reorder_point >= 0");
                });

            migrationBuilder.CreateTable(
                name: "warehouses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    site_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouses", x => x.id);
                    table.UniqueConstraint("ak_warehouses_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.ForeignKey(
                        name: "FK_warehouses_sites_tenant_id_site_id",
                        columns: x => new { x.tenant_id, x.site_id },
                        principalTable: "sites",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_stock_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    name_en = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_ar = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_warehouse_stock_locations", x => x.id);
                    table.UniqueConstraint("ak_warehouse_stock_locations_tenant_id_warehouse_id_id", x => new { x.tenant_id, x.warehouse_id, x.id });
                    table.ForeignKey(
                        name: "FK_warehouse_stock_locations_warehouse_stock_locations_tenant_~",
                        columns: x => new { x.tenant_id, x.warehouse_id, x.parent_location_id },
                        principalTable: "warehouse_stock_locations",
                        principalColumns: new[] { "tenant_id", "warehouse_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_warehouse_stock_locations_warehouses_tenant_id_warehouse_id",
                        columns: x => new { x.tenant_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_balances",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_on_hand = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    quantity_reserved = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    average_unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_balances", x => x.id);
                    table.CheckConstraint("ck_inventory_balances_nonnegative", "quantity_on_hand >= 0 AND quantity_reserved >= 0 AND quantity_reserved <= quantity_on_hand AND average_unit_cost >= 0");
                    table.ForeignKey(
                        name: "FK_inventory_balances_spare_parts_tenant_id_part_id",
                        columns: x => new { x.tenant_id, x.part_id },
                        principalTable: "spare_parts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_balances_warehouse_stock_locations_tenant_id_ware~",
                        columns: x => new { x.tenant_id, x.warehouse_id, x.stock_location_id },
                        principalTable: "warehouse_stock_locations",
                        principalColumns: new[] { "tenant_id", "warehouse_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_balances_warehouses_tenant_id_warehouse_id",
                        columns: x => new { x.tenant_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "inventory_movements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    reference_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    reference_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_movements", x => x.id);
                    table.ForeignKey(
                        name: "FK_inventory_movements_asp_net_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_spare_parts_tenant_id_part_id",
                        columns: x => new { x.tenant_id, x.part_id },
                        principalTable: "spare_parts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_warehouse_stock_locations_tenant_id_war~",
                        columns: x => new { x.tenant_id, x.warehouse_id, x.stock_location_id },
                        principalTable: "warehouse_stock_locations",
                        principalColumns: new[] { "tenant_id", "warehouse_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_inventory_movements_warehouses_tenant_id_warehouse_id",
                        columns: x => new { x.tenant_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_adjustments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_number = table.Column<string>(type: "text", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    adjustment_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    new_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    reason = table.Column<int>(type: "integer", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_adjustments", x => x.id);
                    table.CheckConstraint("ck_stock_adjustments_nonnegative", "new_quantity >= 0 AND adjustment_quantity <> 0");
                    table.ForeignKey(
                        name: "FK_stock_adjustments_asp_net_users_performed_by_id",
                        column: x => x.performed_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_adjustments_spare_parts_tenant_id_part_id",
                        columns: x => new { x.tenant_id, x.part_id },
                        principalTable: "spare_parts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_adjustments_warehouse_stock_locations_tenant_id_wareh~",
                        columns: x => new { x.tenant_id, x.warehouse_id, x.stock_location_id },
                        principalTable: "warehouse_stock_locations",
                        principalColumns: new[] { "tenant_id", "warehouse_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_adjustments_warehouses_tenant_id_warehouse_id",
                        columns: x => new { x.tenant_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    supplier_name = table.Column<string>(type: "text", nullable: true),
                    supplier_reference = table.Column<string>(type: "text", nullable: true),
                    receipt_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    received_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_receipts", x => x.id);
                    table.CheckConstraint("ck_stock_receipts_positive", "quantity > 0 AND unit_cost >= 0 AND total_cost >= 0");
                    table.ForeignKey(
                        name: "FK_stock_receipts_asp_net_users_received_by_id",
                        column: x => x.received_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_receipts_spare_parts_tenant_id_part_id",
                        columns: x => new { x.tenant_id, x.part_id },
                        principalTable: "spare_parts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_receipts_warehouse_stock_locations_tenant_id_warehous~",
                        columns: x => new { x.tenant_id, x.warehouse_id, x.stock_location_id },
                        principalTable: "warehouse_stock_locations",
                        principalColumns: new[] { "tenant_id", "warehouse_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_receipts_warehouses_tenant_id_warehouse_id",
                        columns: x => new { x.tenant_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_reservations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reserved_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    consumed_quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    released_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_reservations", x => x.id);
                    table.UniqueConstraint("ak_stock_reservations_tenant_id_id", x => new { x.tenant_id, x.id });
                    table.CheckConstraint("ck_stock_reservations_quantities", "reserved_quantity > 0 AND consumed_quantity >= 0 AND consumed_quantity <= reserved_quantity");
                    table.ForeignKey(
                        name: "FK_stock_reservations_asp_net_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_corrective_work_orders_tenant_id_work_or~",
                        columns: x => new { x.tenant_id, x.work_order_id },
                        principalTable: "corrective_work_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_spare_parts_tenant_id_part_id",
                        columns: x => new { x.tenant_id, x.part_id },
                        principalTable: "spare_parts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_warehouse_stock_locations_tenant_id_ware~",
                        columns: x => new { x.tenant_id, x.warehouse_id, x.stock_location_id },
                        principalTable: "warehouse_stock_locations",
                        principalColumns: new[] { "tenant_id", "warehouse_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_reservations_warehouses_tenant_id_warehouse_id",
                        columns: x => new { x.tenant_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference_number = table.Column<string>(type: "text", nullable: false),
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    state = table.Column<int>(type: "integer", nullable: false),
                    performed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_transfers", x => x.id);
                    table.CheckConstraint("ck_stock_transfers_positive", "quantity > 0 AND unit_cost >= 0 AND total_value >= 0");
                    table.ForeignKey(
                        name: "FK_stock_transfers_asp_net_users_performed_by_id",
                        column: x => x.performed_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_spare_parts_tenant_id_part_id",
                        columns: x => new { x.tenant_id, x.part_id },
                        principalTable: "spare_parts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouse_stock_locations_tenant_id_destina~",
                        columns: x => new { x.tenant_id, x.destination_warehouse_id, x.destination_location_id },
                        principalTable: "warehouse_stock_locations",
                        principalColumns: new[] { "tenant_id", "warehouse_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouse_stock_locations_tenant_id_source_~",
                        columns: x => new { x.tenant_id, x.source_warehouse_id, x.source_location_id },
                        principalTable: "warehouse_stock_locations",
                        principalColumns: new[] { "tenant_id", "warehouse_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouses_tenant_id_destination_warehouse_~",
                        columns: x => new { x.tenant_id, x.destination_warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transfers_warehouses_tenant_id_source_warehouse_id",
                        columns: x => new { x.tenant_id, x.source_warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_issues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reference_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    issued_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_issues", x => x.id);
                    table.CheckConstraint("ck_stock_issues_positive", "quantity > 0 AND unit_cost >= 0 AND total_cost >= 0");
                    table.ForeignKey(
                        name: "FK_stock_issues_asp_net_users_issued_by_id",
                        column: x => x.issued_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_issues_corrective_work_orders_tenant_id_work_order_id",
                        columns: x => new { x.tenant_id, x.work_order_id },
                        principalTable: "corrective_work_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_issues_spare_parts_tenant_id_part_id",
                        columns: x => new { x.tenant_id, x.part_id },
                        principalTable: "spare_parts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_issues_stock_reservations_tenant_id_reservation_id",
                        columns: x => new { x.tenant_id, x.reservation_id },
                        principalTable: "stock_reservations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_issues_warehouse_stock_locations_tenant_id_warehouse_~",
                        columns: x => new { x.tenant_id, x.warehouse_id, x.stock_location_id },
                        principalTable: "warehouse_stock_locations",
                        principalColumns: new[] { "tenant_id", "warehouse_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_issues_warehouses_tenant_id_warehouse_id",
                        columns: x => new { x.tenant_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "work_order_part_consumptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    part_id = table.Column<Guid>(type: "uuid", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    unit_cost = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    consumed_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work_order_part_consumptions", x => x.id);
                    table.CheckConstraint("ck_work_order_consumptions_positive", "quantity > 0 AND unit_cost >= 0 AND total_cost >= 0");
                    table.ForeignKey(
                        name: "FK_work_order_part_consumptions_asp_net_users_consumed_by_id",
                        column: x => x.consumed_by_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_part_consumptions_corrective_work_orders_tenant_~",
                        columns: x => new { x.tenant_id, x.work_order_id },
                        principalTable: "corrective_work_orders",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_part_consumptions_spare_parts_tenant_id_part_id",
                        columns: x => new { x.tenant_id, x.part_id },
                        principalTable: "spare_parts",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_part_consumptions_stock_reservations_tenant_id_r~",
                        columns: x => new { x.tenant_id, x.reservation_id },
                        principalTable: "stock_reservations",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_part_consumptions_warehouse_stock_locations_tena~",
                        columns: x => new { x.tenant_id, x.warehouse_id, x.stock_location_id },
                        principalTable: "warehouse_stock_locations",
                        principalColumns: new[] { "tenant_id", "warehouse_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_order_part_consumptions_warehouses_tenant_id_warehouse~",
                        columns: x => new { x.tenant_id, x.warehouse_id },
                        principalTable: "warehouses",
                        principalColumns: new[] { "tenant_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_balances_tenant_id_part_id_warehouse_id",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "part_id", "warehouse_id" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_balances_tenant_id_warehouse_id_stock_location_id~",
                table: "inventory_balances",
                columns: new[] { "tenant_id", "warehouse_id", "stock_location_id", "part_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_actor_user_id",
                table: "inventory_movements",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_tenant_id_part_id_occurred_at_utc",
                table: "inventory_movements",
                columns: new[] { "tenant_id", "part_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_tenant_id_reference_type_reference_id",
                table: "inventory_movements",
                columns: new[] { "tenant_id", "reference_type", "reference_id" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_tenant_id_warehouse_id_occurred_at_utc",
                table: "inventory_movements",
                columns: new[] { "tenant_id", "warehouse_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_inventory_movements_tenant_id_warehouse_id_stock_location_id",
                table: "inventory_movements",
                columns: new[] { "tenant_id", "warehouse_id", "stock_location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_spare_parts_tenant_id_barcode",
                table: "spare_parts",
                columns: new[] { "tenant_id", "barcode" },
                unique: true,
                filter: "barcode IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_spare_parts_tenant_id_sku",
                table: "spare_parts",
                columns: new[] { "tenant_id", "sku" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_performed_by_id",
                table: "stock_adjustments",
                column: "performed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_tenant_id_part_id",
                table: "stock_adjustments",
                columns: new[] { "tenant_id", "part_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_tenant_id_request_id",
                table: "stock_adjustments",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_adjustments_tenant_id_warehouse_id_stock_location_id",
                table: "stock_adjustments",
                columns: new[] { "tenant_id", "warehouse_id", "stock_location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_issues_issued_by_id",
                table: "stock_issues",
                column: "issued_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_issues_tenant_id_part_id",
                table: "stock_issues",
                columns: new[] { "tenant_id", "part_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_issues_tenant_id_request_id",
                table: "stock_issues",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_issues_tenant_id_reservation_id",
                table: "stock_issues",
                columns: new[] { "tenant_id", "reservation_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_issues_tenant_id_warehouse_id_stock_location_id",
                table: "stock_issues",
                columns: new[] { "tenant_id", "warehouse_id", "stock_location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_issues_tenant_id_work_order_id",
                table: "stock_issues",
                columns: new[] { "tenant_id", "work_order_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipts_received_by_id",
                table: "stock_receipts",
                column: "received_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipts_tenant_id_part_id",
                table: "stock_receipts",
                columns: new[] { "tenant_id", "part_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipts_tenant_id_reference_number",
                table: "stock_receipts",
                columns: new[] { "tenant_id", "reference_number" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipts_tenant_id_request_id",
                table: "stock_receipts",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_receipts_tenant_id_warehouse_id_stock_location_id",
                table: "stock_receipts",
                columns: new[] { "tenant_id", "warehouse_id", "stock_location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_reservations_created_by_id",
                table: "stock_reservations",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_reservations_tenant_id_part_id",
                table: "stock_reservations",
                columns: new[] { "tenant_id", "part_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_reservations_tenant_id_request_id",
                table: "stock_reservations",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_reservations_tenant_id_warehouse_id_stock_location_id",
                table: "stock_reservations",
                columns: new[] { "tenant_id", "warehouse_id", "stock_location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_reservations_tenant_id_work_order_id_state",
                table: "stock_reservations",
                columns: new[] { "tenant_id", "work_order_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_performed_by_id",
                table: "stock_transfers",
                column: "performed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_tenant_id_destination_warehouse_id_destinat~",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "destination_warehouse_id", "destination_location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_tenant_id_part_id",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "part_id" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_tenant_id_reference_number",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "reference_number" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_tenant_id_request_id",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_stock_transfers_tenant_id_source_warehouse_id_source_locati~",
                table: "stock_transfers",
                columns: new[] { "tenant_id", "source_warehouse_id", "source_location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_stock_locations_tenant_id_warehouse_id_code",
                table: "warehouse_stock_locations",
                columns: new[] { "tenant_id", "warehouse_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_warehouse_stock_locations_tenant_id_warehouse_id_parent_loc~",
                table: "warehouse_stock_locations",
                columns: new[] { "tenant_id", "warehouse_id", "parent_location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_warehouses_tenant_id_code",
                table: "warehouses",
                columns: new[] { "tenant_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_warehouses_tenant_id_site_id_is_active",
                table: "warehouses",
                columns: new[] { "tenant_id", "site_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_work_order_part_consumptions_consumed_by_id",
                table: "work_order_part_consumptions",
                column: "consumed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_order_part_consumptions_tenant_id_part_id",
                table: "work_order_part_consumptions",
                columns: new[] { "tenant_id", "part_id" });

            migrationBuilder.CreateIndex(
                name: "ix_work_order_part_consumptions_tenant_id_request_id",
                table: "work_order_part_consumptions",
                columns: new[] { "tenant_id", "request_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_work_order_part_consumptions_tenant_id_reservation_id",
                table: "work_order_part_consumptions",
                columns: new[] { "tenant_id", "reservation_id" });

            migrationBuilder.CreateIndex(
                name: "ix_work_order_part_consumptions_tenant_id_warehouse_id_stock_l~",
                table: "work_order_part_consumptions",
                columns: new[] { "tenant_id", "warehouse_id", "stock_location_id" });

            migrationBuilder.CreateIndex(
                name: "ix_work_order_part_consumptions_tenant_id_work_order_id_consum~",
                table: "work_order_part_consumptions",
                columns: new[] { "tenant_id", "work_order_id", "consumed_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions WHERE permission_id IN (SELECT id FROM permissions WHERE code LIKE 'inventory.%');
                DELETE FROM permissions WHERE code IN ('inventory.view', 'inventory.parts.manage', 'inventory.warehouses.manage', 'inventory.receive', 'inventory.issue', 'inventory.transfer', 'inventory.adjust', 'inventory.reserve', 'inventory.audit');
                """);
            migrationBuilder.DropTable(
                name: "inventory_balances");

            migrationBuilder.DropTable(
                name: "inventory_movements");

            migrationBuilder.DropTable(
                name: "stock_adjustments");

            migrationBuilder.DropTable(
                name: "stock_issues");

            migrationBuilder.DropTable(
                name: "stock_receipts");

            migrationBuilder.DropTable(
                name: "stock_transfers");

            migrationBuilder.DropTable(
                name: "work_order_part_consumptions");

            migrationBuilder.DropTable(
                name: "stock_reservations");

            migrationBuilder.DropTable(
                name: "spare_parts");

            migrationBuilder.DropTable(
                name: "warehouse_stock_locations");

            migrationBuilder.DropTable(
                name: "warehouses");
        }
    }
}
