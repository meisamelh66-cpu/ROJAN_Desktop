# Phase 08 — Testing Strategy

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Turn the five empty test-project scaffolds (`Domain.Tests`,
`Application.Tests`, `Infrastructure.Tests`, `Presentation.Tests`,
`ArchitectureTests`) into a real, production-grade test suite covering the
two existing vertical slices (Dashboard, Customer CRM — see
`docs/adr/0001-phase-09-naming-collision.md` for that module's numbering),
using only what was already pinned in `Directory.Packages.props` (`xunit`,
`xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`,
`coverlet.collector`, `NetArchTest.Rules`) — no Moq/NSubstitute/
FluentAssertions, per explicit instruction.

## Deliverables

- [x] **ArchitectureTests** (4 tests): dependency-direction rules turning
      code-comment intentions into executable checks — `Domain` has no
      dependency on outer layers; `Application` has no dependency on
      `Infrastructure`/`Presentation`/`Shell`; `Presentation` has no
      dependency on `Domain`/`Infrastructure`/`Shell`; ViewModels have no
      dependency on `System.Windows.Threading`/`System.Windows.Controls`
      (the "testable without a running Dispatcher" goal from
      `docs/architecture/00-overview.md`).
- [x] **Application.Tests** (10 tests): `CustomerQueryService` and
      `DashboardQueryService` — full-field Domain→DTO mapping, empty-input
      handling, and every enum branch (`CustomerStatus` Lead/Active/
      Inactive, `TrendDirection` Up/Down/Flat) via hand-written stub
      repositories (`StubCustomerRepository`, `StubDashboardRepository`).
- [x] **Presentation.Tests** (13 tests): `CustomerPageViewModel` and
      `DashboardPageViewModel` — the `Loading→Loaded/Empty/Error` state
      machine (Loading proven via a `TaskCompletionSource`-backed stub so
      the transition is actually observed, not assumed), search/filter
      behavior and selection-reconciliation logic unique to
      `CustomerPageViewModel`, and `LoadCommand` recovering from an Error
      state. Hand-written stub query services
      (`StubCustomerQueryService`, `StubDashboardQueryService`).
- [x] **Domain.Tests** (4 tests): deliberately minimal — Domain currently
      has no behavior beyond data shapes and repository contracts, so this
      is value-equality smoke coverage documenting the contract
      `CustomerPageViewModel`'s reselection logic relies on, not padding
      for its own sake.
- [x] **Infrastructure.Tests** (5 tests): smoke coverage for
      `FakeCustomerRepository`/`FakeDashboardRepository` — non-empty
      results and `CancellationToken` honoring. Deliberately light; this
      project's real value starts once a live backend repository replaces
      the fakes.
- [x] `.editorconfig` fix: the existing `[*Tests.cs]` override only
      downgraded the naming-*style* rule, not `CA1707` (the actual
      analyzer enforcing PascalCase-only under `TreatWarningsAsErrors`) —
      without closing this gap, no test using the
      `MethodUnderTest_Scenario_ExpectedResult` convention
      `coding-standards.md` §7 itself mandates could build. Added
      `dotnet_diagnostic.CA1707.severity = none` under the existing
      section, with an inline comment explaining why (per
      `coding-standards.md` §6's suppression rule).
- [x] `docs/adr/0001-phase-09-naming-collision.md`: documents the Phase 09
      collision between Customer CRM and the Release-Engineering
      reservation in `branch-strategy.md`/`versioning.md`, and the
      decision to leave existing commits/filenames untouched while
      referring to Customer CRM as Phase 10 going forward.

## Risks

- **No CI yet.** This phase makes the test suite meaningful to run; it
  does not wire it into a pipeline that runs automatically on every PR —
  that's the `dotnet test` + branch-protection proposal from the approved
  planning pass, deliberately scoped out of Phase 08 itself pending the
  Phase 09/Release-Engineering numbering question being settled.
- **Domain and Infrastructure coverage is intentionally thin.** Both will
  need real expansion once Domain gains actual business rules and
  Infrastructure gains a real backend repository — tracked as a known gap,
  not an oversight.
- **No mocking library.** Hand-written stubs work cleanly today because
  every interface under test has 1–2 methods. If future modules introduce
  wider interfaces, hand-rolled stubs may start duplicating boilerplate
  across tests — revisit the "no new packages" constraint at that point
  rather than before.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` — 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` — 36/36 tests passed across all five
      projects (Domain.Tests 4, Application.Tests 10, Presentation.Tests
      13, ArchitectureTests 4, Infrastructure.Tests 5).
- [x] Every new test file follows the
      `MethodUnderTest_Scenario_ExpectedResult` naming convention and
      Arrange/Act/Assert-with-blank-line-separation from
      `coding-standards.md` §7.
- [x] No new NuGet packages added — every test project still references
      only what was already pinned in `Directory.Packages.props`.
- [x] Test project dependency boundaries respected: no test project
      references a layer beyond what its production counterpart is
      allowed to depend on (verified by inspection of each `.csproj`;
      the boundary itself is now also enforced at the `src` level by the
      new `ArchitectureTests`).

## Approval

Approved by: <pending> — <date>
