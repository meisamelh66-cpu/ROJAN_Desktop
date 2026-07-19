# Phase 20 — Enterprise Reporting & Analytics Platform

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Build a centralized Enterprise Reporting & Analytics platform capable
of aggregating data from every existing business module, as the
reporting foundation for future AI and Executive Dashboard features.
Two new modules — Reporting and Analytics. Explicitly read-only:
no existing module's Domain, Application business logic, Navigation
mechanics, Localization Platform architecture, or Fluent 2 Design
System changed. Reuse all existing infrastructure — the platform
reads every number it shows through the eight source modules'
already-published Application-layer query services, never their
repositories or Domain types directly.

## Deliverables

- [x] **Domain** (`Rojan.Desktop.Domain/Reporting`): eleven
      entities/records (`ReportDefinition`, `ReportColumn`,
      `ReportFilter`, `ReportRow`, `ReportResult`, `ReportSnapshot`,
      `KpiDefinition`, `KpiValue`, `ChartDefinition`, `ChartSeries`,
      `AnalyticsSummary`) and six enums (`ReportType`,
      `ReportCategory`, `FilterType`, `ReportColumnDataType`,
      `KpiType`, `ChartType`, `ExportFormat`, plus a Reporting-owned
      `TrendDirection` duplicate — Domain modules don't reference each
      other, same isolation every other slice follows). Genuine Domain
      logic: `TrendCalculator` (trend direction + percentage change
      from a current/previous value pair) and `DateRangeRules`
      (range validation, previous-period-of-equal-length derivation).
      `IReportingRepository` is deliberately narrow — unlike every
      other module's repository, it does **not** own the business data
      its reports summarize; it owns only the static report catalog
      and report-run history (`ReportSnapshot`), the only state
      genuinely local to Reporting.
- [x] **Infrastructure** (`Rojan.Desktop.Infrastructure/Reporting`):
      `FakeReportingRepository` seeds the fifteen system-defined
      report catalog entries (with real columns and supported
      filters) plus four sample `ReportSnapshot`s (two pre-saved, two
      recent) so "Saved Reports"/"Recent Reports" have real content
      on first launch.
- [x] **Application** (`Rojan.Desktop.Application/Reporting`): DTOs
      mirroring every Domain type (Application's own enum copies,
      same convention as `Dashboard.TrendDirection`), `ReportingMapper`,
      and the platform's real logic:
      - `IReportExecutionQueryService`/`ReportExecutionQueryService` —
        one aggregation branch per `ReportType`, each composing only
        the sibling query services it actually needs
        (`ICustomerQueryService`, `IBookingQueryService`,
        `IServiceQueryService`, `IProductQueryService`/
        `IInventoryQueryService`, `IInvoiceQueryService`,
        `IEmployeeQueryService`/`IAttendanceQueryService`/
        `ICommissionQueryService`/`IPayrollQueryService`). `MoneyParser`
        is Reporting's own copy of the string-money parsing four
        read-only modules' display-string price fields need before
        summing (`Accounting.AccountingMapper.ParseMoney` is internal
        to that assembly).
      - `IKpiEngineQueryService`/`KpiEngineQueryService` — eight
        `KpiType`s (Revenue, Appointments, Customers, Inventory,
        Payroll, Attendance, Growth, Trend), each compared against its
        immediately preceding period of equal length via
        `Domain.Reporting.TrendCalculator`. Revenue/Appointments/
        Payroll/Attendance/Growth get genuine current-vs-previous
        trend math; Customers/Inventory report a Flat trend, honestly,
        because neither source module keeps a historical snapshot
        this phase could compare against.
      - `IAnalyticsQueryService`/`AnalyticsQueryService` — the
        Analytics Dashboard's summary (reusing the same
        `AnalyticsAggregator` core the KPI Engine and the three
        Dashboard reports share, so all three surfaces compute
        identical numbers for identical periods) and three
        data-derived charts.
      - `IReportSnapshotQueryService`/`IReportSnapshotCommandService` —
        Saved/Recent Reports history (record a run, pin/unpin, delete).
      - `IReportExportService`/`ReportExportService` — Csv genuinely
        writes a file (proved by an automated test asserting real file
        contents); Pdf/Excel/Print return an honest
        `ExportResultDto(Success: false, Message: "... not yet
        implemented ...")` rather than a silent no-op or an unhandled
        exception, per this phase's "architecture only, stub
        implementations acceptable" instruction for exports.
      - `ReportFilterSet` — the Filter Engine's parsing half: turns
        the generic `ReportFilterDto` list every report receives into
        typed values (`DateRange` → start/end `DateTimeOffset`, every
        other `FilterType` → a raw id/status/category string).
