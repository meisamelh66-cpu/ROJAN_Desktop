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

### Fixed
- `.editorconfig`: the `[*Tests.cs]` override now also disables `CA1707`,
  closing a gap where the documented
  `MethodUnderTest_Scenario_ExpectedResult` test-naming convention
  (`coding-standards.md` §7) could not actually build under
  `TreatWarningsAsErrors`.

## [0.1.0-alpha] - Unreleased

Initial repository scaffold. No business functionality. Pending Phase 01
approval.
