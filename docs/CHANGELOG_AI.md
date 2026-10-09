# AI Task Changelog

## Task #008 — Procurement and Purchasing

- **Date:** 2026-10-01
- **Summary:** Added supplier master and supplier-part pricing, multi-line purchase requisitions, independent approval/rejection, split PR conversion, purchase orders, partial/full receipts, dashboard metrics, tenant-safe document numbers, and immutable lifecycle activity.
- **Inventory integration:** PO receiving calls the existing Inventory receiver within one serializable transaction. Stock receipt/movement, on-hand, moving-average valuation, procurement receipt, PO quantities/status, and history commit or roll back together. Request UUIDs make receipt retries idempotent.
- **Security:** Added nine `procurement.*` permissions; tenant query filters, tenant-aware foreign keys, site scope, active-reference checks, requester/reviewer separation, UUID concurrency, over-conversion/over-receipt rejection, server totals, and restrictive deletion are enforced.
- **Migration:** Added and applied `20260930231103_ProcurementAndPurchasing` without resetting data. Generated `artifacts/sql/Task008_ProcurementAndPurchasing.idempotent.sql`.
- **UI:** Added English/Arabic responsive routes under `/{locale}/procurement` for dashboard, suppliers, requisitions, purchase orders, and receiving, including supplier history, workflow actions, conversion quantities, and receipt confirmation.
- **Tests/checks:** .NET restore/build passed with zero warnings; 94/94 backend tests passed. Frontend typecheck and ESLint passed; 9 suites/29 tests passed; production build generated 93 routes; npm audit found zero vulnerabilities.
- **Limits:** No discounts/tax allocation, landed cost, invoice/AP/payment/GL processing, FX conversion, RFQ/tendering, attachments, notifications, or supplier portal. Authenticated browser interaction depends on an available tenant session/browser surface.
- **Next:** Task #009 should add invoice and three-way-match readiness, or service-request/maintenance-planning analytics, while keeping payments and general ledger out of scope unless separately specified.

## Task #005 — Corrective Maintenance

- **Date:** 2026-09-29
- **Behavior:** Added breakdown intake/review, corrective work-order creation, assignment/reassignment history, technician start/notes/reporting, report submission, independent verification, return for correction, cancellation, real dashboard queues, and asset maintenance history.
- **Security:** Added nine `maintenance.*` permissions; tenant query filters, site-scope checks, eligible-assignee validation, separation of duties, server-owned transitions, UUID concurrency, restrictive foreign keys, audit events, and append-only workflow activity.
- **Migration:** `20260929143219_CorrectiveMaintenance`, applied to local PostgreSQL without reset. Idempotent SQL is `artifacts/sql/Task005_CorrectiveMaintenance.idempotent.sql`.
- **UI:** Added bilingual responsive routes under `/{locale}/maintenance` for the dashboard, breakdowns, work orders, personal work, verification, and details, plus maintenance history on asset details.
- **Material paths:** `Modules/Maintenance/{Domain,Application,Endpoints}`, `PlatformDbContext`, the Task #005 migration and SQL artifact, `components/maintenance`, localized maintenance routes/dictionaries, asset maintenance history, navigation, and responsive styles.
- **Tests:** .NET restore and build passed with zero warnings; 49/49 backend tests passed. Frontend typecheck and lint passed; 7 suites/21 tests passed; the production build generated 45 routes; npm audit found zero vulnerabilities. PostgreSQL readiness, CORS, anonymous authorization, English/LTR, Arabic/RTL, and maintenance route HTTP checks passed.
- **Limitations:** No parts/inventory consumption, labor or cost ledger, attachments, outbound notifications, SLA automation, preventive scheduling, or automatic asset-status changes. Authenticated browser QA requires a tenant session and was not automated in this environment.
- **Next:** Task #006 should implement Preventive Maintenance plans, schedule calculation, due calendar, controlled work-order generation, and background-job idempotency on top of the shared maintenance execution model.

