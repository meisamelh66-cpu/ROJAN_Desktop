# Phase 33 — Enterprise Reporting & Business Intelligence Platform

**Status:** Complete

## Objective

Extend the Phase 20 Reporting/Analytics foundation into a full Business
Intelligence platform — a wider system-report catalog (Financial/
Customer/Appointment/Inventory/Employee), an expanded KPI Dashboard, real
Pie/Donut/Line/Area chart rendering (previously bar-only), a global
catalog search, and a Report Scheduler bridge into the Phase 32
Automation Engine — without duplicating or modifying the existing Report
Center's own architecture (`ReportingModule`/`ReportingPageViewModel`/
`ReportingPage.xaml`), per this phase's own "avoid duplicate
implementations" instruction.

## Why This Extends The Existing Reporting Module, Not A New One

Requirement 33.1 asked for a "Report Center" sidebar hub. `Nav_Reports`
was already the Persian string "گزارش‌ها" and `ReportingModule` was
already an unrestricted-by-default sidebar entry backed by a real
catalog/run/export/snapshot stack (built in Phase 20). Rather than stand
up a second, competing module, this phase extended that existing vertical
slice in place — new `ReportType`/`KpiType`/`ChartType`/`FilterType`
members, new report-definition seed rows, new `ReportExecutionQueryService`
handlers, a catalog search box, and a permission gate — leaving every
Phase 20 file's structure and every pre-existing report/KPI untouched.

## New Report Types (Requirements 33.5–33.9)

An Explore-pass audit of the pre-existing 15 system reports found real
gaps in Financial/Customer/Appointment/Inventory/Employee coverage; 13 new
`ReportType` members were added (mirrored in `Domain.Reporting.ReportType`
and `Application.Reporting.ReportType`, plus both directions of
`ReportingMapper`) to close exactly those gaps, not to duplicate anything
already covered:

| Category | New Reports |
|---|---|
| Financial (33.5) | Cash Flow, Outstanding Payments, Tax Summary |
| Customer (33.6) | VIP Customers, Inactive Customers, Customer Lifetime Value |
| Appointment (33.7) | Appointment Status Breakdown, Peak Hours |
| Inventory (33.8) | Inventory Movements, Supplier Purchases |
| Employee (33.9) | Employee Working Hours, Branch Performance |
| Dashboards (33.3) | AI Usage Summary |

Each ships as a seeded `ReportDefinition` in `FakeReportingRepository`
(Persian name/description/columns/filters) and a dedicated
`RunXxxAsync` handler in `ReportExecutionQueryService`. Three needed a
capability that didn't exist yet: `IInventoryRepository`/
`IInventoryQueryService.GetAllTransactionsAsync()` (only a per-product
`GetTransactionsForProductAsync` existed before, insufficient for
Inventory Movements/Supplier Purchases which need every transaction
across the whole catalog). `ReportExecutionQueryService`'s constructor
grew from 11 to 14 dependencies (`IShiftQueryService` for Employee
Working Hours, `IOrganizationQueryService` for Branch Performance,
`ITokenUsageTracker` for AI Usage Summary) — all three were already
DI-registered elsewhere, so no new DI wiring was required beyond the
constructor signature change itself.

**Profit/Expenses KPI approximation**: no dedicated Expense-tracking
module exists anywhere in this app, so `Expenses := PayrollTotal` and
`Profit := Revenue - PayrollTotal` — an honest approximation, documented
the same way the pre-existing Customers/Inventory KPIs already document
their own "flat trend, no history" limitation.

## KPI Dashboard (Requirement 33.4)

`KpiEngineQueryService.GetKpisAsync` grew from 8 to 15 `KpiType` members.
Original 8: Revenue, Appointments, Customers, Inventory, Payroll,
Attendance, Growth, Trend. New 7: Profit, Expenses, Cancellation Rate,
Average Ticket, Average Service Time, Retention, Employee Productivity —
each a real aggregation over `bookings`/`employees`/`shifts`/payroll data
fetched via `Task.WhenAll`, not a placeholder.

