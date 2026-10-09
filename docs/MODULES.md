# Modules

Status meanings: **Planned** has documentation only; **Foundation** has boundaries or shared primitives but no business workflow; **In Progress** is actively incomplete; **Implemented** is usable business behavior.

| Module | Status | Purpose | Dependencies and notes |
| --- | --- | --- | --- |
| Identity | Implemented | Platform users, passwords, roles, permissions, tenant/platform login, revocable sessions, and administration UI | Depends on Tenancy and EF. MFA, SSO, and invitations are planned. |
| Tenancy | Implemented | Tenants, memberships, platform portal, request tenant resolution, and isolation guardrails | Required by every tenant-owned module. Subscription and enabled modules are planned. |
| Organization | Planned | Company profile, departments, organizational structures | Depends on Tenancy; must model change over time. |
| MasterData | Planned | Tenant-controlled reference values and classifications | Depends on Tenancy; ownership/versioning to be defined per catalog. |
| Locations | Foundation | Sites and flexible arbitrary-depth location tree | UI route exists; data model is documented but no tables/endpoints exist. |
| Assets | Implemented | Asset master data, registry, lifecycle, placement, responsibility, and history | Depends on Locations, Identity, Tenancy, and Analytics. Maintenance/PM links are planned. |
| Maintenance | Implemented | Breakdown intake, corrective work orders, assignment, execution reports, verification, closure, and asset maintenance history | Depends on Assets, Locations, Identity, Tenancy, and audit infrastructure. |
| PreventiveMaintenance | Planned | PM plans, schedules, generation | Depends on Assets, Maintenance, background processing. |
| Inventory | Foundation | Items, stock, movements, reservations | UI route/boundary only. Depends on Locations and Identity. |
| Notifications | Planned | In-app/email/SMS notification orchestration | Depends on background processing and provider adapters. |
| Analytics | Foundation | Read models, KPIs, dashboards | UI route/boundary only; operational sources do not exist yet. |
| DataExchange | Foundation | Imports, exports, templates, mapping, validation, job history | Architecture documented; depends on background processing and module import ports. |
| Integrations | Foundation | ERP/HR/finance/CMMS/IoT/BMS/SSO/webhook adapters | Architecture documented; no provider adapter exists. |

No module marked Foundation should be presented as a completed business capability.

## Task #002 status update

- **Tenancy — Implemented:** controlled platform bootstrap, tenant list/create/status, transactional company/default-type/optional-admin initialization.
- **Organization — Implemented:** one editable bilingual company profile per tenant.
- **Locations — Implemented:** site lifecycle, tenant-configurable types, arbitrary-depth hierarchy, same-site moves, site scope, and bilingual screens.
- **Analytics — Foundation:** append-only administration audit events are persisted; reporting/read models remain planned.
- **DataExchange — Foundation:** no importer exists; application services are the required validation boundary for future files and migrations.

## Task #003 status update

- **Identity — Implemented:** distinct tenant/platform login, revocable rotating sessions, password change/reset boundary, user/role administration, permission catalog, bounded delegation, and final-admin protection.
- **Tenancy — Implemented:** memberships with active state, concurrency, authorization freshness, tenant-wide/site role assignments, and immediate stale-token invalidation.

## Task #003.6 status update

- **Identity — Implemented:** localized Platform Admin login and cookie-based platform-session restoration use the existing session architecture; access tokens stay in memory.
- **Tenancy — Implemented:** responsive platform dashboard, tenant list/create feedback, tenant details, and activate/deactivate UI use the existing platform-only APIs.
- **Boundary:** tenant sessions cannot satisfy the platform permission policies and are rejected again by the portal's restored-session validation call.

## Task #003.7 cross-cutting UI update

- The Nexa design system is a shared frontend capability, not a business module. It supplies semantic light/dark tokens, bilingual RTL/LTR layouts, responsive shells, authentication layouts, reusable controls, and standard result states.
- Tenant navigation exposes only the implemented Organization, Locations, Identity, Tenancy, and session-management workflows. Foundation and planned modules stay out of navigation until their real workflows exist.
- No module status, API contract, persistence model, or authorization rule changed in this task. Future modules must follow `docs/DESIGN_SYSTEM.md` when they become usable.

