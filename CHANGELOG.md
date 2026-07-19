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

### Fixed
- `.editorconfig`: the `[*Tests.cs]` override now also disables `CA1707`,
  closing a gap where the documented
  `MethodUnderTest_Scenario_ExpectedResult` test-naming convention
  (`coding-standards.md` §7) could not actually build under
  `TreatWarningsAsErrors`.

## [0.1.0-alpha] - Unreleased

Initial repository scaffold. No business functionality. Pending Phase 01
approval.
