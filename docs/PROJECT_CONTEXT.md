# Project Context

Read this concise handoff before changing the repository. The current implementation is **Task #008: Procurement and Purchasing** on top of Tasks #001–#007.

## Stack and shape

- Next.js 16.3.6, React 19.3, strict TypeScript, Tailwind CSS 4.3; `/en` and `/ar` routes with RTL/LTR.
- ASP.NET Core/.NET 10, EF Core 10, Npgsql, PostgreSQL 17; modular monolith with `/api/v1` REST endpoints.
- Key paths: `apps/web/src`, `apps/api/src/Platform.Api`, `apps/api/tests`, and `docs`.

## Identity and sessions

- ASP.NET Core Identity is the only password/user system. Global users have explicit tenant memberships; tenant roles contain platform-defined permissions and tenant/site-scoped assignments.
- Tenant and platform logins remain separate. Both issue short-lived JWTs tied to server-side `user_sessions`; refresh secrets are random, stored only as SHA-256 hashes, rotated on refresh, and delivered in an HttpOnly cookie.
- JWT validation checks session, global-user status, tenant/membership status, and membership `AuthorizationVersion` against the database on every request. Membership, role, permission, and scope changes invalidate stale access immediately.
- Auth APIs support login, refresh, current context, logout, logout-all, own-session list/revoke, and password change. Password changes and administrative resets revoke sessions.
- The browser keeps access tokens in memory only, restores via the cookie, retries one refresh on 401, and never stores tokens in local/session storage.

## Administration

- Platform administrators use `/{locale}/platform/login` and a separate guarded platform shell. The dashboard lists tenants, creates a tenant plus initial Tenant Admin, and opens tenant details for active-status management.
- Platform session restoration succeeds only after the platform-only tenant API authorizes the restored token. Tenant sessions receive 403 from that API and cannot enter the portal. Access tokens remain memory-only.
- Tenant admins can search/page users, create or attach a global user, edit display names, activate/deactivate membership, reset a password temporarily, and configure roles with tenant-wide or active-site scope.
- Custom roles use the platform catalog. Administrators cannot grant permissions they lack tenant-wide, use foreign roles/sites, create platform admins, modify required system roles, or remove the final active tenant administrator.
- `CurrentAccessService` provides reusable active-membership, permission, site-access, and granted-site checks for future modules.
- Security actions write tenant audit events without passwords, tokens, or hashes.

## Corrective maintenance

- Breakdown reports reference an existing tenant asset and progress from submission through review into one corrective work order. Reviews and final verification enforce separation of duties.
- Work orders support eligible technician assignment/reassignment, start, append-only notes, a draft maintenance report, submission, verification, return for correction, cancellation, and immutable activity history.
- Every read and write is tenant-filtered and site-authorized. Assignment also checks active membership and `maintenance.workorders.execute`; UUID versions reject stale state changes.
- The dashboard, queues, forms, details, asset maintenance history, English/Arabic copy, and RTL/LTR layouts use real APIs and permission-aware controls.
- Corrective maintenance does not automatically change the asset operational status. Parts and material cost are integrated through Task #007; labor cost, attachments, notifications, and SLA automation remain future work.

## Frontend experience

- `app/globals.css` is the semantic token source for light and dark colors, typography, spacing, radii, shadows, focus, motion, shell dimensions, and responsive behavior. See `docs/DESIGN_SYSTEM.md` before adding UI.
- `ThemeProvider` supports light, dark, and system preferences. It persists only `nexa-theme`; access tokens remain memory-only and refresh secrets remain HttpOnly.
- The tenant and platform shells share a compact visual language while preserving separate authentication and authorization boundaries. Navigation exposes only implemented routes.
- Tenant and Platform Admin login use responsive split layouts, password visibility, clear busy/error states, and localized English/Arabic copy.
- Tenant login still requests the backend's tenant UUID. A friendlier selector requires an explicit non-enumerating discovery, invitation, or deep-link API and must not be approximated in the client.
- Platform login distinguishes an unreachable authentication service from the intentionally generic 401 credential response, which keeps local process failures from being misreported as password failures.
- The tenant landing page links only to working company, location, user, role, and session workflows and contains no fabricated metrics.