- [x] **Presentation**: two new sidebar modules.
      - **Reporting** (`ReportingModule`, replaces the `"reports"`
        placeholder one-for-one, same swap `SettingsModule` made in
        Phase 19A): Catalog (browse/pick a report), Report Viewer
        (Filter Panel — a generic `FilterType`+value row per
        additional filter the selected report supports, plus a
        dedicated date-range picker; Run — cancellable via a
        `CancellationTokenSource`, never blocks the UI thread; a
        results grid whose columns are rebuilt per-report in
        code-behind since each report's column set is genuinely
        different; Export, opening `ExportDialogViewModel` through the
        existing dialog framework, the same way
        `Accounting.PosCheckoutViewModel` is shown), Saved Reports,
        Recent Reports (re-run, pin/unpin, delete).
      - **Analytics** (`AnalyticsModule`, a genuinely new entry, same
        as Calendar/HR before it — no placeholder existed to swap):
        a Daily/Weekly/Monthly period switcher, eight KPI Cards, and a
        Chart Area rendering three charts via a new `SimpleBarChart`
        control — every chart type (Line/Bar/Pie/Area/Column) renders
        as native, proportional horizontal bars; **no external
        charting library anywhere in this app**, per this phase's
        explicit instruction.
      - New `Strings.Nav_Analytics` resource key across all three
        languages (fa-IR/en-US/ar-SA) for the new sidebar entry; the
        Reporting sidebar entry reuses the existing `Nav_Reports` key.
        Report/KPI/chart *content* (report names, column headers,
        KPI labels) stays English literals, the same explicit,
        documented scope boundary Phase 19A drew around Dashboard's
        DTO-supplied KPI card labels — deep content localization is
        out of scope for this phase.

## Migration Notes / Scope Boundaries

- **Read-only by design.** Nothing in this phase writes to any
  existing module's data. The only thing Reporting itself persists is
  its own report-run history (`ReportSnapshot`).
