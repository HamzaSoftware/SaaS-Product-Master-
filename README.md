# SaaS Platform Foundation

Production-oriented foundation for a multilingual, multi-tenant B2B operations platform. The current web experience includes a bilingual tenant workspace, a separate Platform Administration portal, an asset registry, corrective and preventive maintenance, inventory and spare-parts management, procurement and purchasing, and the responsive Nexa design system with light and dark themes.

Future coding agents should read [`docs/PROJECT_CONTEXT.md`](docs/PROJECT_CONTEXT.md) first.

UI foundations, tokens, component patterns, RTL rules, and extension guidance are documented in [`docs/DESIGN_SYSTEM.md`](docs/DESIGN_SYSTEM.md).

## Stack

- Web: Next.js 16.3, React 19.3, strict TypeScript, Tailwind CSS 4.3
- API: ASP.NET Core 10, C# 14, EF Core 10, REST
- Database: PostgreSQL 17
- Architecture: modular monolith in a monorepo

## Repository

```text
apps/web/        Next.js application and internal UI foundation
apps/api/        ASP.NET Core API and backend tests
docs/            Architecture and AI handoff documentation
infrastructure/  Reserved for deployment infrastructure
scripts/         Reserved for repeatable development/operations scripts
```

## Prerequisites

- Node.js 22 or newer
- .NET SDK 10.0.112 or a compatible newer 10.0 feature band
- Docker Desktop with Docker Compose, or a PostgreSQL 17 instance

## Local setup

Start PostgreSQL:

```powershell
Copy-Item .env.example .env
docker compose up -d postgres
```

Restore the API, apply the initial migration, and start it:

```powershell
dotnet tool restore
dotnet restore SaaSPlatform.sln
dotnet ef database update --project apps/api/src/Platform.Api --startup-project apps/api/src/Platform.Api
dotnet run --project apps/api/src/Platform.Api --launch-profile http
```

The development connection string targets `localhost:5432`. Override it with `ConnectionStrings__PlatformDatabase`. In deployed environments, supply a random secret of at least 32 bytes through `Authentication__SigningKey`; the checked-in value is an obvious development placeholder and production startup rejects it.

In a second terminal, start the web application:

```powershell
Set-Location apps/web
Copy-Item .env.example .env.local
npm ci
npm run dev
```

On Windows systems where PowerShell blocks `npm.ps1`, run the installed `npm.cmd` entry point instead. This avoids changing the machine execution policy.

Open `http://localhost:3000/en` or `http://localhost:3000/ar`. The API listens on `http://localhost:5080` in the supplied launch profile.

## Validation

```powershell
dotnet build SaaSPlatform.sln --no-restore
dotnet test SaaSPlatform.sln --no-build --no-restore

Set-Location apps/web
npm run typecheck
npm run lint
npm test
npm run build
npm audit
```

Health endpoints:

- `GET /health/live` checks that the process can serve requests.
- `GET /health/ready` checks PostgreSQL connectivity and returns 503 when the database is unavailable.
- `GET /api/v1` returns API identity/version information.

## Database commands

Create a migration:

```powershell
dotnet ef migrations add MigrationName --project apps/api/src/Platform.Api --startup-project apps/api/src/Platform.Api --output-dir BuildingBlocks/Infrastructure/Migrations
```

Apply migrations only as an explicit deployment or development step. Application startup does not mutate the database.

## Authentication status

Tenant and platform login issue short-lived access JWTs tied to revocable server-side sessions. The rotating refresh secret is stored as an HttpOnly cookie and only its SHA-256 hash is persisted. Membership, role, and scope changes invalidate stale access immediately. The web holds access tokens in memory only. Tenant login is available at `/en/login` and `/ar/login`; IAM screens are under `/{locale}/administration/users`, `/{locale}/administration/roles`, and `/{locale}/settings/security`.

The appearance control supports light, dark, and operating-system themes and stores only that non-sensitive preference in `localStorage`. English and Arabic share semantic design tokens and mirror through logical CSS properties.

## Configuration

Environment variables use ASP.NET Core's double-underscore convention, for example:

```text
ConnectionStrings__PlatformDatabase=Host=localhost;Port=5432;Database=saas_platform;Username=saas;Password=...
Authentication__Issuer=saas-platform-api
Authentication__Audience=saas-platform-web
Authentication__SigningKey=<secret-at-least-32-bytes>
Cors__AllowedOrigins__0=https://app.example.com
```

Do not commit `.env`, `.env.local`, real credentials, tokens, or signing keys.

## First platform administrator

Apply migrations first, then run the explicit one-shot bootstrap with secrets supplied by configuration:

```powershell
$env:Bootstrap__Email = "operator@example.com"
$env:Bootstrap__Password = "use-a-secret-manager-value"
$env:Bootstrap__DisplayName = "Platform Operator"
dotnet run --project apps/api/src/Platform.Api -- --bootstrap-platform-admin
```

The command is idempotent for the same email. It refuses a different second platform administrator and refuses to convert an existing tenant user. Remove the bootstrap variables after use. No public platform-admin creation endpoint exists.

