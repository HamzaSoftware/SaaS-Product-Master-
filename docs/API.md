# API

## Base and versioning

Public application endpoints use the `/api/v1` URL prefix. Breaking contracts require a new major URL version; additive fields/endpoints remain in the current version. Health endpoints are operational and intentionally unversioned.

## Implemented endpoints

| Method | Path | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/v1` | Anonymous | API identity/version |
| POST | `/api/v1/auth/login` | Anonymous, rate limited | Verify password and tenant membership; issue tenant-bound JWT |
| GET | `/api/v1/context` | `PlatformAccess` policy | Return authenticated user, tenant, and permissions |
| GET | `/health/live` | Anonymous | Process liveness |
| GET | `/health/ready` | Anonymous | PostgreSQL readiness |

## Authentication

Send the access token as `Authorization: Bearer <token>`. The short-lived token includes standard subject/email/JTI claims plus session and authorization-version claims and, for tenant sessions, `tenant_id`, `membership_id`, and repeated `permission` claims. Login deliberately returns the same 401 title for an invalid password, inactive/missing membership, or inactive tenant to limit account discovery.

The browser Platform Administration Portal uses `POST /api/v1/auth/platform-login`, restores the revocable platform session through the shared refresh endpoint, and calls `/api/v1/platform/tenants` with an in-memory bearer token. No Swagger/OpenAPI UI is configured.

The optional `X-Tenant-Id` header is only a consistency signal. It must match the signed claim and never creates tenant authority.

## Request and response conventions

- JSON property names use the ASP.NET Core web defaults (`camelCase`).
- Successful endpoints return typed JSON resources. Avoid a redundant universal envelope unless a concrete client requirement emerges.
- `X-Correlation-Id` is accepted or generated and returned on responses.
- Validation and errors use RFC 7807 `application/problem+json` with `status`, `title`, `type`, optional field `errors`, and `correlationId`.
- Production 500 responses omit exception details. Logs retain the exception with correlation context.
- Authentication failures use 401; authenticated callers without tenant/permission/scope use 403; missing resources use 404; concurrency conflicts should use 409.

Example validation response:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "credentials": ["Email, password, and tenant ID are required."]
  },
  "correlationId": "..."
}
```

## Collection conventions

`PageRequest` and `PagedResponse<T>` establish defaults for future collection endpoints: one-based `page`, `pageSize` default 25 and maximum 200, optional `search`, and a validated `sort` expression. Each module must define its allow-listed filters and sortable fields. Stable ordering with a unique tie-breaker is mandatory. Cursor pagination may replace page/offset for high-volume feeds when measured requirements justify it.

## Security conventions

Validate transport DTOs at the boundary and domain invariants in the module. Never bind an incoming tenant ID onto a tenant-owned aggregate. Permission and future scope policies run server-side for each operation. File endpoints will use separate upload limits/content validation and asynchronous processing.

## Localization

API machine contracts currently use stable English codes/titles. Future validation/domain errors should carry stable error codes and localizable parameters so the web client can translate them. Do not store translated prose as the only error identity.

## Task #002 endpoints

| Method | Path | Access |
| --- | --- | --- |
| POST | `/api/v1/auth/platform-login` | Controlled platform administrator |
| GET/POST | `/api/v1/platform/tenants` | `Tenant.View` / `Tenant.Create` platform permission |
| PATCH | `/api/v1/platform/tenants/{id}/status` | Platform administrator |
| GET/PUT | `/api/v1/company` | Tenant-wide `Company.View` / `Company.Edit` |
| GET/POST/PUT | `/api/v1/sites[/{id}]` | Site permissions and tenant/site scope |
| PATCH | `/api/v1/sites/{id}/status` | Site activate/deactivate grant |
| GET/POST/PUT | `/api/v1/location-types[/{id}]` | Tenant-wide location-type permissions |
| PATCH | `/api/v1/location-types/{id}/status` | Tenant-wide activate/deactivate grant |
| GET/POST/PUT | `/api/v1/locations[/{id}]` | Location permission and matching site scope |
| PATCH | `/api/v1/locations/{id}/move` | Same-site move permission |
| PATCH | `/api/v1/locations/{id}/status` | Location activate/deactivate grant |