## Chart Engine (Requirement 33.10)

`SimpleBarChart` was bar-only before this phase (Phase 20's original
scope). It now branches on `ChartType`:

- **Bar / Column / HeatMap** — the pre-existing proportional horizontal
  bars (HeatMap has no dedicated renderer yet; falls through to the bar
  default, architecture-ready only, consistent with its own "documented
  boundary" status).
- **Pie / Donut** *(new)* — real wedges drawn in code-behind via
  `Path` + `ArcSegment`, combined into a ring (`CombinedGeometry`) for the
  donut hole, onto a dedicated `Canvas`, with a legend `ItemsControl`
  reusing the existing `ChartBarItem` (given a new `SliceBrush` property).
- **Line / Area / Trend** *(new)* — a real `Polyline` (plus a filled
  `Polygon` for Area) built from normalized point coordinates on a second
  `Canvas`.

Still native WPF only, per this app's original "no charting library"
constraint — no new NuGet dependency was introduced.

## Global Search (Requirements 33.2 / 33.13)

`ReportingPageViewModel` gained `CatalogSearchText` + a private
full-catalog cache (`_allReportDefinitions`) and `ApplyCatalogFilter()` —
a client-side name/description substring filter, chosen over a
server-side search method because the whole catalog (now 28 reports) is
small and already fully in memory, unlike Customers/Bookings' larger,
server-side-searched datasets. Recent/Saved/Pinned reports were already
real (`IReportSnapshotQueryService.GetRecentSnapshotsAsync`/
`GetSavedSnapshotsAsync`, `ToggleSavedAsync`) — built in Phase 20, reused
unchanged here.

## Report Scheduler ⇄ Automation Engine Bridge (Requirement 33.12)

Rather than building a second, separate scheduling subsystem, this phase
added exactly one new integration point into the existing Phase 32
Automation Engine: `WorkflowStepType.RunReport` (Domain + Application
mirror + both `AutomationMapping` directions) and
`RunReportStepExecutor : IWorkflowStepExecutor`
(`Application.Automation.WorkflowStepExecutors.cs`). Its `ExecuteAsync`
reads `Config["reportDefinitionId"]` (fails the step if missing or
unknown), runs the report unfiltered via
`IReportExecutionQueryService.RunReportAsync(id, [], ct)`, always exports
a CSV copy via `IReportExportService.ExportAsync(..., ExportFormat.Csv, ct)`
so a scheduled run leaves a real artifact behind, and — if
`Config["recipientEmail"]` is set — emails the result location through
the existing outbox-only `IEmailNotificationService`, the same
"architecture ready for delivery, no real SMTP" boundary Phase 32's own
Email step already established.

A user schedules a report today exactly the way Phase 32 already lets
them schedule anything: create a `Workflow` with a `RunReport` step,
publish it, then create a `ScheduledJob` pointing at it from the existing
Automation page's Workflows/Scheduled Jobs tabs. No second "Scheduled
Reports" UI section was added inside the Reporting page itself — building
one would have duplicated the Scheduled Jobs tab that already exists for
this exact purpose, directly against this phase's own "avoid duplicate
implementations" instruction. (Reporting_Schedule_* resx keys drafted
early in this session for such a section were removed unused rather than
shipped as dead localization entries.)

## Permission Model

`ReportingModule.Metadata.RequiredPermission` was `null` (unrestricted at
nav level) despite `Permission.ReportingView` already existing — now set
to `Permission.ReportingView`, closing that gap. `WorkspaceRole.Hr`'s
permission set in `RolePermissions.cs` was missing
`Permission.ReportingView` entirely; added, since HR needs Employee
Reports (33.9) visibility per 33.14's role list.

## Localization (Requirement 33.15)