Platform administrators sign in at `http://localhost:3000/en/platform/login` or `http://localhost:3000/ar/platform/login`. The protected portal lists tenants, creates the tenant/company profile and initial Tenant Admin transactionally, and manages tenant activation. Tenant administrators sign in through `http://localhost:3000/en/login` or `/ar/login` using the tenant UUID shown after creation. No Swagger/OpenAPI UI is configured.

If Platform Admin sign-in reports that the authentication service is unavailable, verify `http://localhost:5080/health/ready` returns HTTP 200 and confirm this web application, rather than another local Next.js project, owns port 3000. A 401 response from the login endpoint remains a generic credential failure by design. Do not bootstrap another administrator or reset the database while diagnosing local process or port conflicts.

Task #002 administration routes remain available for company, sites, location types, and locations. Task #003 removes the temporary `sessionStorage` token behavior.

## Asset registry

Task #004 adds tenant-isolated asset categories, manufacturers, models, asset registration, search/filtering, details, operational status, relocation, retirement/reactivation, responsible users, warranty/purchase fields, and append-only history. Tenant users open it at `/{locale}/assets`; master data is under `/{locale}/assets/master-data`.

Task #005 adds corrective maintenance from breakdown intake through review, work-order assignment, technician execution/reporting, independent verification, return for correction, and closure. Start at `/{locale}/maintenance`; technician work is at `/{locale}/maintenance/my-work` and verification is at `/{locale}/maintenance/verification`.

## Preventive maintenance

Task #006 adds asset-linked preventive plans, fixed-calendar recurrence, checklist snapshots, bounded automatic generation, and PM execution through the shared maintenance work-order lifecycle. Start at `http://localhost:3000/en/maintenance/pm` or `http://localhost:3000/ar/maintenance/pm`; plans, the due calendar, and PM work orders are below that route.

The API hosted scheduler is configured under `PreventiveMaintenanceScheduler` in `apps/api/src/Platform.Api/appsettings.json`. It is enabled locally, checks every 60 minutes, generates seven days ahead, and creates at most three missed occurrences per plan per run. Database uniqueness makes repeated or concurrent generation idempotent. The protected `POST /api/v1/preventive-maintenance/generate` endpoint supports an authorized manual run.

Migration `20260929191659_PreventiveMaintenance` is applied to the existing local database. Its deployment script is `artifacts/sql/Task006_PreventiveMaintenance.idempotent.sql`; do not delete the Compose volume when applying it elsewhere.

## Inventory and spare parts

Task #007 adds site-linked warehouses, hierarchical stock locations, a tenant spare-parts catalog, balances, receiving, issuing, atomic transfers, adjustments, work-order reservations/consumption, low-stock reporting, warehouse moving-average valuation, and an immutable movement ledger. Open `http://localhost:3000/en/inventory` or `http://localhost:3000/ar/inventory`.

Every stock command requires a client-generated request UUID. Retrying the same command returns the existing transaction instead of posting stock twice. Quantities and costs use decimal columns; balance updates run in serializable transactions and retain UUID concurrency plus database nonnegative constraints. Unused reservations release automatically when a CM or PM work order is cancelled or verified closed.

Migration `20260930143940_InventoryAndSpareParts` is applied without resetting existing data. Its deployment script is `artifacts/sql/Task007_InventoryAndSpareParts.idempotent.sql`.

## Procurement and purchasing

Task #008 adds supplier master data and supplier-part pricing, multi-line purchase requisitions, independent approval/rejection, controlled split conversion into purchase orders, issue/cancel/close transitions, partial and full PO receiving, tenant-aware document sequences, and immutable procurement activity. Start at `http://localhost:3000/en/procurement` or `http://localhost:3000/ar/procurement`.

Goods receipts run in one serializable database transaction and call the Task #007 inventory receiver. A posted receipt creates the stock receipt and movement, updates on-hand and warehouse moving-average valuation, updates PO quantities/status, and writes procurement history atomically. The client request UUID and database uniqueness make retries idempotent; over-receipt is rejected.

Migration `20260930231103_ProcurementAndPurchasing` is applied without resetting existing data. Its deployment script is `artifacts/sql/Task008_ProcurementAndPurchasing.idempotent.sql`.


## Service requests and request management

Task #009 adds tenant-configured bilingual service categories, requester-owned intake, triage, scoped assignment, direct execution, resolution/closure, immutable notes and activity, duplicate awareness, and conversion into the existing corrective-maintenance workflow. Open the module at `/{locale}/requests`; requester, triage, assigned-work, and category views are below that route.

Request numbers use the existing tenant/type/year sequence store and the format `SR-YYYY-000001`. Conversion creates one linked CM breakdown through `ICorrectiveMaintenanceService` in the same database transaction. The linked corrective work order becomes visible after the existing CM review and conversion step.

Migration `20261001141730_ServiceRequestsAndRequestManagement` was applied without resetting data. Its deployment script is `artifacts/sql/Task009_ServiceRequests.idempotent.sql`.