## Task #005 status update

- **Maintenance — Implemented:** breakdown submission/review, single work-order conversion, scoped assignment, technician notes and reports, verification/return, cancellation, dashboard queues, and asset maintenance history.
- **Authorization:** nine `maintenance.*` permissions are seeded and granted to existing system Tenant Administrator roles. Site-scoped grants restrict assets, reports, work orders, technicians, and history.
- **Deferred:** preventive schedules, spare parts and inventory consumption, labor/cost accounting, attachments, notification delivery, SLA escalation, and automatic asset-status synchronization.

## Task #004 status update

- **Assets — Implemented:** categories, manufacturers, models, paginated/searchable registry, details/editing, site/location placement, responsibility, purchase/warranty data, operational status, retirement, relocation, real dashboard summaries, and history.
- CM, PM, inventory, procurement, and finance remain out of scope. They must reference the stable Asset UUID rather than copy asset identity.

## Task #006 status update

- **Preventive Maintenance — Implemented:** asset-linked plans, deterministic fixed-calendar recurrence, upcoming occurrences, pause/resume/archive, checklist definitions and immutable work-order snapshots, idempotent scheduled/manual generation, dashboard/calendar, assignment, execution, reporting, correction, verification, cancellation, and asset linkage.
- **Shared Maintenance — Extended:** CM and PM use one work-order lifecycle, activity stream, structured maintenance report, reviewer separation, and asset history. Existing corrective records remain compatible through a type discriminator and nullable source links.
- **Authorization:** seven `maintenance.pm.*` permissions are seeded and granted to existing system Tenant Administrator roles. Active membership, tenant isolation, site scope, assignee restrictions, authorization-version freshness, and UUID concurrency are enforced server-side.
- **Deferred:** meter/condition triggers, holiday calendars, notifications, attachments, automatic corrective creation from a failed PM item, parts consumption, and maintenance costing.

## Task #007 status update

- **Inventory — Implemented:** warehouses, separate hierarchical storage, spare-parts catalog, balances, receiving, general/work-order issue, atomic transfer, adjustment, reservation/partial consumption/release, reorder metrics, valuation, and append-only ledger.
- **Maintenance — Integrated:** CM and PM use one Parts & Materials workflow. Material cost and inventory activity remain attached to the shared work order; Asset Details derives consumption through work orders.
- **Authorization:** nine `inventory.*` permissions support tenant-wide and site grants. Every stock operation checks active references, tenant/site ownership, work-order eligibility, stock availability, idempotency, and concurrency.
- **Procurement integration:** suppliers, requisitions, purchase orders, approvals, and PO-line receiving are implemented in Task #008. Taxes/freight allocation, invoicing, and three-way matching remain deferred.

## Procurement

`Modules/Procurement` owns supplier commercial data, purchase requisitions and review, purchase-order conversion/lifecycle, goods receipts, document sequences, and procurement activity. It references Sites, Warehouses, Spare Parts, users, and Task #007 stock receipts through tenant-aware restrictive relationships.

The module exposes `IProcurementService` and uses `IInventoryService.ReceiveForProcurement` for stock effects. Supplier prices and PR estimates are planning values only. Only posted PO receipts feed moving weighted-average valuation.

Frontend components live under `components/procurement`, localized copy under `i18n/procurement-dictionaries.ts`, and routes under `app/[locale]/procurement`.

## Service Requests

`Modules/ServiceRequests` implements tenant-configured categories and the Draft → Submitted → UnderReview → Assigned → InProgress → Resolved → Closed lifecycle, plus rejection, cancellation, CM conversion, and controlled reopen. Requester reads are ownership-filtered and exclude internal notes. Assignment requires an active member with `service_requests.execute` in the request site.

Frontend components live under `components/service-requests`, bilingual copy under `i18n/service-request-dictionaries.ts`, and routes under `app/[locale]/requests`. Asset Details exposes linked requests; request details links the related CM breakdown and work order when present.
