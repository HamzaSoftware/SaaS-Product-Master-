# Architecture

## System shape

The repository is a modular monolith with independently deployed web and API processes and one PostgreSQL database.

```text
Browser
  -> Next.js web (localized UI and application shell)
  -> /api/v1 REST API (authentication, tenant context, authorization)
  -> PostgreSQL (platform and tenant-owned data)
```

The web application never enforces data isolation by itself. It renders the user's experience; the API remains the authority for tenant, identity, permissions, and future scope checks.

## Backend boundaries

`Modules/Identity` owns platform users, tenant roles, permission definitions, role-permission grants, authentication, and authorization-facing endpoints. `Modules/Tenancy` owns tenants, memberships, and request tenant resolution. These foundations are implemented.

Organization, Locations, Assets, Maintenance, PreventiveMaintenance, and Inventory are implemented module boundaries. Notifications, DataExchange, and Integrations remain planned. New work should live inside its module and expose small contracts rather than reaching into another module's persistence internals.

`BuildingBlocks` namespaces contain deliberately small cross-cutting mechanisms: EF context/conventions, errors, correlation IDs, API paging contracts, and the module catalog. Shared code must not become a dumping ground for domain logic.

## Tenant isolation

Login accepts a tenant ID only as a lookup input. The API verifies the password, active user membership, and active tenant before issuing a signed JWT containing `tenant_id` and `membership_id`. Later requests resolve the tenant from that verified claim. If `X-Tenant-Id` is supplied and conflicts with the claim, middleware returns 403; an anonymous header never establishes tenant authority.

`PlatformDbContext` adds query filters to every currently implemented tenant-owned entity. Its save convention requires a tenant context, assigns an empty `TenantId` on new entities, and rejects mismatched writes/updates/deletes. Tenant-aware unique indexes prevent collisions inside a tenant. Platform-level `Tenant`, `PlatformUser`, and `PermissionDefinition` records are not filtered.

Code that deliberately calls `IgnoreQueryFilters` is security-sensitive. The login query is the only current use and immediately constrains both `TenantId` and `UserId` before issuing identity.

Future modules must implement `ITenantOwned` and register a query filter until a generalized, tested convention replaces the explicit filters. Integration tests must prove cross-tenant reads and writes fail.

## Authentication and authorization

ASP.NET Core Identity stores password hashes and supports lockout/token providers. Tenant and platform login issue short-lived HMAC-signed JWTs linked to revocable database sessions. Production rejects the checked-in placeholder signing key. MFA and OIDC/SSO can be added around the platform user without changing tenant memberships.

Authorization data is tenant-specific:

```text
PlatformUser -> TenantMembership -> MembershipRole + Scope -> TenantRole -> PermissionDefinition
```

The current token flattens distinct permission codes into `permission` claims. `PlatformAccess` demonstrates a policy requiring authentication, a tenant, and `Platform.Access`. Future resource operations should use permission requirements plus a scope evaluator that checks tenant-wide, site, department, or custom scope IDs against the target resource. Roles such as “Admin” must not replace permission checks.

## Frontend

The App Router uses a required `[locale]` segment. Dictionaries are centralized in `src/i18n`; Arabic sets `dir="rtl"` on the localized application subtree and English sets `dir="ltr"`. Layout CSS uses logical inline properties to behave correctly in both directions.

The tenant and platform shells own their separate navigation and session context while sharing the Nexa visual foundation. Navigation exposes implemented workflows only. `src/app/globals.css` defines semantic light/dark tokens and responsive layout rules; `ThemeProvider` resolves light, dark, or system preference and persists only the non-sensitive appearance choice. General controls and state patterns live under `src/components/ui` and should be extended before creating page-specific variants. The detailed contract is in `docs/DESIGN_SYSTEM.md`.

Tenant login continues to send the UUID required by the existing authentication API. A company-name or slug selector must be backed by an intentional discovery, invitation, or deep-link contract with anti-enumeration and rate-limit behavior; the frontend does not infer or expose tenant identity independently.

## Location model

The proposed Locations model is:

```text
Tenant -> Site -> LocationNode(parent?) -> LocationNode ...
```

`LocationNode` will hold `TenantId`, `SiteId`, optional `ParentId`, a type reference, name/code, ordering, archive state, and concurrency token. Types may be seeded or tenant-defined. The service layer must reject cycles, cross-site/cross-tenant parents, and unreasonable depth. Materialized paths or PostgreSQL `ltree` may be added only after query requirements are measured; adjacency lists are the starting point.

