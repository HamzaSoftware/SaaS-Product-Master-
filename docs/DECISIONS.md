# Architecture Decisions

## ADR-001 — Modular monolith

- **Date:** 2026-09-27
- **Decision:** Build one ASP.NET Core modular monolith with explicit module folders/contracts.
- **Reason:** The product needs transactional consistency and fast iteration; current scale does not justify distributed operations.
- **Alternatives:** Microservices were rejected as premature. An unstructured single project was rejected because boundaries must support future separation.
- **Consequences:** Modules share a process/database but may not bypass each other's contracts. Extraction remains possible when an evidenced need appears.

## ADR-002 — Global users with tenant memberships

- **Date:** 2026-09-27
- **Decision:** A platform user identity is global; tenant access is represented by an active membership with tenant roles/scopes.
- **Reason:** One person may belong to multiple customers while credentials, MFA, and future SSO identities remain singular.
- **Alternatives:** Duplicating users per tenant complicates login/SSO; putting `TenantId` directly on the user prevents multi-tenant membership.
- **Consequences:** Login selects and verifies a tenant membership. Product roles are tenant roles, not ASP.NET Identity's global roles.

## ADR-003 — Layered tenant isolation

- **Date:** 2026-09-27
- **Decision:** Resolve tenant authority from signed identity, then enforce it through middleware, EF filters/save guards, and tenant-aware constraints.
- **Reason:** A single frontend filter or repository convention is too easy to bypass accidentally.
- **Consequences:** `IgnoreQueryFilters`, raw SQL, imports, and workers require explicit tenant constraints and security tests.

## ADR-004 — JWT bearer foundation

- **Date:** 2026-09-27
- **Decision:** Issue short-lived tenant-bound JWT access tokens after ASP.NET Core Identity password verification.
- **Reason:** The Next.js client and REST API are separate processes, and bearer auth leaves room for mobile/integration clients.
- **Alternatives:** Server cookies remain viable for a future browser BFF, but no BFF exists in this phase.
- **Consequences:** Refresh/session revocation and browser token storage strategy must be decided before production login UI. CSRF controls depend on that future storage/transport decision.

## ADR-005 — PostgreSQL and EF migrations

- **Date:** 2026-09-27
- **Decision:** Use PostgreSQL 17, EF Core 10, UUID identifiers, snake-case schema names, and explicit migrations.
- **Reason:** PostgreSQL provides mature relational integrity and future hierarchical/JSON/indexing options without adding another data store.
- **Consequences:** Migrations are reviewed artifacts and applied explicitly; API startup never changes production schema.

## ADR-006 — Locale in the frontend route

- **Date:** 2026-09-27
- **Decision:** Use `/en/...` and `/ar/...`, centralized dictionaries, logical CSS properties, and localized subtree direction.
- **Reason:** Locale-specific URLs make navigation deterministic and validate RTL from the first UI shell.
- **Consequences:** Every page requires a locale and all visible strings should move through dictionaries/message catalogs.

## ADR-007 — Flexible location adjacency list

- **Date:** 2026-09-27
- **Decision:** Model a site containing self-referencing typed location nodes of arbitrary reasonable depth.
- **Reason:** Fixed Site/Building/Floor/Room schemas do not fit all industries or future customer changes.
- **Alternatives:** Materialized paths/`ltree` are deferred until query measurements justify their extra maintenance.
- **Consequences:** Application services must prevent cycles and cross-tenant/site parents.

## ADR-008 — Asynchronous long-running work

- **Date:** 2026-09-27
- **Decision:** Large imports, PM generation, reports, notifications, and integrations will be durable background jobs with transactional handoff.
- **Reason:** Long HTTP requests are unreliable and cannot provide robust retry/progress semantics.
- **Consequences:** Add a worker/queue only with the first concrete workload; require idempotency, retries, dead-letter handling, audit, and tenant-aware limits.

## ADR-009 — Current stable platform versions

- **Date:** 2026-09-27
- **Decision:** Target .NET 10.0.12 LTS/SDK 10.0.112 and Next.js 16.3.6 with React 19.3.
- **Reason:** This is a new commercial foundation. .NET 8 approaches end of support, and earlier Next.js 16.3 releases have fixed critical advisories.
- **Consequences:** Developers must install the declared SDK. Package updates require build, test, and vulnerability verification.

## ADR-010 — Separate platform administration boundary