## Task #004 — Asset Master Data & Asset Registry

- **Date:** 2026-09-28
- **Behavior:** Added tenant-isolated categories, manufacturers, models, searchable/paginated assets, identification and commercial fields, responsibility, controlled operational status, relocation, retirement/reactivation, real dashboard distributions, and activity history.
- **Security:** Added seven `assets.*` permissions. References are tenant-validated; access is site-scoped; relocation requires both sites; Platform Admin has no tenant asset authority. UUID concurrency and restrictive foreign keys protect updates.
- **Migration:** `20260928141221_AssetRegistry`, applied successfully to local PostgreSQL. Idempotent SQL is `artifacts/sql/Task004_AssetRegistry.idempotent.sql`.
- **UI:** Added bilingual `/assets`, `/assets/new`, `/assets/[id]`, and `/assets/master-data` routes with RTL/LTR, responsive tables/forms, real data, confirmation, loading/empty/error/conflict states, and permission-aware operations.
- **Validation:** .NET build passed with zero warnings; 36 backend tests passed. Frontend typecheck/lint passed; 6 suites/16 tests passed; production build generated 33 pages; npm audit found zero vulnerabilities.
- **Limitations:** Responsibility currently accepts a known tenant user UUID pending a reusable user picker. Documents/images, custom statuses, bulk import, depreciation, CM/PM, stock, and procurement remain deferred.
- **Next:** Task #005 should implement Corrective Maintenance referencing Asset UUIDs and reusing asset/site scope and history contracts.

## Task #003.7 — Premium SaaS Design System & UX Overhaul

- **Date:** 2026-09-28
- **Behavior:** Rebuilt the tenant shell, Platform Admin shell, tenant landing page, and both login experiences around the Nexa enterprise interface. Navigation now exposes only implemented workflows; forms, tables, feedback states, and responsive layouts share one component language with English/Arabic and LTR/RTL support.
- **Design system:** Added semantic light/dark tokens, spacing/type/radius/shadow/focus/motion scales, a persisted light/dark/system theme, reusable password, switch, checkbox, status, section, tabs, pagination, and loading-button patterns, plus `docs/DESIGN_SYSTEM.md`.
- **Security:** Authentication contracts and backend authorization are unchanged. Access tokens remain memory-only; refresh secrets remain isolated in HttpOnly cookies. Browser storage contains only the non-sensitive theme preference. Tenant login retains the API's UUID input pending a secure non-enumerating discovery or invitation contract.
- **Material paths:** `apps/web/src/app/globals.css`, `components/theme`, `components/navigation/app-shell.tsx`, `components/auth`, `components/platform`, `components/ui`, localized dictionaries, tenant landing page, and the design/architecture handoff documents.
- **Tests/checks:** Frontend typecheck and lint passed; 5 suites with 14 tests passed; the Next.js production build generated all 27 application pages after placeholder module routes were removed. Runtime and browser QA results are recorded in the completion report.
- **Database/API:** No backend source, database schema, migration, tenant data, or Platform Admin account was changed.
- **Limitations:** No remote font dependency was introduced, so typography uses preferred fonts when installed and safe system fallbacks otherwise. Friendly tenant discovery remains deferred until the backend defines its security behavior.
- **Next steps:** Add invitation delivery and a non-enumerating tenant entry flow, then validate new business modules against the design-system checklist before exposing them in navigation.

## Task #003.6 — Platform Admin Portal