## DataExchange

DataExchange will own upload metadata, templates, mappings, validation results, import jobs/history, error artifacts, and export jobs. The request path stores a safe file reference and creates a job; parsing/validation/import run asynchronously in bounded batches. Domain modules expose import commands/validators so DataExchange does not write their tables directly. Confirmation, results, and every state transition are auditable.

## Integrations

Integrations is the anti-corruption boundary for ERP, HR, finance, CMMS, IoT/BMS, webhooks, and external APIs. Provider adapters implement internal ports and map provider contracts to platform commands/events. Credentials belong in a secret store. Retry/idempotency state and external identifiers stay in the integration boundary instead of domain entities where possible.

## Background processing

When the first durable workload arrives, add a worker process hosted from the same modular codebase plus a PostgreSQL-backed queue or established job library. Use a transactional outbox for domain-to-background handoff, idempotency keys, retry/backoff, dead-letter handling, cancellation, progress, and tenant-aware concurrency limits. Do not hold an HTTP connection open for large imports, PM generation, scheduled reports, or integrations.

## Audit

Future auditable commands emit a structured audit record containing tenant, actor, operation, entity type/ID, UTC time, correlation/reference, and carefully selected old/new values. Sensitive fields must be redacted at capture time. Store audit records append-only through one module-facing service; avoid an opaque reflection-based “audit everything” mechanism.

## Security and operations

- Production assumes HTTPS/HSTS and explicit allowed CORS origins.
- JWT signing keys, database credentials, provider secrets, and uploaded files never enter source control.
- Login is rate limited. Add broader endpoint quotas when traffic patterns are known.
- RFC 7807 responses omit production exception details and include correlation IDs.
- JSON console logs are structured; tenant/user scopes should be added only after redaction rules are defined.
- File imports will require extension/signature/size checks, malware scanning, isolated object storage, and generated storage keys.

## Task #002 implemented architecture

Organization and Locations now own company profiles, sites, configurable location types, and the adjacency-list location hierarchy. Analytics owns append-only audit events. Tenant JWTs carry permission plus tenant/site grant claims; `SiteScopeAuthorizer` evaluates the target site. A separate `platform_admin` claim and explicit `/api/v1/platform/*` routes keep operator actions outside tenant-domain access.

`TenantAdministrationService` contains ownership, duplicate, hierarchy, move, lifecycle, concurrency, and audit rules. HTTP is one adapter; future DataExchange jobs must initialize a trusted tenant context and reuse these application rules. Composite tenant/site foreign keys backstop the service. Paths are assembled by following ancestors, avoiding denormalized path maintenance; `ltree` remains a measured future option.

## Task #003 identity and session architecture

Access JWTs are short-lived snapshots linked to a server-side session ID and membership authorization version. JWT validation performs a live session/user/membership/version check, so revocation and administration changes take effect before token expiry. Refresh secrets use 256 bits of randomness, are SHA-256 hashed at rest, rotate after use, and travel only in an HttpOnly SameSite cookie (`Secure` outside development).

`IdentityAdministrationService` owns user, membership, role, scope, bounded-delegation, and final-admin rules. `CurrentAccessService` is the reusable authorization query surface for future modules. Platform sessions have no tenant claims or tenant permissions.

## Task #003.6 platform portal architecture

The localized `/{locale}/platform/*` route tree uses its own client provider and shell while sharing the established API client, refresh rotation, and logout behavior. Its access token is module memory only. On reload, the provider exchanges the HttpOnly refresh cookie and then calls the platform-protected tenant list; it considers the browser authenticated only when that call succeeds. This prevents a valid tenant session from entering the operator portal without duplicating server authorization in the UI.

The portal calls the existing platform endpoints for tenant list, transactional tenant/admin creation, and status changes. Tenant details reuse list DTOs because the backend does not expose a separate detail route. No migration or backend contract was added.

## Task #004 Asset Registry architecture

`Modules/Assets` owns categories, manufacturers, models, assets, controlled status/lifecycle transitions, and append-only asset activity. Restrictive composite tenant foreign keys connect assets to categories, sites, locations, manufacturers, and models. Validation requires active references, a location in the selected site, an active tenant membership for responsibility, valid dates/money, and UUID concurrency.

Asset access is permission- and site-scoped. Cross-site relocation checks both source and destination. The shared audit stream records commands while `asset_activities` provides asset-focused history for future corrective and preventive maintenance.