## Current foundation and limits

- Assets owns categories, manufacturers, models, assets, and append-only asset activity. Future maintenance and inventory modules must reference Asset UUIDs.
- Operational status (`Operational`, `UnderMaintenance`, `OutOfService`, `Standby`) is separate from lifecycle (`Active`, `Retired`). Retired assets retain history and cannot be edited until reactivated.
- Asset access is permission- and site-scoped. Relocation requires `assets.relocate` on source and destination sites. Platform Administrators receive no tenant asset access.
- Migration `20260928141221_AssetRegistry` was applied locally without recreating data. Its idempotent script is `artifacts/sql/Task004_AssetRegistry.idempotent.sql`.
- Migration `20260929143219_CorrectiveMaintenance` was applied locally without recreating data. Its idempotent script is `artifacts/sql/Task005_CorrectiveMaintenance.idempotent.sql`.

- Task #002 company/site/location behavior remains intact. Task #003 migration is `20260927202422_IdentitySessionsAndAccessAdministration`.
- Admin-supplied initial/reset passwords are temporary until invitation/email recovery exists. No fake email or unauthenticated reset endpoint exists.
- Local PostgreSQL 17 runs through Docker Compose, and all migrations were applied and verified on 2026-09-28. Tasks #003.6 and #003.7 required no schema change.
- Frontend tests cover localization, platform validation/lifecycle logic, and theme resolution. Task #003.7 frontend typecheck, lint, 5 suites/14 tests, and the 27-page production build pass.
- MFA, OIDC/SSO, device naming, subtree scopes, and business modules are deferred.
- Next recommended task: invitation/email delivery plus MFA/SSO preparation, or DataExchange groundwork.

## Preventive maintenance

- Preventive plans reference stable Asset UUIDs and copy the asset site for scope enforcement. Plans carry a fixed local start date/time, recurrence type and interval, next due instant, state, optional default technician, and UUID concurrency version.
- Daily and weekly schedules advance by calendar days; monthly, quarterly, semiannual, and annual schedules preserve the original day anchor. Short months clamp to month end and later months recover the anchor; February 29 becomes February 28 in non-leap years and returns to February 29 in leap years.
- The asset site's tenant time zone converts intended local dates to UTC. Invalid daylight-saving local times move forward one hour; ambiguous times use the smaller UTC offset. Completion time never shifts the schedule.
- Active-plan downtime catches up at most three occurrences per plan per run. Paused plans do not generate, and overdue dates are skipped prospectively when resumed. Archived plans are terminal.
- Generated PM work orders reuse the Task #005 assignment, start, activity, report, correction, verification, cancellation, and history engine. Type and nullable source links distinguish CM from PM without changing existing CM contracts.
- Plan checklist definitions are copied into work-order response rows. Later plan edits cannot change historical work. Required responses block report submission and failed responses require notes.
- `PreventiveMaintenanceScheduler` runs in an isolated scope per tenant and relies on a database unique occurrence key for multi-instance safety. Manual generation requires `maintenance.pm.generate`.
- Tenant Administrators receive seven `maintenance.pm.*` permissions through migration `20260929191659_PreventiveMaintenance`. All plan, generation, occurrence, work-order, asset, and technician operations enforce tenant and site scope.
- The localized UI is under `/{locale}/maintenance/pm`; Asset Details includes linked plans and combined maintenance history.
- Current limits: no drag-and-drop checklist ordering, notifications, meter-based triggers, holiday calendars, attachment storage, or automatic corrective work order from a failed checklist item.
- Recommended next task: Inventory and Spare Parts, including stock locations, reservations, PM/CM consumption, reorder rules, and cost capture.

