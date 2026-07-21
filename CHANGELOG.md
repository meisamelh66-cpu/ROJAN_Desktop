# Changelog

All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/) —
see `docs/standards/versioning.md`.

## [Unreleased]

### Added
- Repository initialized as a standalone git repo, independent from
  `ROJAN_DesignLab`.
- Phase 01 foundation: solution shell (`RojanDesktop.sln`), folder
  structure, `.editorconfig`, `Directory.Build.props`,
  `Directory.Packages.props`.
- Coding standards, branch strategy, versioning, and documentation
  standards (`docs/standards/`).
- Phase 09: Enterprise Customer CRM — first real business module built on
  the Phase 07 module-shell architecture. Read-only customer list with
  search and master-detail view, backed by a fake in-memory repository
  (Domain/Application/Infrastructure/Presentation vertical slice,
  following the same pattern as the Phase 06B Dashboard module). Referred
  to as Phase 10 going forward — see
  `docs/adr/0001-phase-09-naming-collision.md`.
- Phase 08: Testing Strategy — 36 tests across `Domain.Tests`,
  `Application.Tests`, `Infrastructure.Tests`, `Presentation.Tests`, and
  `ArchitectureTests`, covering the Dashboard and Customer CRM vertical
  slices. Architecture tests enforce dependency direction and ViewModel
  testability as executable checks. No new NuGet packages — built on the
  `xunit` setup already pinned in `Directory.Packages.props`.