- **Date:** 2026-09-27
- **Decision:** Bootstrap the first platform administrator only through an explicit configuration-backed CLI switch, issue a distinct platform token, and restrict operator actions to `/api/v1/platform/*`.
- **Reason:** Tenant administration must never imply SaaS-operator authority, and a public bootstrap endpoint is unsafe.
- **Consequences:** Platform operators use `/api/v1/auth/platform-login`; tenant operations still require a tenant membership/token.

## ADR-011 — UUID concurrency and status lifecycle

- **Date:** 2026-09-27
- **Decision:** Mutable administration records carry a UUID version and use activate/deactivate operations instead of physical deletion.
- **Reason:** Configuration is concurrently editable and will acquire historical references.
- **Consequences:** Clients refresh after 409. A location cannot deactivate while an active descendant exists, and status changes never cascade silently.

## ADR-012 — Short access tokens with revocable rotating sessions

- **Date:** 2026-09-27
- **Decision:** Link short-lived JWTs to database sessions; keep the rotating refresh secret in an HttpOnly cookie and only its SHA-256 hash in PostgreSQL.
- **Reason:** Stateless JWTs cannot immediately honor membership, role, password, or session revocation.
- **Consequences:** Authenticated requests perform a live validation query. The web keeps access tokens in memory and makes at most one refresh retry.

## ADR-013 — Authorization version and bounded delegation

- **Date:** 2026-09-27
- **Decision:** Membership authorization changes rotate a UUID embedded in JWTs, and administrators may assign only permissions they hold tenant-wide.
- **Reason:** This closes stale-claim and privilege-escalation paths.
- **Consequences:** Role/scope changes require refresh. System roles and the final active administrator receive application-layer safeguards.

## ADR-014 — Separate browser shell for platform administration

- **Date:** 2026-09-28
- **Decision:** Host Platform Administration under `/{locale}/platform/*` with a dedicated provider and shell while reusing the shared in-memory token and HttpOnly refresh-session client.
- **Reason:** Platform operators need a browser workflow, while tenant navigation and tenant authentication state must not imply operator authority.
- **Alternatives:** Reusing the tenant shell was rejected because its context endpoint requires tenant claims. Persisting a separate token in browser storage was rejected because it weakens the established session strategy.
- **Consequences:** Restored sessions are verified through a platform-authorized API call before the portal renders. Backend platform policies remain authoritative; the frontend route guard is user experience only.

## ADR-015 — Semantic design tokens and preference-based themes

- **Date:** 2026-09-28
- **Decision:** Use one semantic token layer for light and dark themes, with a client provider that supports light, dark, and operating-system preference.
- **Reason:** Tenant and platform workflows need a coherent visual language across English, Arabic, authentication, tables, forms, and responsive layouts without duplicating theme-specific component code.
- **Alternatives:** Page-level colors and CSS inversion were rejected because they drift across modules and produce unreliable contrast.
- **Consequences:** Components consume semantic variables from `globals.css`. Only the non-sensitive `nexa-theme` preference is stored in the browser. New UI must follow `docs/DESIGN_SYSTEM.md` and validate both directions and themes.

## ADR-016 — Keep tenant UUID login until secure discovery exists

- **Date:** 2026-09-28
- **Decision:** Present the current tenant UUID as a workspace identifier and keep the existing login contract unchanged.
- **Reason:** Resolving a company name or slug in client code could reveal tenants or membership and would duplicate authentication behavior without a defined anti-enumeration contract.
- **Consequences:** A future friendly login flow requires a rate-limited, non-enumerating backend discovery endpoint, an invitation link, or a tenant-specific deep link. The UI may change only with that reviewed contract.

## ADR-017 — Separate asset status, lifecycle, and history

- **Date:** 2026-09-28
- **Decision:** Store controlled operational status separately from active/retired lifecycle and record significant changes in append-only asset activity alongside shared audit events.
- **Reason:** Equipment may be temporarily unavailable without being retired, while maintenance modules need durable identity and relocation/status history.
- **Consequences:** Retired assets retain references and cannot be edited or moved until reactivated. Future CM, PM, and Inventory records reference Asset UUIDs and do not copy asset master data.

## ADR-018 — Corrective maintenance state machine and separation of duties