Site lists accept `search`, `page`, and `pageSize`. Location lists require `siteId`. Status and update bodies carry `version`; conflicts return RFC 7807 status 409. Tenant IDs are resolved server-side. There are no delete routes. Cross-site location moves are explicitly rejected.

## Task #003 endpoints

- Authentication: `POST /api/v1/auth/login`, `/platform-login`, `/refresh`, `/logout`, `/logout-all`, and `/change-password`.
- Own sessions: `GET /api/v1/auth/sessions`, `DELETE /api/v1/auth/sessions/{id}`.
- Users: `GET/POST /api/v1/administration/users`, `GET/PUT /{id}`, `PATCH /{id}/status`, `PUT /{id}/access`, and `POST /{id}/reset-password`.
- Roles: `GET/POST /api/v1/administration/roles`, `PUT /{id}`, `PATCH /{id}/status`, and `GET /permissions`.

The refresh secret is an HttpOnly cookie; clients hold access tokens in memory. Login failures are generic. User lists support search and paging. Mutable contracts carry UUID versions. Tenant IDs are not accepted in tenant-owned administration DTOs.

## Task #003.6 browser client

- English Platform Admin login: `/en/platform/login`; Arabic: `/ar/platform/login`.
- The platform dashboard consumes existing `GET/POST /api/v1/platform/tenants` contracts.
- Tenant details consume the list response and `PATCH /api/v1/platform/tenants/{id}/status` with the current UUID version.
- A restored tenant session cannot authorize the platform tenant request, so the platform provider clears its in-memory access token and returns to platform login.

## Task #004 Asset API

- `GET/POST /api/v1/assets`; `GET/PUT /api/v1/assets/{id}`.
- `GET /api/v1/assets/dashboard`; `GET /api/v1/assets/{id}/history`.
- `PATCH /api/v1/assets/{id}/status`, `/relocation`, and `/lifecycle`.
- `GET/POST/PUT/PATCH status` under `/api/v1/asset-categories`, `/api/v1/manufacturers`, and `/api/v1/asset-models`.

Asset list supports paging, text search, category/site/location, operational status, and lifecycle filters. Mutable requests carry UUID versions and stale writes return 409 ProblemDetails. Permissions are `assets.view`, `assets.create`, `assets.update`, `assets.manage-master-data`, `assets.change-status`, `assets.relocate`, and `assets.retire`.

## Task #005 Corrective Maintenance API

- Breakdowns: `GET/POST /api/v1/breakdowns`, `GET /api/v1/breakdowns/assets`, `GET /api/v1/breakdowns/{id}`, and `POST /api/v1/breakdowns/{id}/review`.
- Work orders: `GET/POST /api/v1/work-orders`, `GET /api/v1/work-orders/dashboard`, `GET /api/v1/work-orders/technicians`, and `GET /api/v1/work-orders/{id}`.
- Execution: `PATCH /assignment`, `POST /start`, `POST /activities`, `PUT /maintenance-report`, and `POST /maintenance-report/submit` below a work-order resource.
- Control: `POST /verification` and `POST /cancel` below a work-order resource.
- Asset history: `GET /api/v1/assets/{assetId}/maintenance-history`.

List endpoints accept status/site/priority/asset/assignee/search filters as applicable and use the shared paging response. Mutable aggregate requests carry UUID versions; stale or unsafe transitions return 409. Permissions are `maintenance.breakdowns.view/create/review` and `maintenance.workorders.view/create/assign/execute/verify/cancel`. Site-scoped grants are enforced by the API.

## Task #006 Preventive Maintenance API