- **Date:** 2026-09-28
- **Behavior:** Added dedicated English/Arabic Platform Admin login, authenticated responsive shell, dashboard metrics, tenant directory, secure tenant/initial-admin creation, success/error feedback, tenant details, and tenant activation lifecycle controls.
- **Security:** Reused rotating HttpOnly refresh sessions and the shared memory-only access-token client. Restored sessions must pass the platform-protected tenant-list call; tenant sessions cannot enter the portal. Passwords are secure inputs and are never persisted by the client.
- **Material paths:** `apps/web/src/app/[locale]/platform`, `components/platform`, `i18n/platform-dictionaries.ts`, `lib/platform-api.ts`, `app/globals.css`, and the platform branch in `navigation/app-shell.tsx`.
- **Tests/checks:** Frontend typecheck and lint passed; 4 frontend suites with 11 tests passed; Next.js production build generated 37 pages including all platform routes; all 24 backend tests passed unchanged.
- **Database/API:** No schema, migration, Platform Admin account, or backend contract change. The portal consumes the existing platform authentication and tenant administration endpoints.
- **Limitations:** Tenant details are composed from the tenant list because no individual GET endpoint exists. Initial passwords remain temporary until invitation/email recovery is implemented. Browser automation was unavailable; HTTP/runtime route checks were used.
- **Next steps:** Add invitation delivery and forced first-login password change, then MFA/SSO preparation and richer platform audit/subscription views.

## Task #003.5 — Local Development Setup & First Run

- **Date:** 2026-09-28
- **Database:** Started PostgreSQL 17 with Docker Compose using the persistent `postgres_data` volume; container health and API database readiness passed.
- **Migrations:** Applied and verified `InitialFoundation`, `TenantAdministrationAndLocations`, and `IdentitySessionsAndAccessAdministration` through EF Core migration history.
- **Bootstrap:** Verified one active Platform Administrator without reading or logging account credentials.
- **Runtime:** API is available at `http://localhost:5080`; frontend is available at `http://localhost:3000`; API identity, liveness, readiness, frontend routes, and CORS passed.
- **UI boundary:** The repository currently has tenant login only. Platform login and tenant creation remain API-only; no Swagger/OpenAPI UI is configured.
- **Windows note:** If PowerShell blocks `npm.ps1`, invoke the installed `npm.cmd` rather than changing execution policy.

## Task #003 — Identity, Sessions & Scoped Access Administration

- **Date:** 2026-09-27
- **Summary:** Added session-bound JWTs, rotating hashed refresh cookies, live revocation/authorization validation, tenant user/membership/role/scope administration, password management, final-admin and bounded-delegation rules, audit events, and bilingual IAM/security UI.
- **Migration:** `20260927202422_IdentitySessionsAndAccessAdministration`; includes existing-data backfills and IAM permission/system-role grants.
- **API/UI:** Added refresh/logout/session/password APIs, user/access/role/permission APIs, localized login, users, user access, roles, and security routes.
- **Security:** No browser token persistence, no raw refresh storage, generic failures, live inactive/session/version checks, cross-tenant/site denial, active-site validation, system-role protection, and final-admin protection.
- **Tests/checks:** .NET build and 24 backend tests passed; frontend typecheck, lint, 7 tests, production build, and npm audit passed. API root/liveness returned 200, protected context and cookie-less refresh returned 401, and readiness returned the expected 503 without PostgreSQL.
- **Environment limits:** The PostgreSQL migration was compiled and its idempotent SQL inspected, but could not be applied because Docker/PostgreSQL was unavailable. Browser visual QA was unavailable because no browser surface was connected.
- **Temporary limitation:** Admin-supplied passwords remain until email invitation/recovery infrastructure is implemented.
- **Recommended next task:** Invitation/email delivery with MFA/SSO preparation, or DataExchange foundations.

## Task #002 — Platform Bootstrap, Tenant Administration & Flexible Locations

