START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
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
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE TABLE breakdown_reports (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        number character varying(32) NOT NULL,
        asset_id uuid NOT NULL,
        site_id uuid NOT NULL,
        location_snapshot character varying(300),
        reported_by_id uuid NOT NULL,
        reported_at_utc timestamp with time zone NOT NULL,
        title character varying(200) NOT NULL,
        description text NOT NULL,
        observed_symptoms text,
        downtime_started_at_utc timestamp with time zone,
        priority integer NOT NULL,
        status integer NOT NULL,
        version uuid NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_breakdown_reports PRIMARY KEY (id),
        CONSTRAINT ak_breakdown_reports_tenant_id_id UNIQUE (tenant_id, id),
        CONSTRAINT "FK_breakdown_reports_asp_net_users_reported_by_id" FOREIGN KEY (reported_by_id) REFERENCES asp_net_users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_breakdown_reports_assets_tenant_id_asset_id" FOREIGN KEY (tenant_id, asset_id) REFERENCES assets (tenant_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_breakdown_reports_sites_tenant_id_site_id" FOREIGN KEY (tenant_id, site_id) REFERENCES sites (tenant_id, id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE TABLE breakdown_reviews (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        breakdown_report_id uuid NOT NULL,
        reviewer_id uuid NOT NULL,
        approved boolean NOT NULL,
        reason text,
        reviewed_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_breakdown_reviews PRIMARY KEY (id),
        CONSTRAINT "FK_breakdown_reviews_asp_net_users_reviewer_id" FOREIGN KEY (reviewer_id) REFERENCES asp_net_users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_breakdown_reviews_breakdown_reports_tenant_id_breakdown_rep~" FOREIGN KEY (tenant_id, breakdown_report_id) REFERENCES breakdown_reports (tenant_id, id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE TABLE corrective_work_orders (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        number character varying(32) NOT NULL,
        breakdown_report_id uuid NOT NULL,
        asset_id uuid NOT NULL,
        site_id uuid NOT NULL,
        location_snapshot character varying(300),
        priority integer NOT NULL,
        description text NOT NULL,
        status integer NOT NULL,
        assigned_technician_id uuid,
        assigned_by_id uuid,
        assigned_at_utc timestamp with time zone,
        planned_start_utc timestamp with time zone,
        actual_start_utc timestamp with time zone,
        actual_completion_utc timestamp with time zone,
        closed_at_utc timestamp with time zone,
        version uuid NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_corrective_work_orders PRIMARY KEY (id),
        CONSTRAINT ak_corrective_work_orders_tenant_id_id UNIQUE (tenant_id, id),
        CONSTRAINT "FK_corrective_work_orders_asp_net_users_assigned_by_id" FOREIGN KEY (assigned_by_id) REFERENCES asp_net_users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_corrective_work_orders_asp_net_users_assigned_technician_id" FOREIGN KEY (assigned_technician_id) REFERENCES asp_net_users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_corrective_work_orders_assets_tenant_id_asset_id" FOREIGN KEY (tenant_id, asset_id) REFERENCES assets (tenant_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_corrective_work_orders_breakdown_reports_tenant_id_breakdow~" FOREIGN KEY (tenant_id, breakdown_report_id) REFERENCES breakdown_reports (tenant_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_corrective_work_orders_sites_tenant_id_site_id" FOREIGN KEY (tenant_id, site_id) REFERENCES sites (tenant_id, id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE TABLE maintenance_reports (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        work_order_id uuid NOT NULL,
        asset_id uuid NOT NULL,
        technician_id uuid NOT NULL,
        diagnosis text,
        work_performed text,
        repair_outcome integer,
        actual_start_utc timestamp with time zone,
        actual_end_utc timestamp with time zone,
        labor_minutes integer,
        additional_notes text,
        submitted_at_utc timestamp with time zone,
        status integer NOT NULL,
        version uuid NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_maintenance_reports PRIMARY KEY (id),
        CONSTRAINT ak_maintenance_reports_tenant_id_id UNIQUE (tenant_id, id),
        CONSTRAINT "FK_maintenance_reports_asp_net_users_technician_id" FOREIGN KEY (technician_id) REFERENCES asp_net_users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_maintenance_reports_assets_tenant_id_asset_id" FOREIGN KEY (tenant_id, asset_id) REFERENCES assets (tenant_id, id) ON DELETE RESTRICT,
        CONSTRAINT "FK_maintenance_reports_corrective_work_orders_tenant_id_work_o~" FOREIGN KEY (tenant_id, work_order_id) REFERENCES corrective_work_orders (tenant_id, id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE TABLE work_order_activities (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        work_order_id uuid NOT NULL,
        actor_user_id uuid,
        action character varying(100) NOT NULL,
        details text,
        metadata_json text,
        occurred_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_work_order_activities PRIMARY KEY (id),
        CONSTRAINT "FK_work_order_activities_asp_net_users_actor_user_id" FOREIGN KEY (actor_user_id) REFERENCES asp_net_users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_activities_corrective_work_orders_tenant_id_work~" FOREIGN KEY (tenant_id, work_order_id) REFERENCES corrective_work_orders (tenant_id, id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE TABLE work_order_assignments (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        work_order_id uuid NOT NULL,
        technician_id uuid NOT NULL,
        assigned_by_id uuid NOT NULL,
        assigned_at_utc timestamp with time zone NOT NULL,
        note text,
        CONSTRAINT pk_work_order_assignments PRIMARY KEY (id),
        CONSTRAINT "FK_work_order_assignments_asp_net_users_assigned_by_id" FOREIGN KEY (assigned_by_id) REFERENCES asp_net_users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_assignments_asp_net_users_technician_id" FOREIGN KEY (technician_id) REFERENCES asp_net_users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_work_order_assignments_corrective_work_orders_tenant_id_wor~" FOREIGN KEY (tenant_id, work_order_id) REFERENCES corrective_work_orders (tenant_id, id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE TABLE maintenance_report_reviews (
        id uuid NOT NULL,
        tenant_id uuid NOT NULL,
        maintenance_report_id uuid NOT NULL,
        reviewer_id uuid NOT NULL,
        verified boolean NOT NULL,
        notes text,
        reviewed_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_maintenance_report_reviews PRIMARY KEY (id),
        CONSTRAINT "FK_maintenance_report_reviews_asp_net_users_reviewer_id" FOREIGN KEY (reviewer_id) REFERENCES asp_net_users (id) ON DELETE RESTRICT,
        CONSTRAINT "FK_maintenance_report_reviews_maintenance_reports_tenant_id_ma~" FOREIGN KEY (tenant_id, maintenance_report_id) REFERENCES maintenance_reports (tenant_id, id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_breakdown_reports_reported_by_id ON breakdown_reports (reported_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_breakdown_reports_tenant_id_asset_id_reported_at_utc ON breakdown_reports (tenant_id, asset_id, reported_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE UNIQUE INDEX ix_breakdown_reports_tenant_id_number ON breakdown_reports (tenant_id, number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_breakdown_reports_tenant_id_site_id_status_priority ON breakdown_reports (tenant_id, site_id, status, priority);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_breakdown_reviews_reviewer_id ON breakdown_reviews (reviewer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX "ix_breakdown_reviews_tenant_id_breakdown_report_id_reviewed_at~" ON breakdown_reviews (tenant_id, breakdown_report_id, reviewed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_corrective_work_orders_assigned_by_id ON corrective_work_orders (assigned_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_corrective_work_orders_assigned_technician_id ON corrective_work_orders (assigned_technician_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_corrective_work_orders_tenant_id_asset_id_created_at_utc ON corrective_work_orders (tenant_id, asset_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX "ix_corrective_work_orders_tenant_id_assigned_technician_id_sta~" ON corrective_work_orders (tenant_id, assigned_technician_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE UNIQUE INDEX ix_corrective_work_orders_tenant_id_breakdown_report_id ON corrective_work_orders (tenant_id, breakdown_report_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE UNIQUE INDEX ix_corrective_work_orders_tenant_id_number ON corrective_work_orders (tenant_id, number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_corrective_work_orders_tenant_id_site_id_status_priority ON corrective_work_orders (tenant_id, site_id, status, priority);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_maintenance_report_reviews_reviewer_id ON maintenance_report_reviews (reviewer_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX "ix_maintenance_report_reviews_tenant_id_maintenance_report_id_~" ON maintenance_report_reviews (tenant_id, maintenance_report_id, reviewed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_maintenance_reports_technician_id ON maintenance_reports (technician_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_maintenance_reports_tenant_id_asset_id ON maintenance_reports (tenant_id, asset_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE UNIQUE INDEX ix_maintenance_reports_tenant_id_work_order_id ON maintenance_reports (tenant_id, work_order_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_work_order_activities_actor_user_id ON work_order_activities (actor_user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX "ix_work_order_activities_tenant_id_work_order_id_occurred_at_u~" ON work_order_activities (tenant_id, work_order_id, occurred_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_work_order_assignments_assigned_by_id ON work_order_assignments (assigned_by_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX ix_work_order_assignments_technician_id ON work_order_assignments (technician_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    CREATE INDEX "ix_work_order_assignments_tenant_id_work_order_id_assigned_at_~" ON work_order_assignments (tenant_id, work_order_id, assigned_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260929143219_CorrectiveMaintenance') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260929143219_CorrectiveMaintenance', '10.0.12');
    END IF;
END $EF$;
COMMIT;