- Phase 09: Release Engineering — `.github/workflows/ci.yml` (build+test
  gate on every PR/branch push) and `.github/workflows/release.yml`
  (tag-triggered packaging, gated on the tag matching
  `Directory.Build.props`'s version exactly). `build/get-version.ps1` and
  `build/publish.ps1` (self-contained, single-file, `win-x64`, ZIP-only
  packaging — no installer, a deliberate scope decision per
  `docs/phases/phase-09-release-engineering.md`). No new NuGet packages,
  no third-party GitHub Actions.
- Phase 10: Enterprise Customer CRM Expansion — Customer 360 profile
  (statistics cards, tags, notes, activity timeline, editable status,
  new-customer form) on top of the existing Customer CRM vertical slice.
  New Domain entities (`CustomerTag`, `CustomerNote`, `CustomerActivity`),
  expanded `CustomerStatus` (`Lead`/`Prospect`/`Active`/`Vip`/`Inactive`/
  `Churned`), the codebase's first command service
  (`ICustomerCommandService`) alongside a new profile query
  (`ICustomerProfileQueryService`) and search query
  (`ICustomerQueryService.SearchCustomersAsync`). `FakeCustomerRepository`
  is now mutable in-memory state (still no database, no API). No Shell/
  navigation changes. 82/82 tests passing (46 new).
- Phase 11: Enterprise Booking Module Foundation — third real business
  module (`Rojan.Desktop.*.Bookings`), same vertical-slice pattern as
  Dashboard and Customer CRM: `Booking`/`BookingStatus` Domain entities,
  `IBookingQueryService`/`IBookingCommandService` Application layer,
  mutable in-memory `FakeBookingRepository` (8 seed bookings), and a
  list/detail/create/status-transition Presentation UI. Wired into Shell
  navigation — the `"appointments"` placeholder sidebar entry is now the
  real `BookingModule`, renamed to "Bookings". No database, no API, no
  external integrations. 121/121 tests passing (39 new).
- Phase 12: Enterprise Specialist Management Foundation — fourth real
  business module (`Rojan.Desktop.*.Specialists`), same vertical-slice
  pattern as Dashboard, Customer CRM, and Bookings:
  `Specialist`/`SpecialistStatus`/`SpecialistSkill` Domain entities,
  `ISpecialistQueryService` (list + search)/`ISpecialistProfileQueryService`/
  `ISpecialistCommandService` Application layer, mutable in-memory
  `FakeSpecialistRepository` (5 seed specialists covering every status),
  and a directory/profile/search/status/skills Presentation UI. Wired
  into Shell navigation — the `"employees"` placeholder sidebar entry is
  now the real `SpecialistModule`, renamed to "Specialists". No database,
  no API, no external integrations, no Calendar, no Payroll. 165/165
  tests passing (44 new).
- Phase 13: Enterprise Service Catalog & Availability Foundation — fifth
  real business module (`Rojan.Desktop.*.Services`), same vertical-slice
  pattern as Dashboard, Customer CRM, Bookings, and Specialists:
  `Service`/`ServiceCategory`/`ServiceStatus`/`SpecialistService` (the
  specialist-to-service assignment mapping) Domain entities,
  `IServiceQueryService` (list + search)/`IServiceProfileQueryService`/
  `IServiceCommandService` (specialist assignment only - no catalog
  create/update commands were requested) Application layer, mutable
  in-memory `FakeServiceRepository` (9 seed services covering every
  category and status), and a catalog/search/category/duration/price/
  assigned-specialists Presentation UI. Wired into Shell navigation — the
  `"services"` placeholder sidebar entry is now the real `ServiceModule`
  (no rename needed, the placeholder was already named "Services"). No
  database, no API, no external integrations, no Calendar, no Payment.
  207/207 tests passing (42 new).
- Phase 14: Enterprise Calendar & Availability Engine Foundation — sixth
  real business module (`Rojan.Desktop.*.Calendar`), same vertical-slice
  pattern as every prior module: `WorkingSchedule`/`TimeSlot`/
  `AvailabilitySlot`/`AvailabilityStatus` Domain types,
  `ICalendarQueryService` (30-minute slot generation across a
  specialist's working hours plus conflict detection against existing
  booked ranges)/`ICalendarCommandService` (reserve/release a single
  slot, with a write-time conflict re-check) Application layer, mutable
  in-memory `FakeCalendarRepository` (weekly schedules for the three
  Active specialists, seeded conflicts computed relative to "today"),
  and a daily-availability grid Presentation UI - not a list-plus-detail
  split like every other module, a single day's slot grid *is* the page.
  Wired into Shell navigation as a genuinely new sidebar entry (no
  placeholder was named "Calendar") via the same one-line composition-
  root mechanism every other module uses. No database, no API, no
  external calendar sync, no full booking wizard. 232/232 tests passing
  (25 new).
- Phase 15: Enterprise Booking Workflow & Reservation Flow — the first
  Application-layer use case that coordinates multiple vertical slices
  at once: new `Rojan.Desktop.Application.BookingWorkflow` slice
  (`IBookingWorkflowService`/`BookingWorkflowService`) composing
  Customers, Services, Specialists, Calendar, and Bookings Application
  services to power a guided Booking Wizard (Customer → Service →
  Specialist → Date → TimeSlot → Review → Confirmation), shown as a
  dialog via a new `IDialogService` - the first producer of
  `MainWindowViewModel.ActiveDialog`, an extension point reserved since
  Phase 07. `Booking` expanded with `ServiceId`/`SpecialistId`/`Price`;
  `BookingStatus` expanded to a full six-value lifecycle
  (`Pending`/`Confirmed`/`InProgress`/`Completed`/`Cancelled`/`NoShow`)
  governed by a new `BookingRules` Domain validation class, now enforced
  by `BookingCommandService` on every write. The existing free-text
  quick-add form on the Bookings page is untouched, not replaced.
  Creating a booking through the wizard reserves the matching calendar
  slot first and rolls it back if the booking write fails, since there
  is no database transaction spanning both. No database, no API, no
  payments, no notifications, no Shell architecture change. 278/278
  tests passing (46 new).
- Phase 16: Microsoft Fluent 2 Design System Integration — a UI/UX-only
  sprint, no business functionality: replaced the prior "glass" visual
  language (translucent white fills, a purple→magenta gradient on nearly
  every surface, 32px pill-shaped cards) with Microsoft Fluent 2 -
  neutral navy-tinted surfaces plus one restrained ROJAN accent color,
  reserved for primary actions, selection, and focus. Rebuilt the entire
  `Themes/` token set (`Colors`/`Dark`/`Light`/`Typography`/`Spacing`/
  `Shapes`/`Shadows`/`Elevation`/`Icons`/`RojanTheme`, renamed from
  `Theme.xaml`); added implicit `TextBox`/`ComboBox`/`DatePicker`/
  `CheckBox` styles (previously completely unstyled - default Windows
  chrome against a dark theme, the single biggest visual inconsistency
  fixed); redesigned the sidebar's selection indicator and every
  sidebar/header/chrome icon onto one consistent glyph set (Segoe Fluent
  Icons, no new package dependency); redesigned the Booking Wizard
  dialog with a real Fluent step-progress indicator and Primary/
  Secondary button hierarchy. Zero Domain/Application/Infrastructure/
  workflow changes - Presentation (Themes/Controls/Views/a new
  Converters folder) and Shell (MainWindow.xaml, module icon-glyph
  string literals) only. 278/278 tests passing (unchanged from Phase 15
  - no ViewModel behavior changed).
- Phase 17: Enterprise Inventory & Product Management — seventh real
  business module (`Rojan.Desktop.*.Inventory`), same vertical-slice
  pattern as every prior module, replacing the "inventory" placeholder
  sidebar entry one-for-one: `Product`/`ProductCategory`/`Supplier`/
  `InventoryItem`/`StockTransaction`/`ServiceProductMapping` Domain
  entities (six aggregate types in one slice - the widest single
  repository interface in this app) plus a new `StockTransactionRules`
  Domain rule governing how each transaction type moves on-hand
  quantity; `IProductQueryService` (list/search/category/supplier
  options)/`IProductProfileQueryService`/`IInventoryQueryService`
  (low-stock monitoring)/`IInventoryCommandService` (product/category/
  supplier creation, validated stock transactions, service-to-product
  mapping) Application layer; `FakeInventoryRepository` (6 categories,
  4 suppliers, 10 products, 4 deliberately low-stock, service mappings
  cross-referencing the real Services module seed ids); a catalog/
  quick-add/profile/stock-transaction/service-mapping Presentation UI
  reusing every Phase 16 Fluent control and token unchanged - no Design
  System changes this phase. No database, no API, no external
  integrations. 362/362 tests passing (84 new).
- Phase 17A: Microsoft Fluent 2 Compliance Audit & Final UI Polish — a
  UI-only refinement pass, no business features, architecture frozen:
  renamed every design token to the strict semantic set a Fluent 2 audit
  expects (`Background`/`Border`/`BorderStrong`/`Disabled`/`Error`/
  `Success`/`Warning`, dropping the old `Navy`/`Stroke`/`*Text` naming;
  new dedicated `Card` and `Scrim` tokens); renamed the typography scale
  to `Display`/`Title`/`SectionHeader`/`Subtitle`/`Body`/`Caption`
  (removing an orphaned, never-consumed `PageTitle` step this audit
  caught); renamed every remaining `Glass*`-prefixed style key
  (`GlassCard`/`GlassPanel`/`GlassButton`/`GlassNavigationItem` →
  `Card`/`Panel`/`ButtonPrimary`/`NavigationItem`) across all 11
  consuming files, since a style still named "Glass" read as legacy even
  though its value had already been fully Fluent 2 since Phase 16;
  closed a real accessibility gap - `ComboBox`, `CheckBox`, and the
  window close button had no disabled-state visual treatment at all;
  reduced the dialog scrim from a theme-tinted 50% to a proper neutral
  black 40%. Zero Domain/Application/Infrastructure/business-rule
  changes - Presentation `Themes/` and one Shell markup file only.
  362/362 tests passing (unchanged - no ViewModel behavior changed).
- Phase 18: Enterprise POS & Accounting Foundation — eighth real business
  module (`Rojan.Desktop.*.Accounting`), same vertical-slice pattern as
  every prior module, replacing the "accounting" placeholder sidebar
  entry one-for-one: `Invoice`/`InvoiceItem`/`Payment`/`Receipt`/
  `CashSession` Domain entities (five aggregate types in one slice) with
  new `InvoiceCalculator`/`InvoicePaymentRules` Domain rules - the first
  module to use `decimal` for money instead of the display-only
  string-money convention every prior module used, since this is the
  first module doing genuine monetary arithmetic; `IInvoiceQueryService`/
  `IInvoiceCommandService`/`IPaymentQueryService`/`IPaymentCommandService`
  Application layer, composing over Customers/Bookings/Services/
  Inventory's own query services for the POS checkout's cart options and
  over Inventory's command service to decrement stock on product sale
  (the "Integrate with Booking, Customer, Inventory" requirement);
  `FakeAccountingRepository` (8 seed invoices spanning every status,
  cross-referencing real Booking/Customer/Service/Product seed ids, 6
  payments, 6 receipts, 2 cash sessions); Presentation: `AccountingPage`
  (Revenue KPI cards, searchable invoice list, read-only invoice detail
  panel) plus `PosCheckoutView` - a Cart → Payment → Receipt wizard-
  dialog fulfilling both the "POS checkout page" and "Payment dialog"
  deliverables in one dialog surface, reusing the existing
  `IDialogService`. No database, no API, no online payment. 449/449
  tests passing (87 new). Runtime-verified end-to-end: KPI cards,
  invoice list/detail, and a full POS sale (cart → payment → receipt)
  that correctly decremented Inventory stock and updated the Revenue
  KPIs.
- Phase 19: Enterprise Staff, HR & Commission Management — ninth real
  business module (`Rojan.Desktop.*.HR`), a genuinely new "Staff & HR"
  sidebar entry (no placeholder existed to swap, same as Calendar in
  Phase 15) - the widest vertical slice in this app: `Employee`/
  `EmployeeProfile`/`Shift`/`ShiftAssignment`/`Attendance`/`LeaveRequest`/
  `CommissionRule`/`CommissionTransaction`/`PayrollSummary` Domain
  entities (nine aggregate types in one repository interface) plus
  `EmployeeStatusRules`/`AttendanceRules`/`CommissionCalculator`/
  `PayrollCalculator` Domain rules; `IEmployeeQueryService`/
  `IEmployeeCommandService`/`IAttendanceQueryService`/
  `IAttendanceCommandService`/`IShiftQueryService`/`IShiftCommandService`/
  `ICommissionQueryService`/`ICommissionCommandService`/
  `IPayrollQueryService`/`IPayrollCommandService` Application layer (ten
  services). The headline integration:
  `CommissionCommandService.GenerateCommissionsFromAccountingAsync`
  scans Accounting's own `IInvoiceQueryService` for paid/partially-paid
  invoices, resolves each one's booking via Bookings'
  `IBookingQueryService` to find the specialist who performed the
  service, matches that specialist to an employee, and generates a
  `CommissionTransaction` via `Domain.HR.CommissionCalculator` - reading
  Accounting and Bookings only through their own published query
  services, never modifying either module's code, and safe to call
  repeatedly (already-processed invoices are skipped). `FakeHrRepository`
  (20 seed employees - 5 cross-referencing real Specialist seed ids,
  attendance/shift/leave history, 4 pre-seeded commission transactions
  deliberately leaving one real paid Accounting invoice unprocessed so
  the live generator has something real to do, 2 payroll summaries).
  Presentation: one `HrPage` with HR Dashboard KPI cards always visible
  (Employees/Present Today/Late Today/On Leave/Payroll This Month/
  Commission This Month/Average Attendance) plus six switchable sections
  (Employees/Attendance/Shifts/Leave/Commission/Payroll), each a
  master list plus a minimal quick-add form, and the selected employee's
  `EmployeeProfileViewModel` always visible on the right - same
  master-detail shape as every other module. No new Design System
  components - every control reuses Phase 16/17A's Fluent styles
  unchanged. No changes to Architecture, Navigation mechanics, or any
  existing module's business logic. 574/574 tests passing (125 new).
  Runtime-verified end-to-end: every Dashboard KPI matched seed data
  exactly (20 employees, 3 present, 2 late, 1 on leave, $42.12 commission
  this month, 77.8% average attendance), all six sections rendered
  correctly, and "Generate Commissions from Accounting" correctly
  produced one new commission live (Priya Nair, 12% of a real $518.40
  Accounting invoice = $62.21) from a real unprocessed invoice/booking
  pair. One real bug found and fixed during verification: a `DataTrigger`
  inside a `Button` `Style` illegally tried to set that same element's
  `Style` property to swap Primary/Secondary looks, which WPF disallows
  and threw at runtime - fixed by having the trigger set the individual
  Background/Foreground/BorderThickness properties instead of swapping
  styles.
- Phase 19A: Enterprise Globalization & Localization Platform — the
  complete localization architecture: `LocalizationService`,
  `CultureService`, `ResourceManager`-backed `Strings` (hand-written
  resx wrapper), `LanguagePackManager`, `IDateProvider`/
  `PersianCalendarProvider`/`GregorianCalendarProvider`,
  `ICurrencyFormatter` (Toman/Rial/USD/EUR, Latin/Persian/Arabic digit
  glyphs), and RTL/LTR infrastructure (`FlowDirection` set once from
  `ILocalizationService.CurrentLanguage`, cascading automatically through
  WPF's layout system — no per-View changes needed). Three built-in
  languages (Persian default/RTL, English, Arabic/RTL), each shipping as
  a real `Languages/*.pack` JSON manifest (not a hardcoded list) so pack
  discovery is provably automatic; a fourth, deliberately-partial
  `de-DE.pack` demo proves the "future packs load their own resources"
  mechanism (`Strings.SetPackOverrides`) actually works, with undefined
  keys honestly falling through to the compiled resx. Language changes
  persist to `%LocalAppData%\RojanDesktop\settings.json` and take effect
  on next launch (restart-required, not live, by design). Language Store
  Foundation (`ILanguagePackRepository`) ships as
  `LocalOnlyLanguagePackRepository` — an always-empty catalog whose
  install/remove throw `NotSupportedException` rather than silently
  no-op, per this phase's explicit "do not connect to servers yet" scope.
  Fully migrated: Application Shell, Navigation, Dashboard, the new
  Settings module (Language section: built-in languages, installed
  packs, Apply + restart flow, Available Languages foundation UI), the
  shared `DashboardWidget` style, and the dialog framework. Business
  modules (Customers, Bookings, Calendar, Specialists, Services,
  Inventory, Accounting, HR) are deliberately deferred to Phase 19B —
  see `docs/phases/phase-19A-globalization-platform.md`'s migration
  report for a per-module remaining-string estimate. No changes to
  Domain, Application business logic, Navigation mechanics, or the
  Fluent 2 Design System. 619/619 tests passing (45 new, including a new
  `Rojan.Desktop.Shell.Tests` project — the first with dedicated
  Shell-owned-class coverage). One real runtime bug found and fixed
  during verification: `App.OnStartup` was `async void`, so its first
  `await` returned control to WPF's `Application.Run()` before culture
  was set; WPF then started the Dispatcher's main message loop against
  an `ExecutionContext` baseline captured at that pre-culture-change
  moment, and every later dispatcher operation replayed from that stale
  baseline — `CurrentUICulture` silently reverting to the OS default
  mid-session despite having "already" been set. Fixed by making
  `OnStartup` fully synchronous (blocking via `GetAwaiter().GetResult()`)
  through the startup-critical section, so culture is durably set before
  the method returns control to the framework. A related bug (the
  Reports/AI Center placeholder nav titles frozen in the OS-default
  culture) was fixed by registering them as DI factories instead of
  eagerly-constructed instances, so `Strings.Nav_Reports`/`Nav_AiCenter`
  evaluate lazily at first resolve instead of during `ConfigureServices`
  (before culture is set). Runtime-verified end-to-end via UI Automation:
  first launch defaults to Persian + RTL (sidebar mirrors to the right,
  breadcrumb reads right-to-left); switching to English and restarting
  persists and applies English + LTR; switching to Arabic and restarting
  persists and applies Arabic + RTL; the navigation region's asymmetric
  `BorderThickness` (right-only in LTR) was confirmed to mirror
  automatically under RTL, same as WPF's Grid column mirroring.
- Phase 20: Enterprise Reporting & Analytics Platform — a new,
  read-only aggregation layer over every existing module, with two new
  sidebar entries: Reporting (replaces the "reports" placeholder) and
  Analytics (a genuinely new entry). Domain.Reporting adds eleven
  entities/DTO-shaped records (`ReportDefinition`, `ReportColumn`,
  `ReportFilter`, `ReportRow`, `ReportResult`, `ReportSnapshot`,
  `KpiDefinition`, `KpiValue`, `ChartDefinition`, `ChartSeries`,
  `AnalyticsSummary`), six enums, and genuine Domain logic
  (`TrendCalculator`, `DateRangeRules`) — `IReportingRepository` only
  owns the report catalog and report-run history
  (`ReportSnapshot`); every other number it shows is read live through
  eight sibling modules' own Application-layer query services
  (Customers, Bookings, Calendar-adjacent Specialists/Services,
  Inventory, Accounting, HR), never their repositories directly, same
  cross-slice composition pattern `Accounting.InvoiceQueryService` and
  `HR.CommissionCommandService` already established. Fifteen reports
  (Revenue, Sales, Appointments, Customer Growth, Customer Retention,
  Service Popularity, Specialist Performance, Inventory Valuation, Low
  Stock, Payroll Summary, Commission Summary, Attendance Summary,
  Daily/Weekly/Monthly Dashboard) run through one
  `ReportExecutionQueryService`, each a real aggregation branch over
  live data — not fixtures. A reusable KPI Engine
  (`IKpiEngineQueryService`, eight `KpiType`s) and Analytics Dashboard
  (`IAnalyticsQueryService`) share one `AnalyticsAggregator` core so
  both compute identical numbers for identical periods. Three
  Chart Area charts (Revenue by day, Appointments by status, Top
  Services) render through a new `SimpleBarChart` control — native
  proportional bars, no external charting library anywhere in this
  app, per this phase's explicit scope. A Filter Engine
  (`ReportFilterSet`, generic `FilterType`/value pairs covering all
  eight filter dimensions) and Export architecture
  (`IReportExportService` — Csv genuinely writes a file, Pdf/Excel/Print
  are honest `Success: false` stubs, never a silent no-op) round out
  the platform. Report generation is fully asynchronous and
  cancellable (`CancellationTokenSource` wired from the Report
  Viewer's Cancel button through to every repository call) and never
  blocks the UI thread. New Presentation surfaces: Reporting page
  (Catalog/Report Viewer/Saved Reports/Recent Reports sections, a
  generic Filter Panel, a dynamically-columned results grid, an Export
  Dialog shown through the existing dialog framework) and an Analytics
  Dashboard (Daily/Weekly/Monthly period switcher, eight KPI cards,
  three charts). No changes to Domain/Application business logic in
  any existing module, Navigation mechanics, the Localization
  Platform's architecture, or the Fluent 2 Design System — existing
  modules were read from, never modified. 735/735 tests passing (116
  new: Domain 13, Application 62 including a full aggregation-logic
  suite for `ReportExecutionQueryService`/`KpiEngineQueryService`/
  `AnalyticsQueryService`/`ReportExportService` against real seeded
  data via stubbed sibling query services, Infrastructure 14,
  Presentation 27 across the new Reporting/Analytics ViewModels).
  `ArchitectureTests` (4, unchanged) confirm the new module respects
  the same dependency-direction rules as every other slice. Two real
  bugs found and fixed during runtime verification: (1) the Analytics
  Dashboard's KPI Engine took 15-20+ seconds to first paint because
  `AnalyticsAggregator` awaited eight-plus independent repository
  calls sequentially, each carrying `FakeXxxRepository`'s own
  artificial network-latency delay, compounded by an N+1
  per-employee attendance loop and three redundant aggregation runs
  per page load — fixed by parallelizing every independent fetch with
  `Task.WhenAll` (including the attendance loop and the KPI Engine's
  current/previous-period pair), cutting first paint to under a
  second. (2) `SimpleBarChart`'s bars never rendered despite
  `Rebuild()` correctly populating its `Bars` collection every time
  (confirmed via runtime tracing) - the `ItemsControl` bound to
  `Bars` via `{Binding Bars, ElementName=Root}` proved unreliable once
  `SimpleBarChart` was itself hosted inside an outer `ItemsControl`'s
  own `DataTemplate` (the Chart Area) - fixed by assigning
  `ItemsSource` directly in code-behind instead of through the
  `ElementName` binding, which sidesteps the resolution issue
  entirely since `ItemsControl` still tracks the same
  `ObservableCollection`'s future `Add`/`Clear` calls regardless.
  Re-verified clean end-to-end via UI Automation after both fixes:
  Revenue Report ran and displayed 6 real rows totaling $1,372.68
  across 8 invoices; CSV export produced a real file (also covered by
  an automated test asserting file contents); a run was pinned via
  "Recent Reports" → Saved Reports and appeared alongside the two
  pre-seeded saved snapshots; the Analytics Dashboard's Weekly period
  switch correctly recomputed all eight KPIs and three charts against
  a full week of seeded data.
- Phase 21: ROJAN AI Center — a new, cross-cutting AI module composing
  every existing business module without creating tight coupling. New
  `Domain.AI` (nineteen records/enums/static-logic types, including
  `BusinessHealthCalculator` — weighted, clamped 0-100 composite score
  — and `ConversationRules` — pin cap, title derivation) plus
  `IAIRepository`, which (same "compute fresh, don't own the source
  data" reasoning `Reporting.IReportingRepository` established in
  Phase 20) owns only what is genuinely local to AI Center:
  conversations, prompt templates, provider/model selection, token
  usage history, and feature settings — Insights/Recommendations/
  Suggested Tasks/Smart Notifications/Business Health Score are
  computed fresh on every request from sibling modules' own
  Application-layer query services, never persisted.
  `Application.AI` adds seventeen services: a reusable Prompt System
  (`IPromptBuilder` composing System/Developer/User/Business Context/
  Analytics Context/Language Context/Session Context blocks via
  `IIntentClassifier`, `IContextProvider`, `IAnalyticsContextProvider`,
  and `IPromptTemplateRepository`), a Conversation System
  (`IConversationManager` — sessions, messages, pin/unpin capped at
  10, search, export, clear-unpinned-only; `IAIHistoryService` — a
  read-only composition over it for Recent/Pinned/Search views), a
  Provider abstraction (`IAIProvider` — `CompleteAsync` plus a genuine
  `IAsyncEnumerable<string>` `StreamCompleteAsync` — and the only
  concrete implementation, `MockAIProvider`: deterministic,
  keyword-derived replies, real word-by-word streaming, ~4-char/token
  estimation; **no API keys anywhere**, OpenAI/Anthropic/AzureOpenAI/
  LocalModel are abstraction-only per this phase's explicit
  instruction), five analytical engines (`IInsightEngine` — one
  insight per KPI plus a Commission insight, Trend/Risk/Opportunity/
  Info severity classified from trend direction and change magnitude;
  `IRecommendationEngine` — recommendations from Risk/Opportunity/
  Critical insights, Suggested Tasks from the High/Urgent-priority
  subset with priority-scaled due dates; `ISummaryEngine` — Daily/
  Executive Summary; `IBusinessHealthService` — a five-component
  weighted score over live Revenue/Appointments/Retention/Attendance/
  Inventory KPIs; `INotificationInsightService` — a thin Risk/
  Critical/Opportunity filter over `IInsightEngine`), and the
  composition root (`AIOrchestrator`/`IAIService`) wiring the Prompt
  System, the active `IAIProvider`, `IResponseFormatter`,
  `IConversationManager`, and `ITokenUsageTracker` into one
  async/cancellable `SendMessageAsync`/`StreamMessageAsync` pipeline.
  New Presentation surface: **AI Center** (`AiCenterModule`, replaces
  the `"ai-center"` placeholder one-for-one, same swap `ReportingModule`
  made in Phase 20) — Home (Business Health Score, Daily Summary,
  Smart Notifications), Chat (the Business Assistant, session-aware),
  Insights (Insight Dashboard across every category), Recommendations
  (Recommendations Panel + Action Center's Suggested Tasks), History
  (search, pin/unpin, export, clear, Conversation Viewer), and Settings
  (feature toggles, Model Selector, Usage Dashboard, Prompt Templates).
  876/876 tests passing (141 new): `Domain.Tests` (+2:
  `BusinessHealthCalculatorTests`, `ConversationRulesTests`),
  `Application.Tests` (+14 test classes covering `AIMapper`,
  `ConversationManager`, `InsightEngine`, `RecommendationEngine`,
  `NotificationInsightService`, `BusinessHealthService`,
  `SummaryEngine`, `MockAIProvider` — including genuine streaming and
  cancellation behavior — `IntentClassifier`, `ResponseFormatter`,
  `TokenUsageTracker`, `AIConfigurationService`, `AISettingsService`,
  and `AIOrchestrator` end-to-end against a real `ConversationManager`
  + `MockAIProvider` pipeline), `Infrastructure.Tests` (+1:
  `FakeAIRepositoryTests`), `Presentation.Tests` (+1:
  `AiCenterPageViewModelTests` — Load, Chat send, new/switch/pin/
  delete conversation, search, clear history, export, settings save,
  model configuration save, all driven through a real
  `ConversationManager`/`AIHistoryService`/`TokenUsageTracker` stack
  over a test-local in-memory repository). `ArchitectureTests` (4,
  unchanged) confirm `Domain.AI`/`Application.AI`/the new Presentation
  surface respect the same dependency-direction rules as every other
  slice. Runtime verified end-to-end via UI Automation: navigated to
  AI Center → Business Health Score rendered a real weighted score
  (79.9/100) with five live components; Daily Summary and Smart
  Notifications showed real narrative text and severity-classified
  alerts computed from live Reporting/HR data; Chat resumed the most
  recently active seeded conversation, sent a new message, and
  received a real Mock-provider reply; Insights/Recommendations/
  History/Settings all rendered live data (recommendations, suggested
  tasks, pinned/recent conversations, feature toggles, Model
  Selector). One real bug found and fixed during this pass: the Home
  section's `TextBlock` showing the Business Health Score number used
  a non-existent `Rojan.TextStyle.Heading` resource key — WPF resolves
  `StaticResource` lazily enough that this didn't fail the build, only
  at runtime on first navigation to AI Center (a cascade of
  `StaticResourceExtension` exceptions) — fixed by using
  `Rojan.TextStyle.Display`, the same style `Dashboard.KPIValue`
  already uses for headline numbers; re-verified clean afterward.
- Fluent 2 Premium Light Theme — a visual-refinement-only pass (no
  business logic, architecture, or navigation changes) that makes
  Light.xaml the shipped default theme and adds genuine, working
  Light/Dark/System theme switching. **Colors.xaml** now documents
  ROJAN's four brand accents by name (Purple — the one interactive
  accent, unchanged — Rose Gold, Lavender, Aqua), all explicitly
  emphasis-only per this pass's instruction; Error/Warning/Success
  moved out of the theme-invariant file into Light.xaml/Dark.xaml
  themselves, each theme now carrying its own AA-safe shade (the old
  invariant values only reached ~3.6:1 against a light background,
  failing WCAG's 4.5:1 for text). **Light.xaml** was rewritten as a
  genuinely premium, warm, layered surface ramp (Background <
  SurfaceSecondary < Card/Surface < SurfaceElevated, never pure
  white), with HintText/Disabled darkened to close a real contrast
  gap this pass found. **Typography.xaml** gained six new roles
  (WindowTitle, Disabled, Hyperlink, Error, Warning, Success), each
  setting its own Foreground. **Controls.xaml** gained
  `Rojan.Style.ButtonOutlined` and an implicit `RadioButton` style
  (the one missing form control) plus an opt-in `Rojan.Style.ToggleSwitch`;
  Shadows.xaml/Elevation.xaml's opacities were lowered (0.24-0.4 →
  0.10-0.22) to match Fluent 2's genuinely soft ambient shadows rather
  than the prior dark-theme-era heavier look. **Three real WCAG bugs
  found and fixed**: `SectionHeader`'s page title, `WidgetHeader`'s
  card title, and `KPIValue`'s headline number all used
  `Rojan.Brush.ButtonText` (white, meant for accent-filled buttons)
  directly on the Card background — invisible on Light.xaml, harmless
  only by coincidence on the old dark theme — fixed to `TextPrimary`,
  along with two more instances of the identical pattern in
  `DashboardPage`/`CustomerPage`'s activity/timeline rows. **A new
  Theming platform**, architecturally mirroring the Localization
  platform exactly: `Presentation.Theming.IThemeService`/`ThemeMode`
  (Light/Dark/System) and `Shell.Theming.ThemeService` (persists the
  choice to its own `theme.json`, resolves System via the Windows
  `AppsUseLightTheme` registry value, defaults to Light on first
  launch, restart-required — never live — same UX as the language
  switch). Making the theme choice actually take effect required a
  real architectural change to how the design system is assembled:
  every View previously self-merged its own copy of `RojanTheme.xaml`
  (a "self-sufficient, standalone-Blend-preview" pattern that
  hardcoded Dark.xaml three times over, in `RojanTheme.xaml`,
  `Controls.xaml`, and `ShellChrome.xaml`) — this is now built exactly
  once, in code, by `Shell.Theming.ThemeResources.Apply` inside
  `App.xaml.cs`'s `OnStartup`, before any Window exists, choosing
  Light.xaml or Dark.xaml per `IThemeService.ResolvedTheme`; roughly
  twenty View/Control files had their local `RojanTheme.xaml`
  self-merge removed (converters/local styles they also carried were
  preserved) so every consumer now shares the single app-level
  resource tree the way the Localization platform's culture/RTL setup
  already worked. `RojanTheme.xaml` itself is kept (now pointing at
  Light.xaml) as a standalone convenience aggregate — unused by the
  running app, but not broken for any future consumer, satisfying
  "maintain backward compatibility." A new Theme section was added to
  Settings, directly beside Language, sharing its `RestartCommand`
  (one relaunch applies whichever preference(s) changed) — new
  `Strings.Settings_Theme_*` keys across fa-IR/en-US/ar-SA. 898/898
  tests passing (11 new): `Presentation.Tests` (+5:
  `SettingsPageViewModelTests`' new Theme-section cases — preselect,
  select-without-persisting, apply-with-restart-required,
  apply-to-same-theme, localized display text), `Shell.Tests` (+6:
  `ThemeServiceTests` — first-launch Light default, persisted-mode
  restore, corrupt-file fallback, System-mode resolution against the
  live registry without asserting a specific OS value, restart-required
  semantics for both a changed and an unchanged resolved theme). No
  new `ArchitectureTests` were needed — the existing dependency-direction
  checks already cover the new `Presentation.Theming`/`Shell.Theming`
  namespaces by pattern. Runtime verified end-to-end via UI Automation:
  first launch (no persisted `theme.json`) rendered the warm Light
  theme by default across Dashboard and Settings; selecting Dark,
  applying, and clicking "Restart Now" correctly relaunched the
  process, persisted `{"mode":"Dark"}`, and rendered the full dark
  navy theme identically to before this pass; selecting "Match
  System" and applying correctly persisted `{"mode":"System"}`. No
  `StaticResourceExtension` errors at any point, confirming the
  resource-dictionary restructuring resolves correctly end-to-end
  despite removing every file's redundant self-merge.
- Phase 22: Enterprise Multi-Branch & Organization Platform — transforms
  the app into a multi-tenant platform: `Domain.Organizations`
  (`Organization`, `Branch`, `BranchSettings` and its sub-records,
  `Permission` — 23 members, `WorkspaceRole` — 11 members, and
  `RolePermissions`, the static Permission Engine core), a full
  Application layer (DTOs, `OrganizationMapper`, `IOrganizationQueryService`/
  `IOrganizationCommandService`, `IPermissionEngine`), and
  `FakeOrganizationRepository` seeding two organizations ("ROJAN Beauty
  Group" with Downtown/Uptown branches, "Luxe Salon Collective" with
  one branch), each branch with its own `BranchSettings`.
  `ICurrentSessionService`/`CurrentSessionService` follow the same
  "interface in Presentation, concrete in Shell" split as Localization/
  Theming, persisting the active Organization/Branch/Role to their own
  `session.json` — unlike Language/Theme, branch and role switches are
  **live** (`SessionChanged`), not restart-required. Navigation is now
  permission-aware via one additive, optional `ModuleMetadata.RequiredPermission`
  field (defaults to `null` — every pre-existing module is unaffected);
  `MainWindowViewModel` filters and live-refreshes `NavigationItems` on
  every session change. A new header Branch Switcher live-switches the
  active branch. `OrganizationPageViewModel`/`OrganizationPage` (gated
  by `Permission.OrganizationManage`) provide the admin surface —
  Organizations, Branches (scoped to the selected organization, the
  concrete "no cross-branch data leakage" proof via
  `GetBranchesAsync`), Branch Settings, a read-only Permissions
  reference grid, and Session — following every other module page's
  Loading/Empty/Error/Retry/Refresh/Last-Updated shape, entirely
  localized (fa-IR/en-US/ar-SA, ~50 new string keys) with no hardcoded
  brushes or strings. Deliberate scope boundary: org/branch filtering
  is proven end-to-end on this new vertical slice only — the 10+
  existing modules' repositories were not retrofitted, consistent with
  this codebase's established "foundation now, wire up later"
  precedent. 32 new tests across `Domain.Tests`
  (`RolePermissionsTests`), `Application.Tests` (`PermissionEngineTests`,
  `OrganizationQueryServiceTests`), `Infrastructure.Tests`
  (`FakeOrganizationRepositoryTests`), and `Shell.Tests`
  (`CurrentSessionServiceTests`, `MainWindowViewModelNavigationTests`)
  — full solution suite (922 tests) passes, zero warnings, zero
  errors. Runtime-verified via UI Automation on both Debug and the
  Release self-contained publish: permission-gated "Organizations" nav
  entry, header Branch Switcher listing the seeded branches, all five
  Organization page sections rendering correctly under the Fluent 2
  Premium theme in RTL Persian, and the Permissions grid's displayed
  grants matching `RolePermissions` exactly for every role.
- Phase 22 Enhancement Pass — extends the Enterprise Multi-Branch
  platform above additively per a follow-up specification, without
  reverting or rewriting anything already committed. `Organization`
  gains `Code`/`Phone`/`Email`/`Address` (new Create Organization form
  fields) plus `TimeZone`/`Language`/`Currency` (infrastructure-only,
  defaulted on create). `Permission` gains `Approve`/`Reject`/`Import`/
  `ManageUsers`, granted to Accounting/Hr (Approve/Reject),
  Inventory/Accounting (Import), and OrganizationManager/BranchManager
  (ManageUsers) — additive, no role lost a permission. `WorkspaceRole`
  gains `Marketing` (DashboardView/CustomerRead/ReportingView/AiUse) —
  the one genuinely new role among the follow-up spec's examples, every
  other named role mapping onto an existing member. The header Branch
  Switcher is rebuilt from a plain `ComboBox` into a real Fluent 2
  flyout (`Popup`, `PopupAnimation="Fade"` for smooth transitions): a
  search box filtering by branch name/code across every organization,
  Favorite and Recently-used sections backed by new
  `ICurrentSessionService.FavoriteBranchIds`/`RecentBranchIds`
  (persisted in `session.json`, recents capped at
  `CurrentSessionService.MaxRecentBranches` = 5), and every
  organization's branches grouped by name — not just the current
  organization's. 17 new tests (`RolePermissionsTests` +6,
  `CurrentSessionServiceTests` +3, new
  `MainWindowViewModelBranchSwitcherTests` +7) — full solution suite
  (939 tests) passes, zero warnings, zero errors. Runtime-verified via
  UI Automation on both Debug and a fresh Release self-contained
  publish: the Create Organization form's four new fields render
  correctly in RTL Persian, and the redesigned Branch Switcher opens
  via `InvokePattern`, correctly grouping both seeded organizations
  with their branches, branch codes, and favorite-star affordances.
- Phase 22A Enterprise Context Migration — integrates the Phase 22
  platform into the rest of the app. Two new Application-layer
  abstractions: `IEnterpriseContext` (current organization/branch/role,
  Presentation-independent - Shell's `CurrentSessionService` implements
  it alongside `ICurrentSessionService`, one object registered once and
  aliased to both) and `IPermissionGate`/`PermissionGate`
  (`Ensure(Permission)` throws `UnauthorizedOperationException` rather
  than returning a bool). Every mutating command-service interface
  across every module (17 total: Customers, Bookings, BookingWorkflow,
  Calendar, Specialists, Services, Inventory, Invoices, Payments,
  Employees, Attendance/Leave, Shifts, Commissions, Payroll, Report
  Snapshots, Report Export, AI Conversations/Chat, Organizations/
  Branches) is now wrapped by a `*PermissionGate` decorator registered
  in place of the raw implementation - zero changes to any existing
  command service class or its tests, since Presentation only ever
  resolves the public interface. Full per-record Organization/Branch
  data isolation (a real `OrganizationId`/`BranchId` field, filtered at
  the query-service boundary) was implemented end-to-end for three
  flagship modules covering this phase's explicitly named categories -
  Customers ("CRM"), Bookings ("Appointments"), Inventory ("Products") -
  with seed data deliberately spread across org-1/branch-1,
  org-1/branch-2, and org-2/branch-3 so the guarantee has genuinely
  mixed data to filter, not a single-tenant fixture. Remaining modules
  get full Permission Enforcement via the decorator pattern but were
  not individually migrated to per-record scoping this pass - a
  documented, deliberate scope boundary (retrofitting the same
  entity+DTO+mapper+seed+test changes into every remaining module's
  entities is the same-shaped but multiplicatively larger effort the
  three flagship migrations already prove end-to-end). 16 new tests
  (`PermissionGateTests`, two decorator-level "unauthorized never
  reaches the inner service" proofs, and Organization/Branch-Scoping
  cases added to `CustomerQueryServiceTests`/`BookingQueryServiceTests`/
  `ProductQueryServiceTests`) - full solution suite (955 tests) passes,
  zero warnings, zero errors. Runtime-verified via UI Automation:
  Customers/Bookings/Inventory all load correctly scoped to the default
  session's branch (Inventory's Total Products reads 8 of the 10
  seeded, the other 2 correctly excluded as belonging to a different
  branch/organization).
- Phase 23 Enterprise UX/UI Refinement & Localization Completion —
  design-token and localization pass across the whole app. Color system:
  retargeted the existing Fluent 2 brush tokens to a layered surface
  hierarchy (`Rojan.Brush.Background` → warm cream, new
  `Rojan.Brush.Workspace` token → soft lavender for the page content
  area, `Rojan.Brush.Card`/`Surface` → pure white), replacing the large
  flat-white workspace look with no XAML consumer changes since every
  page already referenced these brushes by key. Typography: `Display`/
  `Title` moved to Bold and enlarged, `SectionHeader`/`Subtitle`/`Body`/
  `Caption` all strengthened. New `DataGrid`/`DataGridColumnHeader`/
  `DataGridCell`/`DataGridRow` implicit styles in `Controls.xaml`.
  Localization: closed ~360 hardcoded-English-string gaps across 13
  module pages (~180 new `Strings.cs`/resx entries across
  `Strings.resx`/`Strings.en.resx`/`Strings.ar.resx`), plus two
  systemic gaps invisible to a page-by-page XAML audit — Dashboard's
  KPI/activity labels came from Infrastructure's fake-repository seed
  data and couldn't reach Presentation's `Strings` directly (fixed with
  `KpiLabelConverter`/`ActivityDescriptionConverter` mapping each DTO's
  stable `Id` to a localized string), and every Domain status/type/
  method enum (`CustomerStatus`, `BookingStatus`, `InvoiceStatus`,
  `PaymentMethod`, `StockTransactionType`, etc.) rendered its raw C#
  member name via default `ToString()` regardless of language (fixed
  with one shared `EnumLabelConverter` + `Strings.GetEnumLabel`, keyed
  by member name so one key set covers every enum). `AiCenterPage.xaml`
  and `HrPage.xaml` (the two largest hardcoded-string surfaces, ~150
  combined instances) are explicitly deferred as a documented scope
  boundary, per the Phase 22/22A precedent - see
  `docs/phases/phase-23-enterprise-ux-ui-refinement.md` for the full
  breakdown including other deliberately-deferred edges (enum
  `ComboBox` pickers, calendar/date formatting). No business-logic or
  test changes; full 955-test suite still passes, zero warnings, zero
  errors. Runtime-verified via UI Automation against the default fa-IR
  session across Dashboard/Customers/Bookings/Inventory/Accounting - one
  real gap (`BookingPage`'s detail-panel card title) was caught live
  during this verification and fixed on the spot.
- Phase 24 Localization Audit Completion — closes the two scope
  boundaries Phase 23 explicitly deferred. `AiCenterPage.xaml` and
  `HrPage.xaml` (the app's two largest hardcoded-string surfaces) are
  now fully localized (~103 new `Strings.cs`/resx entries, `Ai_*`/
  `Hr_*`, reusing existing `Common_*`/`Reporting_*` keys wherever
  possible). Completing them surfaced five more enum types Phase 23's
  sweep hadn't reached (`ConversationRole`, `InsightSeverity`,
  `InsightCategory`, `RecommendationPriority`, `AIProviderType`,
  `EmployeeRole`, `Department`, `EmploymentType`, `EmployeeStatus`,
  `AttendanceStatus`, `CommissionType`, `LeaveStatus`); a follow-up
  solution-wide sweep for any remaining bare enum binding caught three
  more sites in pages already "done" for their literal strings
  (`OrganizationPage`'s `WorkspaceRole` displays, `ReportingPage`'s
  `ReportCategory` column, `ServicePage`'s `ServiceCategory` column) -
  51 new `Enum_<MemberName>` resx entries cover all of them through the
  same shared `EnumLabelConverter`/`Strings.GetEnumLabel` mechanism
  Phase 23 introduced, no new converter code needed. Also closed
  Phase 23's one flagged residual gap: `ServicePage`'s duration KPI
  used a hardcoded `StringFormat={}{0} min}` that couldn't route
  through `Strings` (`KPIValue.Value` is a plain string, not a
  `Run`-composed `TextBlock`) - fixed with a new
  `MinutesSuffixConverter` (`Rojan.Converter.MinutesSuffix`). Still
  out of scope, unchanged from Phase 23: editable enum `ComboBox`
  pickers, fake-repository seed data, Gregorian date formatting - see
  `docs/phases/phase-24-localization-audit-completion.md`. No new
  tests (XAML/resx + one converter only, no business logic); full
  955-test suite passes unchanged, zero warnings, zero errors.
  Runtime-verified via UI Automation: Staff & HR, Services, and AI
  Center all render fully in Persian, including every status/role/
  department/category badge and the duration KPI's "۶۰ دقیقه".
- Phase 25 Enterprise Identity & Secure Client Platform — foundation
  architecture for secure Windows/Desktop, Android, and future Web
  clients on top of the approved Hybrid Offline/Online model
  (commercial licensing/payment/usage limitations explicitly out of
  scope). `Domain.Identity` (`OrganizationIdentity`, `BranchIdentity`,
  `WorkspaceIdentity`, `UserIdentity`, `DeviceIdentity`,
  `InstallationIdentity`, `SessionIdentity`) and `Domain.Security`
  (`AuthenticationState`/`ConnectionState`/`SyncState`/`CertificateState`
  enums, `AuthToken`/`RefreshToken`/`DeviceFingerprint`/
  `OfflineCertificate`/`PendingSyncOperation`/`SyncConflict` value
  objects, `SessionRules`/`CertificateRules` pure state derivation) are
  new, dependency-free bounded contexts. Application gains 11 new
  interfaces (`IDeviceRegistrationService`, `IIdentityContextService`,
  `IAuthenticationService`, `ISessionService`, `ICertificateService`,
  `IConnectivityService`, `ISyncQueueService`, `IApiClient`,
  `ISecureStorageService`, `ISecretProvider`, `IKeyProvider`,
  `IEncryptionService`) plus a working `RetryPolicy` (exponential
  backoff with jitter, infrastructure-free like `PermissionEngine`).
  Infrastructure ships real, working implementations, not stubs: device
  registration mints and persists a real device/installation id and
  recomputes a SHA-256 hardware fingerprint every launch;
  `LocalSessionService` issues real random token pairs with real
  expiry math and restores across restarts; `LocalCertificateService`
  issues a locally-generated offline certificate with a real 365-day
  validity window; `DpapiSecureStorageService`/`AesEncryptionService`/
  `LocalKeyProvider` use real Windows DPAPI and AES-256-GCM, not
  placeholder encryption; `ConnectivityService` uses real
  `NetworkInterface`/`NetworkChange` APIs; `SyncQueueService` persists
  a real durable queue and genuinely attempts to drain it through
  `HttpApiClient` (which fails honestly with a clear
  `ApiConnectivityException` today, since `ROJAN_API_BASE_URL` is
  unset - no backend exists yet, no hardcoded endpoint either).
  `HttpApiClient` composes connectivity-checking, retry, Bearer-token
  attachment, a 30s timeout, and exception mapping around one owned
  `HttpClient`. Every new service is registered in `AddInfrastructure()`/
  `AddApplication()` (no Service Locator); `Shell.App.xaml.cs` bootstraps
  device registration/session restoration/certificate issuance/sync-
  queue restoration at startup, mirroring the existing culture/theme/
  session ordering. No Presentation-layer changes (no UI consumes any
  of this yet - see `docs/phases/phase-25-enterprise-identity-secure-platform.md`'s
  "Why no Presentation changes"), so zero risk to Fluent 2/localization/
  theme/accessibility. 68 new tests (955 → 1023), full suite passes,
  zero warnings, zero errors, `ArchitectureTests` (dependency-direction
  enforcement) included. Runtime-verified: DI graph resolves cleanly,
  device/certificate state persists correctly with real generated
  values, and a full screenshot pass confirms zero UI regressions.
- Enterprise Theme Refinement (Premium Lavender Enhancement) — increases
  lavender presence across every named chrome/content surface via
  explicit target colors, color-tokens-only (no layout/spacing/
  typography/component-hierarchy changes). `Rojan.Color.Background`
  (app chrome/status bar) → `#FCF8FF`, `Workspace` → `#F1EAFE`, `Card`/
  `Surface`/`SurfaceElevated` → `#FAF6FF` (deliberately no longer
  literal pure white), plus two brand-new tokens replacing what
  previously all shared `Background`: `Header` (`#F5EEFE`,
  `MainWindow`'s top bar) and `Navigation` (`#F8F2FE`, the sidebar).
  Text colors, borders, `SurfaceSecondary`/`SurfaceHover`/
  `SurfacePressed`, and every semantic/status/accent color are
  unchanged (not named in this pass's target list), so WCAG contrast
  and the existing Fluent 2 styling are unaffected. `Dark.xaml` keeps
  its own existing navy palette unchanged (only gains the two new
  `Header`/`Navigation` keys, aliased to its existing `Background` so
  selecting Dark theme does not throw a missing-resource error) - no
  dark-theme target values were given, only light-theme hex codes.
  Full 1023-test suite passes unchanged, zero warnings, zero errors.
  Runtime-verified via UI Automation screenshots across Dashboard/
  Customers/Bookings/Inventory/Accounting: consistent warmer, softer
  lavender chrome on every page, cards read as soft off-white rather
  than stark white, text contrast and primary-button accent color
  unchanged.
- Phase 26 ROJAN Smart Context Help (SCH) — a centralized, reusable Help
  engine: context/module/page detection, localized content resolution,
  a Fluent-2-styled Help button and dialog, instant keyword search with
  highlighting, and back/forward/breadcrumb/related-topics/favorites/
  recently-viewed navigation. `Domain.Help` (`HelpTopic`, `HelpShortcut`,
  `IHelpRepository`, `HelpContentRules` — pure context resolution +
  version compatibility) and `Application.Help`
  (`IHelpQueryService`/`HelpQueryService`, `IHelpSearchService`/
  `HelpSearchService` — culture-aware weighted search with per-field
  highlight spans, `IHelpFavoritesStore`, `IHelpRecentlyViewedStore`)
  are new, dependency-clean layers. `Infrastructure.Help.HelpTopicRegistry`
  seeds real, substantive Persian/English/Arabic content (~98 new
  `Strings.cs`/resx entries) for 6 flagship modules (Dashboard,
  Customers, Bookings, Inventory, Accounting, Services) plus one generic
  fallback topic — the remaining 8 modules resolve to that fallback, the
  same "flagship subset now, documented boundary for the rest" pattern
  Phase 22A/23/24 already established. `Presentation.Help.HelpContentResolver`
  is the one place a topic's `KeyPrefix` becomes localized display text
  (via a new `Strings.GetByKey` wrapper), keeping Domain/Application/
  Infrastructure free of literal strings entirely.
  `Controls.Help.HelpButton` (animated Fluent-2 icon button) and
  `Views.Help.ContextHelpDialogView` (scrollable dialog with focus
  trapping, ESC/scrim-close scoped only to itself so no other existing
  dialog's behavior changes) plug into the app's existing dialog-region
  chrome unchanged. `MainWindowViewModel.OpenHelpCommand` constructs
  `HelpDialogViewModel` via `new` — the same established
  constructed-by-its-opener shape as `PosCheckoutViewModel`/
  `ExportDialogViewModel` — rather than a DI registration, since it
  needs a runtime module/page context. AI Help, Smart Suggestions,
  Context Prediction, Natural Language Questions, and Interactive
  Walkthrough are extension points only (a "coming soon" placeholder
  section in the dialog) — no AI implementation, per spec. No existing
  page's layout, colors, spacing, or controls changed. 61 new tests
  (1023 → 1084), full suite passes, zero warnings, zero errors,
  `ArchitectureTests` included. Runtime-verified via UI Automation: the
  Help button opens the dialog with correctly localized content in
  every section, search returns ranked/highlighted results, and
  clicking a Related Topic link navigates the dialog in place and
  correctly enables Back — see
  `docs/phases/phase-26-smart-context-help.md`.
- Phase 27 Enterprise Notification Center — a centralized notification
  service, Fluent-2 toast popups, and an in-app Notification Center
  panel with search/filtering/grouping/history/badge counter. `Domain.Notifications`
  (`AppNotification`, `NotificationFilter`, own `NotificationSeverity`/
  `NotificationPriority` enums, `INotificationRepository`,
  `NotificationRules` — pure filter matching, group-key fallback,
  priority ranking, and the Silent Mode toast rule) and
  `Application.Notifications` (`INotificationService`/`NotificationService`
  — raise/query/mark-read/dismiss/clear/Silent-Mode plus
  `NotificationRaised`/`ToastRequested`/`StateChanged` events,
  `INotificationSearchService`/`NotificationSearchService` — the same
  weighted, highlighted substring search shape as Phase 26's
  `HelpSearchService`) are new, dependency-clean layers. Application
  deliberately owns its **own** mirror `NotificationSeverity`/
  `NotificationPriority` enums rather than reusing Domain's — the same
  pattern `Application.Customers.CustomerStatus` already establishes —
  so Presentation never needs a Domain reference, verified by the
  unchanged `ArchitectureTests.DependencyDirectionTests`.
  `Infrastructure.Notifications.LocalNotificationRepository` persists a
  capped (500-entry) JSON history;
  `LocalSilentModePreferenceStore` persists the Silent Mode toggle.
  Silent Mode's rule: while enabled, only `Critical`-priority
  notifications still produce a toast — the Notification Center/history
  is never affected, only the toast popup surface.
  `Presentation.Notifications.NotificationContentResolver` is the one
  place a notification's resx keys become localized display text (~50
  new `Strings.cs`/resx entries, including 6 fully-authored demo
  notifications and 4 new shared `Enum_<MemberName>` severity labels).
  `ToastHostViewModel`'s auto-dismiss timer is abstracted behind a new
  `IToastDismissScheduler` (the real `DispatcherTimer`-backed
  implementation lives outside the `ViewModels` namespace) specifically
  so it satisfies `ArchitectureTests.ViewModelTestabilityTests` ("no
  ViewModel depends on `System.Windows.Threading`"). The header bell
  button's popover — a Phase 07 placeholder explicitly built for this —
  is now the real Notification Center, with a new
  `Rojan.Style.NotificationBadge` unread-count pill overlaid on the
  bell. Toasts render in a brand-new overlay region, deliberately never
  routed through `IDialogService`/`ActiveDialog` (the modal dialog
  region holds one value at a time and shows a scrim — structurally
  incompatible with a stack of non-modal, auto-dismissing popups that
  must stay visible even while a dialog is open).
  `MainWindowViewModel` constructs both `NotificationCenterViewModel`
  and `ToastHostViewModel` directly via `new` (not DI-registered),
  passing through its own already-injected dependencies. Future
  push-notification delivery is architecture-only (`RaiseAsync` is
  already the single entry point a push handler would call) — no push
  implementation, per spec. No existing page's layout, colors, spacing,
  or controls changed. 265 new tests (1084 → 1349), full suite passes,
  zero warnings, zero errors, `ArchitectureTests` (dependency-direction
  and ViewModel-testability) included. Runtime-verified: 6 seeded demo
  notifications appear grouped/searchable/filterable in the Notification
  Center on first launch, the badge shows the correct unread count, and
  Silent Mode persists across a toggle — see
  `docs/phases/phase-27-enterprise-notification-center.md`.
- Phase 28 Enterprise Global Search & Command Palette — a `Ctrl+K`/
  `Ctrl+P` command palette searching pages, modules, customers,
  bookings, specialists, services, products, and commands with
  intelligent ranking, fuzzy matching, recent searches, favorites,
  search highlighting, and instant results. `Domain.Search`
  (`MatchSpan`, `FuzzyMatchResult`, `SearchRules.Match` — culture-aware
  exact/prefix/substring scoring falling back to fuzzy subsequence
  matching, always scored lower than any substring/prefix/exact match)
  and `Application.Search` (`SearchResultType` — a UI-facing taxonomy
  with no Domain equivalent, `HighlightSpan` — a deliberate mirror of
  `Domain.Search.MatchSpan`, `ISearchRankingService`/
  `SearchRankingService` — title+keyword weighted scoring plus a
  type-priority bonus for Commands/Pages and a favorite bonus,
  `IGlobalSearchIndexService`/`GlobalSearchIndexService` — aggregates
  live candidates from five sibling Application query services,
  `ISearchHistoryStore`, `ISearchFavoritesStore`) are new,
  dependency-clean layers. `Application.Search`'s dependency on
  `ICustomerQueryService`/`IBookingQueryService`/
  `ISpecialistQueryService`/`Application.Services.IServiceQueryService`/
  `IProductQueryService` is an Application-to-Application dependency,
  not a layer violation, the same shape the existing Reporting/
  Analytics aggregators already establish — verified by the unchanged
  `ArchitectureTests.DependencyDirectionTests`.
  `Infrastructure.Search.LocalSearchHistoryStore` persists up to 10
  recent searches (case-insensitive dedup-and-move-to-front);
  `LocalSearchFavoritesStore` persists favorited candidate ids.
  `Presentation.Search.StaticSearchCatalog` (a static class, not
  DI-registered) supplies already-localized Page/Command candidates —
  one per registered module plus 7 curated commands (toggle sidebar,
  toggle notifications, open help, toggle Silent Mode, go back, go
  forward, open branch switcher). `CommandPaletteViewModel` is
  constructed directly via `new` by `MainWindowViewModel` (not
  DI-registered) so its search state never leaks between opens, the
  same "constructed by its opener" shape `HelpDialogViewModel` and the
  Notification Center's ViewModels already establish; a Command result
  is invoked through a `Dictionary<string, ICommand>` action map
  `MainWindowViewModel` builds from its own already-wired commands,
  keeping the palette fully decoupled from Shell. A new
  `Window.InputBindings` block (first use of this WPF mechanism in the
  codebase) binds both `Ctrl+K` and `Ctrl+P`; the previously
  non-functional header search box placeholder is now clickable and
  opens the same palette. Inside the palette, the search box never
  loses keyboard focus — its own `PreviewKeyDown` handler drives
  Up/Down/Enter/Escape against the ViewModel, while the results list is
  non-focusable and only reflects the selection visually, the classic
  command-palette UX pattern. 19 new `Strings.cs`/resx entries across
  fa-IR/en/ar (palette chrome, 7 result-type labels, 7 command titles)
  — no hardcoded text anywhere in Presentation/Application/Domain/
  Infrastructure. No existing page's layout, colors, spacing, or
  controls changed. 51 new tests (1148 → 1199), full suite passes, zero
  warnings, zero errors, `ArchitectureTests` (dependency-direction and
  ViewModel-testability) included. Runtime-verified: `Ctrl+K`/`Ctrl+P`
  and the header search box all open the palette, search returns
  instant ranked/highlighted results across pages/commands/business
  data, keyboard navigation and execution both work, and Recent
  Searches/Favorites persist across palette re-opens — see
  `docs/phases/phase-28-enterprise-global-search-command-palette.md`.
- Phase 29 Enterprise Workspace & Window Management — multi-workspace
  support, dockable panels, floating windows, split view, tab management,
  workspace save/restore with restore-last-workspace-on-startup, recent
  workspaces, reset workspace, and 7 new keyboard shortcuts, layered
  additively around the pre-existing sidebar-driven primary content
  region (unchanged since Phase 07). `Domain.Workspaces` (`PaneNode` -
  `PaneLeaf`/`PaneSplit`, a recursive tree - `DockedPanelState`,
  `FloatingWindowState`, `WorkspaceLayout`, `WorkspaceRules` - pure
  split/open-tab/close-tab/resize/collapse-empty-split logic,
  `IWorkspaceRepository`) and `Application.Workspaces` (a full parallel
  mirror of every Domain type plus `WorkspaceMapping` - the shared
  Domain<->DTO translation - `IWorkspaceService`/`WorkspaceService` -
  async, persisted CRUD/switch/save/reset/recent - and `PaneTreeRules` -
  a pure, synchronous, in-memory DTO-facing wrapper around
  `WorkspaceRules`, so interactive pane operations stay instant with no
  I/O per click/drag) are new, dependency-clean layers, the same "own
  mirror type at each layer boundary" pattern Phase 27/28 established.
  `Infrastructure.Workspaces.LocalWorkspaceStore` persists
  `workspaces.json`/`state.json`, with `PaneNode`'s polymorphism handled
  by a private wire-only `PaneNodeRecord` hierarchy (`JsonDerivedType`)
  so Domain stays free of any serialization concern; Recent Workspaces is
  capped at 5. `Presentation.ViewModels.Workspaces.WorkspaceHostViewModel`
  is the orchestrator - constructed via `new` by `MainWindowViewModel`
  (not DI-registered), the same "constructed by its opener, lives for the
  app's lifetime" shape `NotificationCenterViewModel`/`ToastHostViewModel`
  already establish - with a leaf/tab instance cache so an unrelated
  structural change never discards a tab's live content ViewModel state.
  Tab content for a secondary pane resolves through the exact same
  `ModuleDescriptor.CreateViewModel`/implicit-DataTemplate mechanism the
  primary pane already uses - zero changes to any of the 14 existing
  module Views/ViewModels. `Shell.Workspaces.FloatingWindowManager`
  (`IFloatingWindowManager`) opens real `Window` instances
  (`FloatingModuleWindow`, reusing `MainWindow`'s WindowChrome idiom, no
  manual theme merge needed since `ThemeResources.Apply` already merges
  the whole design system into `Application.Resources`).
  `MainWindow.xaml`'s pre-existing `NavigationHost` `ContentControl` is
  untouched - wrapped one `Grid` level deeper by a secondary-pane column
  and the Workspace Outline dock column (Phase 29's flagship dockable
  panel - every open tab/floating window, click to focus/close), both
  zero-width by default via `BoolToGridLengthConverter`/
  `DoubleToGridLengthConverter`, so a workspace that's never been
  split/docked renders pixel-identical to Phase 28. One new header
  button/popover (Workspace switcher: create/rename/duplicate/delete/
  reset, Recent Workspaces, pane actions) reuses the exact Branch
  Switcher popover chrome; one new curated command
  (`open-workspace-switcher`) wires into the Phase 28 Command Palette. 7
  new `Window.InputBindings` extend the one list Phase 28 started
  (Ctrl+Shift+D/J split, Ctrl+W close tab, Ctrl+Shift+F float out,
  Ctrl+Tab/Ctrl+Shift+Tab cycle, Ctrl+Shift+W switcher, Ctrl+Shift+R
  reset). 20 new `Strings.cs`/resx entries across fa-IR/en/ar, 9 new
  `Rojan.Icon.*` glyphs (existing Segoe Fluent Icons family) - no
  hardcoded text anywhere in Presentation/Application/Domain/
  Infrastructure. No existing page's layout, colors, spacing, or
  controls changed. 64 new tests (1199 → 1263), full suite passes on
  both Debug and Release, zero warnings, zero errors, `ArchitectureTests`
  (dependency-direction and ViewModel-testability) included.
  Runtime-verified: the app launches and renders identically to Phase 28
  with the one new header button correctly iconified, and first-run
  bootstrap was confirmed end-to-end by reading the persisted
  `workspaces.json`/`state.json` after the run — see
  `docs/phases/phase-29-enterprise-workspace-window-management.md`.
- Phase 32 Enterprise Automation, Workflow & Business Rules Engine — a
  brand-new Clean Architecture vertical slice: a workflow step-graph
  engine (Start/End/Decision/Delay/Approval/Condition/Notification/Email/
  AI Action/Database Action/API Action), a configurable Business Rules
  Engine ("IF Customer is VIP → Apply Discount" etc.), a Trigger Engine
  (10 trigger types), Cron-ready Scheduled Jobs, multi-step Approval
  Workflow (Leave/Expense/Inventory/Branch), Draft→Published→Archived
  Versioning with rollback, execution Monitoring/Audit, retry/backoff
  Error Recovery, and a summary Dashboard. `Domain.Automation`
  (`WorkflowStep`/`WorkflowDefinition`, `WorkflowRules` — pure validation/
  BFS-reachability — `RetryPolicy`/`RetryRules`, `BusinessRule`/
  `BusinessRuleEngine`, `ScheduledJob`/`ScheduleRules`, `ApprovalRequest`/
  `ApprovalRules`, `WorkflowExecution`, 5 repository interfaces) and
  `Application.Automation` (full mirror DTOs + `AutomationMapping`;
  `WorkflowService`/`BusinessRuleService`/`ScheduledJobService`/
  `ApprovalService`/`TriggerEngine`; `WorkflowExecutionEngine` — the
  step-graph run loop with real per-step retry/backoff wired to
  `Domain.RetryRules`; 11 `IWorkflowStepExecutor` implementations, Database
  Action/API Action deliberately no-op per this phase's "contracts only,
  no external calls" scope, the same boundary Requirement 32.7 already set
  for AI Action's `NoOpAiActionExecutor`) are new, dependency-clean
  layers — unlike Phase 29, the run loop lives directly in Application
  (which may reference Domain), so no Presentation-facing pure-logic
  wrapper was needed this time. `Infrastructure.Automation` persists 5
  JSON files under `%LocalAppData%\RojanDesktop\automation\` (execution
  history capped at 500 entries) plus an email outbox (no real SMTP, per
  Requirement 32.6's own scope) and `WorkflowSchedulerService` — a plain
  `Timer`-driven class, deliberately **not** an `IHostedService`, started/
  stopped explicitly around the WPF `Application` lifecycle in
  `App.xaml.cs`, consistent with this Shell's Generic Host being used for
  DI composition only. A workflow's `Approval` step pauses its execution
  (`WorkflowExecutionStatus.Waiting`) via `ApprovalRequest.WorkflowExecutionId`
  and `ApprovalService.DecideAsync` automatically resumes/fails it once
  decided — the one seam connecting the two subsystems. Two new
  permissions (`AutomationView`/`AutomationManage`, granted to
  `OrganizationManager`/`BranchManager`) exist in both the Domain and
  Application `Permission` mirrors; `AutomationModule` is the first module
  to actually populate `ModuleMetadata.RequiredPermission`. New
  `AutomationPage` (5 tabs: Dashboard/Workflows/Business Rules/Scheduled
  Jobs/Approvals) built entirely from existing `DashboardCard`/
  `DashboardWidget`/`KPIValue` controls and Fluent 2 tokens — no new
  colors, typography, or layout primitives; no existing module changed.
  46 new `Strings.cs`/resx entries across fa-IR/en/ar — no hardcoded text
  anywhere in Presentation/Application/Domain/Infrastructure. 157 new
  tests (1263 → 1420), full suite passes on both Debug and Release, zero
  warnings, zero errors, `ArchitectureTests` (dependency-direction and
  ViewModel-testability) included. Runtime-verified: both builds clean;
  the compiled Shell launched and ran 8 seconds with zero Application-log
  errors before closing cleanly; the persistence/execution stack itself
  was verified via the automated suite running the real
  `Local*Repository`/`WorkflowExecutionEngine`/step-executor code against
  real temp-file JSON (not mocks) — see
  `docs/phases/phase-32-enterprise-automation-workflow-business-rules-engine.md`.

- UI Polish & Localization Sprint (pre-Phase-33) — a full-application pass
  fixing inconsistencies, localization, theme, and UI quality, no new
  business features. **Localization**: translated all 12 `Fake*Repository`
  seed-data fixtures (Customers/Services/Specialists/Bookings/Inventory/
  Calendar/Organizations/Dashboard/HR/Accounting/Reporting/AI — several
  hundred strings: names, descriptions, notes, categories, suppliers) from
  English demo content to a coherent Persian salon-business persona
  (customers/specialists/employees renamed consistently across every
  cross-referencing file; organizations/branches renamed to Tehran
  neighborhoods); added ~50 missing `Enum_*` resx entries (fa-IR/en/ar)
  the `EnumLabelConverter` was silently falling back to raw English enum
  names for (`WorkflowStepType`, `WorkflowStatus`, `TriggerType`,
  `WorkflowExecutionStatus`'s Running/Waiting/Failed,
  `BusinessRuleOperator`, `BusinessRuleActionType`, `ScheduleFrequency`,
  `ApprovalType`'s Leave/Expense/Branch) — a real, previously-undetected
  localization gap in Phase 32's own Automation UI; fixed several literal
  hardcoded English strings directly in `AutomationPage.xaml` ("v",
  " · Priority ", " · Next: ") and one binding missing its `EnumLabel`
  converter entirely (workflow version history's Status column); fixed
  hardcoded English labels/date-format constructed in the Application
  layer that reach the UI unlocalized — `KpiEngineQueryService`'s 8 KPI
  names, `ReportExecutionQueryService`'s dictionary summary keys and
  `MetricRow` labels across every report type, and
  `CustomerProfileQueryService.BuildStatistics`'s 5 stat-card labels
  (`CultureInfo.InvariantCulture`'s `"MMM d, yyyy"` date format also
  replaced with a numeric `"yyyy/MM/dd"` so it can no longer render
  English month abbreviations regardless of app language). **Currency**:
  removed every hardcoded `"$"` literal from seed data and Application
  defaults, replacing it with Toman-formatted values (`"1,200,000 تومان"`,
  `"رایگان"` for genuinely complimentary services); `CultureService.
  GetCultureInfo` now overrides `NumberFormat.CurrencySymbol`/
  `CurrencyDecimalDigits`/`CurrencyPositivePattern` for the app's fa-IR
  culture, so every existing `{0:C}` binding across HR/Accounting/
  Reporting/KPI cards renders `"X تومان"` consistently from one
  registration rather than needing dozens of individual XAML edits.
  **Critical fix caught during this pass**: `Reporting.MoneyParser.Parse`/
  `Accounting.AccountingMapper.ParseMoney` (used by POS Checkout, the
  Appointments/Service-Popularity/Specialist-Performance/Inventory-
  Valuation reports, and Analytics' inventory-value aggregation) only
  stripped a leading `"$"` before calling `decimal.TryParse` — with the
  new `"650,000 تومان"`-shaped price strings this would have silently
  parsed to `0m` everywhere, a real regression this sprint's own currency
  change would have introduced; both parsers now strip the `"تومان"`/
  `"﷼"` suffix and treat `"رایگان"` as `0m` explicitly, with new test
  coverage. **Theme**: `Rojan.Color.Workspace` (Light theme's main
  content-region background) darkened from `#FFF1EAFE` to `#FFCDC7D8`
  (~15%, RGB scaled by 0.85 - preserves hue/saturation, lands lightness at
  ~81% vs. the original ~96%); Card/Surface colors and the glass effect
  are untouched since they're separate tokens. **Support Center**: a new
  permanent "پشتیبانی" sidebar entry (`Support` vertical slice - Domain/
  Application/Infrastructure/Presentation, no permission gate, same as
  `AiCenterModule`) with About/Contact Us/Send Message/Send Email/
  Development Participation Request/FAQ/User Guide/Terms & Privacy/
  Version Info sections on one scrollable page (the same "one page,
  several stacked `DashboardCard` sections" shape `Settings.SettingsPage`
  already establishes). "Send Message"/"Contact Super Admin"/"Report a
  Bug"/"Suggestions" share one message form discriminated by a
  `SupportMessageType` enum rather than four near-identical forms; the
  Development Participation form (name/mobile/email/city/collaboration
  area/GitHub/LinkedIn/portfolio/resume/description) is architecture-only
  per its own scope, persisting locally with no review workflow yet.
  Website/phone/support-email/API values come from a new
  `IRojanBrandConfiguration` seam (`Infrastructure.Support.
  RojanBrandConfiguration`) rather than being hardcoded into any View or
  ViewModel - changing them means changing one registration. 46 new
  `Strings.cs`/resx entries across fa-IR/en/ar. **Testing**: 34 new tests
  (1420 → 1455 total, plus fixing 12 pre-existing tests whose assertions
  hardcoded now-translated seed values or English report labels) covering
  `SupportRules` validation, `SupportMessageService`/
  `DevelopmentApplicationService`, the two Local Support repositories, the
  new `SupportPageViewModel`, and `MoneyParser`'s Toman/رایگان handling.
  Full suite passes on both Debug and Release, zero warnings, zero
  errors. Runtime-verified: both builds clean; the compiled Shell launched
  and ran 8 seconds with zero Application-log errors before closing
  cleanly.

### Fixed
- `.editorconfig`: the `[*Tests.cs]` override now also disables `CA1707`,
  closing a gap where the documented
  `MethodUnderTest_Scenario_ExpectedResult` test-naming convention
  (`coding-standards.md` §7) could not actually build under
  `TreatWarningsAsErrors`.

## [0.1.0-alpha] - Unreleased

Initial repository scaffold. No business functionality. Pending Phase 01
approval.