- **Date:** 2026-09-27
- **Summary:** Added controlled platform-admin bootstrap, explicit platform tenant administration, transactional tenant initialization, company profiles, sites, configurable location types, flexible location trees, site scopes, audit events, concurrency, and bilingual management UI.
- **Material paths:** `Modules/Tenancy`, `Modules/Organization`, `Modules/Locations`, `Modules/Analytics`, administration/location web components and routes.
- **Migration:** Added `20260927165712_TenantAdministrationAndLocations`; compiled but not applied because PostgreSQL/Docker was unavailable.
- **API/UI:** Added platform login and tenant lifecycle plus company/site/type/location APIs; added `/[locale]/administration/*` and `/[locale]/locations`.
- **Security:** Platform and tenant claims are distinct; filters/save guards/composite keys enforce tenant ownership; tenant/site grants run server-side; invalid parents, cycles, cross-site moves, foreign types, duplicates, stale writes, and unsafe parent deactivation are rejected.
- **Tests:** .NET build and 15 backend tests passed; frontend typecheck, lint, 4 tests, production build, and npm audit passed.
- **Known limitations:** PostgreSQL application and browser visual QA unavailable; no refresh/session UI, subtree scope, full import pipeline, or cross-site moves.
- **Recommended next task:** Authentication/session delivery plus tenant user, membership, role, and scope administration.

## Task #001 — SaaS Platform Foundation

- **Date:** 2026-09-27
- **Summary:** Created the monorepo, responsive English/Arabic application shell, internal UI primitives, .NET modular-monolith API, identity/tenancy/authorization foundations, PostgreSQL migration, Docker Compose, tests, and architecture handoff.
- **Material paths:** `apps/web`, `apps/api`, `docs`, `.config`, root configuration, `docker-compose.yml`.
- **Database migrations:** Added `20260927153937_InitialFoundation` for ASP.NET Identity, tenants, memberships, tenant roles, permissions, and scoped role grants. Migration was generated with EF Core 10.0.12; it was not applied on this host because Docker/PostgreSQL was unavailable.
- **API changes:** Added `/api/v1`, `/api/v1/auth/login`, `/api/v1/context`, `/health/live`, and `/health/ready`; RFC 7807 handling, correlation IDs, structured logs, CORS, rate limiting, JWT auth, and tenant middleware.
- **Tests/checks:** .NET 10 build passed with zero warnings; 3/3 backend tenancy tests passed. Frontend type-check and lint passed; 2/2 localization tests passed; Next.js Turbopack production build prerendered all routes; npm audit reported zero vulnerabilities. Live API root/liveness returned 200 and readiness returned expected 503 without PostgreSQL.
- **Known issues:** No bootstrap user/tenant flow; no refresh/session strategy; scope data is not evaluated against resources yet; Docker/database migration could not be exercised on the validation host; business modules are placeholders.
- **Recommended next task:** Task #002 — controlled platform bootstrap and tenant administration foundation, including PostgreSQL integration tests.
# 2026-09-29 — Local authentication diagnosis

- Found that the API was stopped and port 3000 was owned by a different Next.js project while Nexa had a stale development process on port 3001.
- Restored the API on `http://localhost:5080` and Nexa on `http://localhost:3000`; PostgreSQL remained healthy and no database data or administrator account was changed.
- Verified readiness, API identity, CORS credentials headers, the generic invalid-credential response, and the existing Platform Admin's active and unlocked status without reading credential material.
- Updated Platform Admin login feedback so connectivity/server failures are distinct from HTTP 401 credential failures; added focused frontend tests and troubleshooting guidance.
- Validation: frontend typecheck and 18 tests pass; backend 36 tests pass. Browser credential entry remains a user action because no browser automation surface or credentials were available.

## Task #006 — Preventive Maintenance