New Strings.cs/resx entries across fa-IR/en/ar: `Reporting_RunCancelled`,
`Reporting_CatalogSearchPlaceholder`, `Enum_RunReport`. No hardcoded text
introduced in Presentation/Application/Domain/Infrastructure. (As with
every prior phase, raw `enum.ToString()` values stored into
`ReportRowDto.Values` dictionaries remain an accepted, pre-existing
unlocalized boundary — Application cannot reference Presentation's
`EnumLabelConverter`.)

## Export (Requirement 33.11)

CSV export is real (unchanged from Phase 20). PDF/Excel/Print remain
documented stubs (`ReportExportService.NotYetImplemented`) — this phase
did not introduce a new export format, consistent with Phase 20's own
original "architecture and contracts only" scope for those three formats
and this app's standing "no new third-party dependency" boundary.

## Report Builder (Requirement 33.2)

Custom/saved/favorite reports, templates, filtering, and search are real
(favorite = snapshot "Saved" toggle; templates = the system report
catalog itself, all pre-existing from Phase 20). Grouping, sorting, and
row-level drill-down are not implemented this phase — a documented
boundary, consistent with this app's established "flagship subset now"
scope-trimming pattern (Phase 24/26/29/32 each did the same for their own
lower-priority corners).

## Clean Architecture

No business logic in Views; the 13 new report handlers live in
`Application.Reporting.ReportExecutionQueryService`; `RunReportStepExecutor`
lives in `Application.Automation`, referencing `Application.Reporting`
(both Application-layer, no boundary crossed); no Infrastructure
reference inside Domain or Application; Presentation never references
Domain directly — enforced by the still-green
`ArchitectureTests.DependencyDirectionTests`/`ViewModelTestabilityTests`.

## Dependency Injection

`Application.DependencyInjection.AddApplication()` gained one new
registration: `services.AddSingleton<IWorkflowStepExecutor,
RunReportStepExecutor>();`. Every other new dependency
(`IInventoryQueryService.GetAllTransactionsAsync`,
`ReportExecutionQueryService`'s three new constructor parameters) resolves
through DI reflection against services already registered by prior
phases — no new DI wiring beyond the one line above was required.

## Testing

33 new tests added this phase (1455 → 1488 total, all passing):

- **Application.Tests**: 13 new `ReportExecutionQueryServiceTests` (one
  per new report type, against real fixtures for Transactions/Shifts/
  ShiftAssignments/Organizations/Branches/TokenUsage), 2 new
  `KpiEngineQueryServiceTests` (Cancellation Rate and Profit
  computations; the "returns exactly N KPIs" test updated from 8 to all
  15 KPI types), and 5 new `RunReportStepExecutorTests` (missing config
  fails the step, an unknown report id fails the step, a valid run always
  exports CSV, an email is sent only when `recipientEmail` is configured
  and its subject carries the localized report name).
- **Infrastructure.Tests**: `FakeReportingRepositoryTests` updated from 15
  to 28 seeded reports, with 13 new `[InlineData]` rows asserting each new
  report id resolves to its expected `ReportType`.
- **Presentation.Tests**: `ReportingPageViewModelTests`'s stale
  pre-localization-sprint assertion (checking for the literal English word
  "Generated") corrected to match the real resource-driven status message
  ("N rows", resolved via `Strings.Reporting_Rows`).

Full solution suite (1488 tests across Domain.Tests, Application.Tests,
Infrastructure.Tests, Presentation.Tests, Shell.Tests, ArchitectureTests)
passes on both Debug and Release configurations, zero warnings, zero
errors.

## Runtime Verification

Both Debug and Release builds of the full solution succeed with zero
warnings and zero errors. The compiled Release `Rojan.Desktop.Shell.exe`
was launched directly and observed for several seconds: the process
started and stayed running (every `OnStartup` step completed without
throwing, including the new `ReportingModule` permission gate and the new
`RunReportStepExecutor` DI registration), the Windows Application event
log recorded zero error/crash entries for the process during that window,
and it was then closed cleanly via its process handle.