- **Export is architecture-plus-one-real-format.** Csv is genuinely
  implemented (writes to `%TEMP%\RojanDesktopExports\`); Pdf/Excel/Print
  are honest stubs. A future phase adding real Pdf/Excel generation
  only needs to implement `IReportExportService.ExportAsync`'s two
  remaining branches — no architecture changes.
- **Charts are a proof of the data model, not a design statement.**
  Every `ChartType` renders identically today (native proportional
  bars). A future phase could give Pie/Line/Area their own bespoke
  visual without touching `ChartDefinitionDto`'s shape.
- **KPI trend honesty.** Customers and Inventory KPIs report a Flat
  trend because their source modules (`Customers`, `Inventory`) keep
  no historical snapshot to compare against — documented in
  `KpiEngineQueryService`'s own doc comment rather than fabricating a
  number.
- **Report/KPI content is not localized.** Report names, column
  headers, and KPI labels are English literals; only the two sidebar
  entries and shared chrome route through `Strings.*`. A future
  localization pass would need new `Strings.Report_*`/`Strings.Kpi_*`
  keys, following the same pattern Phase 19A established.

## Risks

- **`AnalyticsAggregator` still issues 8+ concurrent calls per period,
  x2-3 periods per page load.** Parallelized via `Task.WhenAll` (see
  Validation below), so first paint is now sub-second against the
  fake in-memory repositories, but a future real backend with actual
  network latency should watch this page's load time.
- **N+1 attendance lookups remain N+1, just parallel.** `IAttendanceQueryService`
  has no "get all attendance" method, so both `AnalyticsAggregator`
  and the Attendance Summary report fetch one employee at a time
  (concurrently, not sequentially, after this phase's fix) rather than
  in a single batched call. Acceptable against ~20 seeded employees;
  worth a `GetAllAttendanceAsync` addition to `IAttendanceQueryService`
  if the seeded roster grows substantially.
- **Report catalog is fixed at fifteen system-defined reports.** No
  custom report builder exists — adding a sixteenth report means
  adding a new `ReportType` enum member, a `FakeReportingRepository`
  catalog entry, and a `ReportExecutionQueryService` branch, not a
  configuration change.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` — 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` — 735/735 tests passing (116 new):
      `Domain.Tests` (+13: `TrendCalculator`'s Up/Down/Flat/rounding
      behavior, `DateRangeRules`' validation and previous-period
      derivation), `Application.Tests` (+62: `ReportExecutionQueryServiceTests`
      exercises real aggregation logic for revenue/sales/appointments/
      customer-retention/service-popularity/specialist-performance/
      inventory-valuation/low-stock/payroll-summary/commission-summary/
      attendance-summary/daily-dashboard against hand-built seed data
      via stubbed sibling query services — including filter behavior
      (date range, status, specialist) — plus `KpiEngineQueryServiceTests`
      (genuine trend math verified against real invoice/payroll data),
      `AnalyticsQueryServiceTests` (chart shape and content),
      `ReportFilterSetTests`, `MoneyParserTests`, `ReportingMapperTests`,
      `ReportCatalogQueryServiceTests`, `ReportSnapshotServicesTests`,
      and `ReportExportServiceTests` — the Csv test asserts a real file
      was written with correct header/row content, not just a
      `Success: true` flag), `Infrastructure.Tests` (+14:
      `FakeReportingRepositoryTests` — all fifteen seeded reports
      present with correct `ReportType`s, snapshot CRUD, saved-state
      toggling, unknown-id handling), `Presentation.Tests` (+27:
      `ReportingPageViewModelTests` — catalog load, report selection,
      run/cancel wiring, filter add/remove, export dialog opening,
      snapshot pin/unpin/delete/re-run, `Dispose` — and
      `AnalyticsPageViewModelTests` — load, period switching).
      `ArchitectureTests` (4, unchanged) confirm
      `Domain.Reporting`/`Application.Reporting`/the new Presentation
      surfaces respect the same dependency-direction rules as every
      other slice.
- [x] Runtime verified end-to-end via UI Automation:
      - Navigated to Reporting → Catalog listed all fifteen reports;
        selected Revenue Report → Report Viewer showed it; Run
        Report produced 6 real rows (grouped by day) totaling
        **8 invoices / $1,372.68**, matching what the same seeded
        Accounting data shows elsewhere in the app.
      - Export Dialog opened correctly (Design-System-styled card,
        Format dropdown defaulting to Csv, Export/Close buttons);
        CSV export mechanism separately confirmed via automated test
        to write a real file with correct header/row content.
      - The just-run report appeared in Recent Reports; pinning it
        moved it into Saved Reports, which also correctly showed the
        two pre-seeded saved snapshots (Revenue Report, Specialist
        Performance) from `FakeReportingRepository`'s seed data.
      - Navigated to Analytics → Daily period showed 8 KPI cards and
        3 charts with real proportional bars (e.g. Revenue - Last 7
        Days: Jul 17 $518, Jul 18 $286, Jul 19 $294 — bar widths
        genuinely proportional to value). Switching to Weekly
        correctly recomputed every KPI (Revenue $294 → $1,167,
        Appointments 0 → 2, etc.) and all three charts against a full
        week of seeded data.
      - First-launch language default (Persian/RTL) and the sidebar's
        two new entries (گزارش‌ها/تحلیل‌ها) confirmed unaffected —
        Phase 19A's localization platform was not touched.
- [x] **Two real bugs found and fixed during this pass** (build and
      the full test suite were green throughout both — visible only at
      runtime):
      1. The Analytics Dashboard took 15-20+ seconds to first paint.
         `AnalyticsAggregator.AggregateAsync` awaited eight-plus
         independent repository calls sequentially, each carrying
         `FakeXxxRepository`'s own artificial per-call delay
         (200-400ms, deliberately added elsewhere in this app so
         Loading states are observable), compounded by an N+1
         per-employee attendance loop (~20 employees × 200ms =
         ~4 seconds) run up to three times per page load (KPI Engine's
         current+previous period, plus the Analytics summary's own
         redundant computation). Fixed by parallelizing every
         independent fetch with `Task.WhenAll` — the top-level
         `AggregateAsync` fetches, the attendance-per-employee loop,
         the KPI Engine's current/previous-period pair, and the
         Analytics page's three top-level calls (KPIs/summary/charts)
         — cutting first paint to well under a second. Applied the
         same fix to the Attendance Summary report's identical N+1
         pattern for consistency.
      2. `SimpleBarChart`'s bars never rendered — chart titles showed,
         but zero bars, despite `Rebuild()` correctly populating its
         `Bars` collection every single time (confirmed via runtime
         tracing: `Bars.Count` was 7/4/5 for the three charts, exactly
         as expected). Root cause: the `ItemsControl` bound to `Bars`
         via `{Binding Bars, ElementName=Root}` — reliable when
         `SimpleBarChart` is used standalone, but this control is
         itself hosted inside an *outer* `ItemsControl`'s own
         `DataTemplate` (the Chart Area's `ItemsControl` over
         `Charts`), and `Bars` is a plain CLR property, not a
         `DependencyProperty` — the `ElementName` binding to it proved
         unreliable in that doubly-templated hosting scenario. Fixed
         by assigning `ItemsSource` directly in code-behind instead of
         through the `ElementName` binding; `ItemsControl` still
         tracks all future `Add`/`Clear` calls via the
         `ObservableCollection`'s own `INotifyCollectionChanged`
         regardless of how `ItemsSource` was originally assigned, so
         this sidesteps the resolution issue entirely without losing
         any reactivity.
      Both fixes re-verified clean via a full fresh runtime pass (see
      above).
- [x] No changes to the Fluent 2 Design System — every new control
      reuses existing shared styles/tokens (`DashboardCard`,
      `DashboardWidget`, `Rojan.Style.Panel`, `Rojan.Style.ButtonPrimary`/
      `ButtonSecondary`) unchanged; `SimpleBarChart` is the only new
      visual primitive, and it composes existing brushes/corner-radius
      tokens rather than introducing new ones.
- [x] Clean Architecture boundaries unchanged — `Domain.Reporting` has
      no outward dependency, `Application.Reporting` depends only on
      Domain plus its eight sibling modules' own Application-layer
      interfaces (never their repositories/Domain types), Presentation
      depends only on Application. Verified by the unmodified,
      still-passing `ArchitectureTests`. No existing module's
      Domain/Application/Infrastructure code was modified — Reporting
      only ever reads through already-published query services.

## Approval

Approved by: <pending> — <date>