- Plans: `GET/POST /api/v1/preventive-maintenance/plans`, `GET/PUT /plans/{id}`, `PATCH /plans/{id}/status`, and `GET /plans/{id}/occurrences`.
- Planning: `GET /api/v1/preventive-maintenance/dashboard`, `GET /calendar`, `GET /assets`, and protected `POST /generate`.
- PM execution lists: `GET /api/v1/preventive-maintenance/work-orders` and `GET /technicians`.
- Checklist execution: `PUT /api/v1/work-orders/{id}/preventive-checklist`; all other assignment, start, notes, report, submit, verification, correction, cancellation, and detail operations reuse `/api/v1/work-orders/{id}`.

Plan lists support search, site, asset, state, and paging. Calendar and occurrence queries accept UTC ranges; PM work-order lists accept workflow filters. Mutable requests carry UUID versions and return the established validation/409 ProblemDetails shapes. Permissions are `maintenance.pm.view/create/update/manage/generate/execute/verify`; every resource is tenant-filtered and site-authorized.

## Task #007 Inventory API

- Overview and reads: `GET /api/v1/inventory/dashboard`, `/balances`, and `/movements`.
- Warehouses: `GET/POST /api/v1/inventory/warehouses`, `GET/PUT /warehouses/{id}`, `PATCH /warehouses/{id}/status`; locations use `GET/POST /locations`, `PUT /locations/{id}`, and `PATCH /locations/{id}/status`.
- Parts: `GET/POST /api/v1/inventory/parts`, `GET/PUT /parts/{id}`, and `PATCH /parts/{id}/status`.
- Immutable operations: `POST /api/v1/inventory/receipts`, `/issues`, `/transfers`, and `/adjustments`.
- Reservations: `POST /api/v1/inventory/reservations`, `/reservations/{id}/consume`, and `/reservations/{id}/release`.
- Maintenance integration: `GET /api/v1/work-orders/{id}/materials` and `GET /api/v1/assets/{id}/part-consumptions`.

Mutation DTOs carry a request UUID for retry safety; mutable master data and reservations carry concurrency versions. List endpoints support paging and warehouse/part/site/search filters. Errors use the existing validation, authorization, not-found, and 409 ProblemDetails behavior. Permissions are `inventory.view`, `parts.manage`, `warehouses.manage`, `receive`, `issue`, `transfer`, `adjust`, `reserve`, and `audit`.

## Procurement API

All routes require a tenant session and enforce permission plus tenant/site scope in the service.

- `GET /api/v1/procurement/dashboard`
- `GET|POST /api/v1/procurement/suppliers`; `GET|PUT /suppliers/{id}`; `PATCH /suppliers/{id}/status`; `PUT /suppliers/{id}/parts`
- `GET|POST /api/v1/procurement/requisitions`; `GET|PUT /requisitions/{id}`; `POST /requisitions/{id}/submit`; `POST /requisitions/{id}/review`
- `GET|POST /api/v1/procurement/purchase-orders`; `GET|PUT /purchase-orders/{id}`; `POST /purchase-orders/{id}/issue|cancel|close`
- `POST /api/v1/procurement/purchase-orders/{id}/receipts`; `GET /api/v1/procurement/receipts`
- `GET /api/v1/procurement/activities/{entityType}/{entityId}`

Receipt POST requires a client `requestId`, current PO version, valid destination stock location, and one or more lines. Repeating the request returns the existing receipt. The server rejects over-receipt and calculates totals.

## Service Requests API

All routes require a tenant session. Application services enforce tenant, site, ownership, assignment, and permission boundaries.

- `GET /api/v1/service-requests/dashboard`
- `GET|POST /api/v1/service-requests`; `GET /service-requests/{id}`; `GET /service-requests/{id}/similar`
- `POST /service-requests/{id}/submit|start|resolve|close|reopen|reject|cancel|convert-to-maintenance`
- `PATCH /service-requests/{id}/triage|assignment`; `POST /service-requests/{id}/notes`
- `GET /api/v1/service-requests/assignees?siteId=...`
- `GET|POST /api/v1/service-requests/categories`; `PUT /service-requests/categories/{id}`
- `GET /api/v1/assets/{assetId}/service-requests`

`mine=true` uses strict requester ownership and returns only requester-visible notes; internal notes, assignment history, and internal activity are omitted. Every mutable transition requires the current UUID version.