- **Date:** 2026-09-29
- **Decision:** Keep corrective workflow transitions in one maintenance application service, retain append-only assignment/activity/review history, enforce reviewer independence, and leave asset operational status unchanged by maintenance closure.
- **Reason:** Browser-provided status is not authoritative, mutable history weakens auditability, and automatic asset status changes can conflict with operations outside a work order.
- **Consequences:** Every transition validates the current state and UUID version. The breakdown reporter cannot review their own report, and the report author cannot verify their own repair. Future asset-status automation must be an explicit cross-module policy with its own permission and audit trail.

## ADR-019 — Fixed-calendar preventive scheduling with shared execution

- **Date:** 2026-09-29
- **Decision:** Keep PM recurrence anchored to the intended local calendar and tenant time zone, persist generated UTC occurrences, and execute PM through the existing maintenance work-order aggregate using a type discriminator and nullable CM/PM source links.
- **Reason:** Calendar plans must not drift with completion time, monthly periods cannot be approximated as days, and duplicating assignment/report/verification logic would create inconsistent security and lifecycle behavior.
- **Policy:** Short months clamp to their final day and later months recover the original day; February 29 clamps to February 28 outside leap years. Invalid DST local times move forward one hour and ambiguous times use the smaller UTC offset. Active downtime creates at most three missed occurrences per plan per run; paused time is skipped when a plan resumes.
- **Consequences:** Generated checklist rows and scheduled dates preserve history. Database uniqueness makes generation safe under retries and concurrent API instances. The legacy `corrective_work_orders` table name remains an implementation detail for data compatibility; API type fields distinguish PM and CM.

## ADR-020 — Append-only inventory ledger and warehouse moving-average cost

- **Date:** 2026-09-30
- **Decision:** Mutate location balances only through transactional inventory commands, preserve every result in an append-only movement ledger, and value each part at one moving weighted-average cost per warehouse.
- **Reason:** Operational quantity, financial value, retry behavior, and work-order material cost must stay reconcilable while avoiding lot-level complexity before Procurement exists.
- **Policy:** Quantities and unit costs use four decimals; monetary totals use two decimals with midpoint rounding away from zero. Issues keep the current warehouse cost. Transfers preserve source value and blend it into a different destination warehouse. Request UUID uniqueness prevents duplicate commands.
- **Consequences:** Corrections use compensating adjustments rather than ledger edits. Stock locations within one warehouse share its current cost. Positive adjustments into zero stock have zero cost in this MVP because no source cost is supplied. Lots, expiry, landed cost, and purchase-order costing require a future decision.

## ADR-021 — Work-order reservations share the CM/PM lifecycle

- **Date:** 2026-09-30
- **Decision:** Link reservations and consumptions to the existing shared work-order UUID and release unused reservations transactionally on cancellation or verified closure.
- **Reason:** Separate CM and PM inventory engines would duplicate authorization, status, asset traceability, and costing rules.
- **Consequences:** Reservation reduces availability without reducing on-hand; consumption reduces both on-hand and reserved. Completed/cancelled orders cannot receive new inventory operations. Asset material history is derived through its work orders rather than a duplicate asset-part table.

## ADR-022 — Procurement receiving reuses Inventory inside one transaction

- **Decision:** PO receipt posting opens one serializable transaction on the shared scoped `PlatformDbContext` and invokes the existing Inventory receive path with the procurement receiving permission. Inventory joins an active transaction rather than nesting or committing it.
- **Reason:** Procurement status, physical stock, movement history, and moving-average value must not diverge. Request UUIDs and unique indexes protect retries; PO concurrency and quantity constraints protect the final remaining quantity.

## ADR-023 — Controlled PR/PO workflow and sequence numbers

- **Decision:** PR requester self-review is forbidden. Approved quantities may be partially converted across suppliers without over-conversion. PO over-receipt is rejected. Human numbers come from tenant/type/year sequence rows under serializable transactions; UUIDs remain primary keys.
- **Reason:** These rules provide a clear MVP approval boundary and safe identifiers without a workflow-builder engine or unsafe `MAX()+1` numbering.

## ADR-024 — Requester-safe service requests and CM conversion

- **Decision:** Keep requester-owned reads separate from operational reads, store internal/requester-visible notes explicitly, and convert asset-linked requests by invoking the existing corrective-maintenance service inside one outer transaction.
- **Reason:** Requesters must not receive internal coordination data, while conversion must preserve one auditable request-to-breakdown link without duplicating CM workflow logic.
- **Consequences:** My Requests always filters by the authenticated user. Assignment checks active membership, execution permission, and site scope. A converted request is terminal; its linked breakdown proceeds through established CM review and work-order creation.
