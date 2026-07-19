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

### Fixed
- `.editorconfig`: the `[*Tests.cs]` override now also disables `CA1707`,
  closing a gap where the documented
  `MethodUnderTest_Scenario_ExpectedResult` test-naming convention
  (`coding-standards.md` §7) could not actually build under
  `TreatWarningsAsErrors`.

## [0.1.0-alpha] - Unreleased

Initial repository scaffold. No business functionality. Pending Phase 01
approval.
