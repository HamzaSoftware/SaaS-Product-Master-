# Nexa Design System

Nexa's interface is a calm, compact enterprise workspace. It favors clear hierarchy, restrained color, strong typography, and predictable interaction over decorative dashboards. The system supports tenant administration and platform operations in English and Arabic, in light and dark themes, without changing the API's security model.

## Foundations

The semantic tokens live in `apps/web/src/app/globals.css`. Components consume names such as `--surface`, `--surface-raised`, `--text`, `--text-muted`, `--border`, `--accent`, `--danger`, and `--focus`; they do not depend on theme-specific color values. `:root` defines the light palette and `html[data-theme="dark"]` supplies the dark palette.

- Typography: the English stack prefers Inter and modern system UI fonts. Arabic prefers IBM Plex Sans Arabic, Tajawal, Segoe UI, and system fallbacks. No remote or bundled font is required for local development.
- Spacing: use the 4, 8, 12, 16, 20, 24, 32, 40, and 48 pixel scale represented by `--space-*` tokens.
- Shape: controls use 6 or 8 pixel radii; cards and major surfaces use 12 or 16 pixel radii.
- Depth: borders separate most surfaces. Use the tokenized small and medium shadows only for raised controls, menus, dialogs, and key panels.
- Motion: interactive transitions use the fast and standard duration tokens. Reduced-motion preferences disable nonessential animation.
- Focus: interactive elements share a visible two-part focus ring based on `--focus`.

The theme preference can be light, dark, or system. `ThemeProvider` resolves the OS preference, reacts to OS changes, and stores only the non-sensitive preference under `nexa-theme`. A small pre-paint script applies the resolved theme before hydration to avoid a flash. Authentication tokens remain memory-only and refresh through the HttpOnly cookie.

## Components

Reusable controls live under `apps/web/src/components/ui`. Extend them before creating page-specific variants.

- `Button`: primary, secondary, ghost, and danger variants; regular, small, and icon sizes; disabled and loading states.
- `Input`, `PasswordInput`, `Select`, and `Textarea`: consistent labels, help/error text, focus treatment, and password visibility control.
- `Checkbox`, `MultiSelect`, and `Switch`: accessible native controls for single, grouped, and binary settings.
- `Card`, `Badge`, and `StatusIndicator`: semantic content grouping and lifecycle status.
- `SearchField`, `FilterBar`, `Tabs`, and `Pagination`: collection navigation patterns.
- `FormSection`: grouped forms with a heading and supporting description.
- `EmptyState`, `LoadingState`, `Skeleton`, and `ErrorState`: standard result states with localized labels supplied by callers.
- Overlay, confirmation-dialog, toast, and table utilities provide focus-managed dialogs, feedback, and dense administration views.

Use real application data. Empty states should explain the next available action; dashboards must not display invented metrics or placeholder modules.

Corrective-maintenance screens reuse the standard cards, filters, status indicators, forms, tables, confirmation patterns, and activity timelines. Dense work-order detail actions wrap responsively, and all labels and flow directions are localized for English/LTR and Arabic/RTL.

## Layouts and navigation

The tenant shell and Platform Admin shell share the same surface, control, typography, and theme language. The desktop shell uses a 248 pixel sidebar, 68 pixel top bar, and a content area capped at 1440 pixels. Navigation lists only implemented destinations. Platform and tenant sessions remain visibly and technically separate.

Authentication uses a responsive split layout: product context on one side and a focused credential panel on the other. Tenant login asks for the current API's tenant UUID as a workspace identifier. Replacing that field requires a backend-supported discovery, invitation, or deep-link contract that is rate-limited and does not reveal tenant membership.

At widths below 1100 pixels, dense grids reduce columns. Below 820 pixels, the sidebar becomes an off-canvas panel and split authentication becomes a single column. Below 580 pixels, actions and form rows stack. Tables stay in a contained horizontal scroller instead of widening the page.

## Localization, direction, and accessibility

Every application URL begins with `/en` or `/ar`. The localized subtree sets `dir="ltr"` or `dir="rtl"`; layout CSS uses logical properties so alignment and spacing mirror correctly. Visible copy belongs in the relevant dictionary, including labels, validation, status, loading, and assistive text.

Controls must keep programmatic labels, keyboard operation, visible focus, adequate target sizes, and semantic disabled/loading states. Use headings in order, status/live regions for asynchronous feedback, and `aria-current` or the active class for current navigation. Color is never the only status signal.

## Adding future modules

1. Add navigation only when the destination and its core workflow are usable.
2. Reuse semantic tokens and shared primitives; do not hard-code theme colors in module components.
3. Add English and Arabic strings together and test both directions at mobile and desktop widths.
4. Include real empty, loading, error, permission-denied, and success states.
5. Keep authorization, tenant isolation, and session validity in the API. UI guards improve navigation but never replace server checks.
6. Add focused tests for state or validation logic, then run typecheck, lint, unit tests, and the production build.

Asset screens establish the operational-module pattern: metric cards from real API aggregates, filterable/paginated tables, multi-section forms, definition-list details, semantic status indicators, explicit lifecycle confirmation, and chronological activity. Future modules should reuse this structure without fabricated dashboard values.

Preventive-maintenance screens extend this pattern with real KPI cards, plan-state badges, human-readable recurrence summaries, an agenda calendar, asset search, and a keyboard-operable checklist builder with move controls. Checklist execution presents Pass, Fail, and Not Applicable as labeled controls, keeps failed-item notes visible through verification, and stacks rows on narrow screens. English/LTR and Arabic/RTL routes share the same logical-property layouts, light/dark tokens, loading, empty, validation, and feedback states.

Inventory screens reuse metric cards, filter bars, horizontally contained tables, status indicators, responsive forms, and confirmation prompts. Financial stock forms use searchable/selectable business labels instead of raw UUID entry, display SAR through `Intl.NumberFormat`, and clearly separate on-hand, reserved, and available quantities. The horizontal Inventory subnavigation remains scrollable on small screens; Parts & Materials actions wrap into touch-friendly rows.

## Procurement screens

Procurement reuses the application shell, metric cards, compact subnavigation, status indicators, contained tables, detail lists, confirmation prompts, and responsive form grid. Multi-line PR, PO conversion, and receiving rows keep business labels visible, constrain quantities for usability, and stack below tablet width. Server validation remains authoritative. English/LTR and Arabic/RTL share logical-property CSS and localized statuses.
# Service request UI extension

Task #009 reuses the established compact operational patterns: the primary request dashboard uses metric cards and a table, My Requests keeps requester actions simple, and triage uses a dense filter bar and table for high-volume work. Detail pages use the shared two-column detail grid, status indicators, inline workflow forms, and timeline. English and Arabic share the same semantic components and mirror through the existing logical CSS and document direction; no module-specific physical left/right layout rules were added.