## Inventory and spare parts

- Warehouses belong to existing Sites; warehouse stock locations form a separate optional hierarchy and never reuse facility locations. Parent constraints keep every node in one warehouse and application validation prevents cycles.
- Tenant-scoped parts use controlled units (`Piece`, `Box`, `Meter`, `Liter`, `Kilogram`) without conversions. SKU is unique per tenant; thresholds and preferred supplier prepare Procurement integration.
- Balances are per tenant/warehouse/location/part. Available quantity is on-hand minus reserved. PostgreSQL check constraints prevent negative on-hand, reserved, available, or cost values; UUID concurrency and serializable transactions prevent overspend.
- Costing is moving weighted average per part and warehouse, rounded to four decimals; financial movement totals round to two decimals using away-from-zero midpoint rounding. Issues preserve their historical cost. Cross-warehouse transfers move source cost into the destination weighted average without changing tenant value.
- Request UUIDs are unique per transactional resource and make receipt, issue, transfer, adjustment, reservation, and consumption retries idempotent.
- Reservations belong to one existing CM/PM work order and one balance. Partial consumption reduces on-hand and reserved together; release restores availability. Verification/closure and cancellation automatically release unused amounts.
- `WorkOrderPartConsumption` and inventory activities provide Asset → Work Order → Consumption → Movement traceability. CM and PM share the same implementation.
- Nine `inventory.*` permissions support tenant-wide or site-scoped warehouse access. Part catalog management requires tenant-wide authority; warehouse and stock mutations validate both source and destination sites.
- Localized routes are under `/{locale}/inventory`; work-order and Asset Details pages show real material usage.
- Known Inventory limits: one line per manual stock command, atomic completed transfers, no lots/serials/expiry, no unit conversions, and no landed-cost allocation.

## Procurement and purchasing

- Suppliers are tenant-wide master data with unique codes, status, commercial/contact fields, currency/payment terms, UUID concurrency, and spare-part links for supplier number, price, MOQ, lead time, and preferred-source guidance.
- Purchase requisitions are site-scoped and move through Draft, Submitted, Approved/Rejected, and PartiallyConverted/Converted. Submitted records are controlled; requester self-review is rejected server-side.
- Approved quantities can be split across supplier purchase orders without over-conversion. PR, PO, and GRN numbers use tenant/type/year sequence rows rather than `MAX()+1`.
- Purchase orders move through Draft, Issued, PartiallyReceived, FullyReceived, and Closed. Cancellation is limited to unreceived draft/issued orders; all totals are server calculated.
- PO receipts are multi-line and idempotent. One serializable EF transaction creates procurement history and calls Task #007 receiving, preserving atomic stock, movement, valuation, and PO updates.
- Nine `procurement.*` permissions support tenant and site grants. Supplier management is tenant-wide; workflow, dashboard, and receipt operations enforce site scope and warehouse/site compatibility.
- Migration `20260930231103_ProcurementAndPurchasing` is applied locally; deployment SQL is `artifacts/sql/Task008_ProcurementAndPurchasing.idempotent.sql`.
- Current limits: no discounts/tax allocation, landed cost, invoices, AP/payments/GL, FX accounting, RFQ, notifications, attachments, or supplier portal.
- Recommended Task #009: invoice and three-way-match readiness, or service-request and maintenance-planning analytics.

## Task #009 service requests

- Tenant categories control bilingual labels, defaults, required asset/location fields, activation, and ordering.
- Requester intake and My Requests use strict user ownership. Requester DTOs exclude internal notes, assignment history, and internal activity.
- Operational users triage, assign eligible site-authorized executors, add notes, execute non-asset work, resolve, close, reject, cancel, or reopen within the explicit state machine.
- Asset-linked conversion calls existing CM breakdown creation atomically and preserves the request/breakdown/work-order navigation chain.
- Migration: `20261001141730_ServiceRequestsAndRequestManagement`. Portal: `/{locale}/requests`.