## Task #005 Corrective Maintenance architecture

`Modules/Maintenance` owns breakdown reports and reviews, corrective work orders, assignment history, append-only activities, maintenance reports, and verification reviews. It references Asset, Site, Location, and Membership UUIDs while retaining its own workflow state and snapshots needed for durable history.

The application service is the state-machine boundary. It validates tenant and site scope, permissions, active assets, eligible technicians, separation of duties, report completeness, and UUID concurrency before one EF transaction commits each command. Query filters and composite tenant foreign keys provide additional isolation. The API never trusts client status values.

Breakdowns progress `Submitted -> Approved/Rejected -> ConvertedToWorkOrder`. Work orders progress `Open -> Assigned -> InProgress -> PendingVerification -> Completed`, or return to correction and reassignment before execution resumes. Cancellation is explicit. Asset operational status remains an independent asset-module decision.

## Task #006 Preventive Maintenance architecture

`PreventiveMaintenancePlan` owns fixed-calendar scheduling and checklist definitions. `PreventiveMaintenanceOccurrence` is the durable, unique `(tenant, plan, scheduled due)` generation record. A generated order uses the existing `CorrectiveWorkOrder` persistence aggregate with a `Type` discriminator, nullable breakdown source, PM plan/occurrence links, scheduled due instant, and snapshotted checklist responses. The legacy class and table names remain to preserve Task #005 data and API compatibility.

`PreventiveMaintenanceService` is the plan, recurrence, generation, calendar, and checklist boundary. The existing corrective service remains the single assignment/execution/report/verification state machine and selects CM or PM permissions from work-order type. This keeps lifecycle rules, activities, reports, reviewer separation, site scope, and concurrency consistent.

The hosted scheduler uses configuration for enablement, interval, and generation horizon, creates an independent dependency-injection scope for each tenant, isolates errors, and honors cancellation. Application checks reduce redundant work; the database occurrence constraint is the final concurrency guard when multiple API instances run. Checklist definitions are immutable with respect to generated orders because generation copies them into response rows.

## Task #007 Inventory architecture

`Modules/Inventory` owns warehouses, warehouse-only storage locations, spare parts, stock balances, operational documents, work-order reservations/consumptions, and the append-only movement ledger. It references existing Tenant, Site, User, Asset-through-WorkOrder, and shared CM/PM WorkOrder identities through restrictive tenant-aware foreign keys.

`InventoryService` is the only balance-mutation boundary. It validates permission and site scope, active master data, work-order state, request idempotency, stock availability, and UUID versions, then posts the business document, balance change, ledger movement, maintenance activity, and audit event in one serializable transaction. Database check constraints and concurrency tokens remain the final race guard.

Moving average is scoped to part and warehouse. Location balances in that warehouse share the current average; receipts recalculate it, issues preserve it, and cross-warehouse transfers blend the transferred source value into the destination. Historical movement and consumption costs never change. Work-order completion and cancellation call the inventory lifecycle hook to release remaining reservations before the shared maintenance transaction commits.

## Procurement transaction boundary

Procurement is a module beside Inventory, not a second stock system. Supplier, PR, PO, receipt, sequence, and activity tables belong to Procurement. Physical receipt posting opens a serializable transaction and calls `IInventoryService.ReceiveForProcurement` on the same scoped `PlatformDbContext`. Inventory recognizes the existing transaction and does not open or commit another one. Procurement commits only after every stock receipt, movement, valuation change, PO quantity/status change, and activity saves successfully.

Document numbers use a tenant/type/year sequence row protected by the serializable transaction, a UUID concurrency token, and database unique indexes. UUID remains the technical identifier. Cross-module foreign keys include `tenant_id` where the principal supports it, use restrictive deletion, and remain covered by global tenant filters.

PR review and PO/receipt transitions live in `ProcurementService`; endpoints only translate HTTP contracts. Requester self-review, over-conversion, over-receipt, stale versions, inactive references, site scope, and warehouse/site compatibility are server-enforced.

## Service request module boundary

`Modules/ServiceRequests` owns category configuration, request intake, requester-safe reads, triage, assignment, direct execution, lifecycle transitions, notes, and activity. It references existing Sites, facility Locations, Assets, Memberships, and CM Breakdowns through stable UUIDs.

CM conversion calls the existing corrective-maintenance application service while the service-request service owns the outer serializable transaction. It creates a breakdown and link atomically; the existing CM review and work-order rules remain authoritative.
