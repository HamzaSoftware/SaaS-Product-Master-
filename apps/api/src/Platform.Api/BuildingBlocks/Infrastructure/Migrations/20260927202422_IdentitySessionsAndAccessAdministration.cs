using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SaaSPlatform.Api.BuildingBlocks.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IdentitySessionsAndAccessAdministration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "tenant_roles",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "version",
                table: "tenant_roles",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "authorization_version",
                table: "tenant_memberships",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<Guid>(
                name: "version",
                table: "tenant_memberships",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "asp_net_users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "version",
                table: "asp_net_users",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            migrationBuilder.Sql("""
                INSERT INTO permissions (id, code, module, description) VALUES
                ('31000000-0000-0000-0000-000000000001', 'administration.users.view', 'Administration', 'View tenant users'),
                ('31000000-0000-0000-0000-000000000002', 'administration.users.manage', 'Administration', 'Manage tenant users'),
                ('31000000-0000-0000-0000-000000000003', 'administration.roles.view', 'Administration', 'View tenant roles'),
                ('31000000-0000-0000-0000-000000000004', 'administration.roles.manage', 'Administration', 'Manage tenant roles'),
                ('31000000-0000-0000-0000-000000000005', 'administration.memberships.manage', 'Administration', 'Manage memberships and scopes'),
                ('31000000-0000-0000-0000-000000000006', 'administration.sessions.view', 'Administration', 'View user sessions'),
                ('31000000-0000-0000-0000-000000000007', 'administration.sessions.revoke', 'Administration', 'Revoke user sessions')
                ON CONFLICT (code) DO NOTHING;

                INSERT INTO role_permissions (tenant_id, role_id, permission_id)
                SELECT r.tenant_id, r.id, p.id
                FROM tenant_roles r
                CROSS JOIN permissions p
                WHERE r.is_system = TRUE
                  AND r.name = 'Tenant Administrator'
                  AND p.code LIKE 'administration.%'
                ON CONFLICT DO NOTHING;
                """);

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    context = table.Column<int>(type: "integer", nullable: false),
                    refresh_token_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_used_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_sessions_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "asp_net_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_user_sessions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_tenant_id_user_id",
                table: "user_sessions",
                columns: new[] { "tenant_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_id_revoked_at_utc_expires_at_utc",
                table: "user_sessions",
                columns: new[] { "user_id", "revoked_at_utc", "expires_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DELETE FROM role_permissions WHERE permission_id IN
                    (SELECT id FROM permissions WHERE code LIKE 'administration.%');
                DELETE FROM permissions WHERE code IN
                    ('administration.users.view', 'administration.users.manage', 'administration.roles.view',
                     'administration.roles.manage', 'administration.memberships.manage',
                     'administration.sessions.view', 'administration.sessions.revoke');
                """);
            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "version",
                table: "tenant_roles");

            migrationBuilder.DropColumn(
                name: "authorization_version",
                table: "tenant_memberships");

            migrationBuilder.DropColumn(
                name: "version",
                table: "tenant_memberships");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "asp_net_users");

            migrationBuilder.DropColumn(
                name: "version",
                table: "asp_net_users");
        }
    }
}