- **Date:** 2026-09-29
- **Summary:** Added asset-linked preventive plans, fixed-calendar recurrence, checklist definitions/snapshots, bounded idempotent generation, hosted scheduling, PM dashboard/calendar, and full PM execution through the shared Task #005 work-order lifecycle.
- **Material paths:** `Modules/Maintenance/Domain/MaintenanceEntities.cs`, `PreventiveMaintenanceService.cs`, `PreventiveMaintenanceEndpoints.cs`, `PreventiveMaintenanceScheduler.cs`, shared corrective service/endpoints, PM web routes/components/dictionaries, Asset Details integration, and maintenance tests.
- **Migration:** Added and applied `20260929191659_PreventiveMaintenance` without resetting data. Generated `artifacts/sql/Task006_PreventiveMaintenance.idempotent.sql`.
- **Scheduling:** Fixed local-calendar anchors, month-end clamp/recovery, leap-day recovery, explicit DST conversion, three-occurrence catch-up bound, pause skip policy, and database-level occurrence uniqueness.
- **Security:** Added seven PM permissions; tenant/site scope, active membership, assignee rules, reviewer independence, server-owned state transitions, UUID concurrency, retired-asset checks, and historical snapshot immutability remain enforced by the API.
- **UI:** Added localized PM overview, plan list/editor/details, checklist builder, agenda calendar, PM work-order list/detail/checklist execution, and asset PM summary under `/{locale}/maintenance/pm`.
- **Tests/checks:** Backend 62/62 and frontend 23/23 tests passed. .NET restore/build, TypeScript, ESLint, npm audit, and the 55-page production build passed. Live readiness and route checks are recorded in the completion report.
- **Known limits:** Browser interaction was unavailable without a connected automation surface. No meter-based triggers, holiday exceptions, notifications, attachments, drag-and-drop checklist order, parts/cost capture, or automatic corrective creation from failed items.
- **Recommended next task:** Task #007 — Inventory and Spare Parts with warehouses/stock locations, reservations, CM/PM consumption, reorder policy, movements, valuation, and audit history.

## Task #007 — Inventory and Spare Parts Management

- **Date:** 2026-09-30
- **Summary:** Added site warehouses, hierarchical stock locations, spare-parts catalog, balances, receiving, issuing, transfers, adjustments, reservations, work-order consumption, reorder metrics, warehouse moving-average valuation, and append-only movement history.
- **Material paths:** `Modules/Inventory/Domain`, `Application/InventoryService.cs`, `Endpoints/InventoryEndpoints.cs`, `PlatformDbContext`, maintenance close/cancel integration, inventory web components/routes/dictionary/tests, navigation, work-order details, and Asset Details.
- **Migration:** Added and applied `20260930143940_InventoryAndSpareParts` without resetting data. Generated `artifacts/sql/Task007_InventoryAndSpareParts.idempotent.sql`.
- **Integrity:** Serializable stock mutations, balance concurrency, decimal precision, database nonnegative checks, composite tenant foreign keys, immutable movements, operation request UUIDs, cross-site transfer checks, and work-order eligibility rules.
- **UI:** Added localized Inventory overview, catalog, warehouses/storage, balances, receiving, issuing, transfers, adjustments, ledger, work-order materials, and asset consumption under `/{locale}/inventory`.
- **Tests/checks:** Backend 78/78 and frontend 25/25 tests passed. .NET restore/build, TypeScript, ESLint, npm audit, and the 77-page production build passed. Runtime checks are recorded in the completion report.
- **Known limits:** Atomic completed transfers only; one part line per command; no lots/serials/expiry, conversions, supplier master, landed cost, attachments, purchase orders, or automated replenishment.
- **Recommended next task:** Task #008 — Procurement with supplier master, requisitions, purchase orders, approvals, PO receiving linkage, and invoice/three-way readiness.

## 2026-10-01 — Task #009 Service Requests & Request Management

Implemented service categories, safe SR numbering, requester intake/My Requests, site-scoped triage and assignment, direct execution, internal/requester notes, lifecycle timestamps and concurrency, duplicate awareness, asset history, and atomic CM breakdown conversion. Added the bilingual responsive portal under `/{locale}/requests`, ten permissions, additive migration and idempotent SQL, backend lifecycle/security tests, frontend rule tests, and documentation updates.

Changed areas: ServiceRequests domain/application/endpoints, PlatformDbContext, identity permission catalog, CM conversion entry point, Program registration, frontend routes/components/navigation/localization, asset details, tests, and documentation.

Current limits: attachments, public guest intake, notifications, SLA timers, requester satisfaction, and automated routing remain future work. CM work-order creation still follows the existing breakdown review workflow.
