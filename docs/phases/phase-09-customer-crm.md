# Phase 09 — Enterprise Customer CRM

> **Naming note:** this document's filename and this heading are kept
> exactly as originally committed (see `docs/standards/branch-strategy.md`
> git-workflow rules on not renaming things after the fact). "Phase 09" is
> reserved for Release Engineering per `branch-strategy.md`/`versioning.md`;
> this module collides with that reservation and is referred to as
> **Phase 10** everywhere else going forward. See
> `docs/adr/0001-phase-09-naming-collision.md` for the full context and
> decision.

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Ship the first real business module on top of the module-shell/navigation
architecture built in Phase 07, using the exact same vertical-slice pattern
established by the Dashboard module in Phase 06A/06B: a Domain entity +
repository abstraction, an Application query service that maps Domain
types to Application-owned DTOs, a fake/in-memory Infrastructure
repository (no backend integration yet, matching Dashboard's own scope),
and a Presentation module/ViewModel/View wired into the existing
`IModule`/`IModuleRegistry`/`NavigationService` machinery. Read-only for
this phase — no create/edit/delete use case exists yet, the same
sequencing Dashboard followed (read-only first, writes later).

Note on numbering: `docs/standards/coding-standards.md` §7 already
reserves "Phase 08" for the Testing Strategy deliverable. This phase is
numbered 09 to avoid colliding with that existing reservation, per an
explicit decision made when the collision was surfaced.

## Deliverables

- [x] `Domain/Customers`: `Customer` record, `CustomerStatus` enum,
      `ICustomerRepository` contract.
- [x] `Application/Customers`: `CustomerDto`, Application-owned
      `CustomerStatus`, `ICustomerQueryService` / `CustomerQueryService`
      (Domain → DTO mapping), registered in
      `Application/DependencyInjection/ServiceCollectionExtensions.cs`.
- [x] `Infrastructure/Customers`: `FakeCustomerRepository` with static
      sample data (six customers spanning Lead/Active/Inactive), 400ms
      artificial delay so the Loading state is observable, registered in
      `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`.
- [x] `Presentation`: `CustomerModule` (`IModule`), `CustomerPageViewModel`
      (search/filter + master-detail selection, reusing
      `ViewModels.Dashboard.DashboardState` for its Loading/Loaded/Empty/
      Error states rather than a duplicate enum), `CustomerPage` view
      (search box, state-aware customer list via the existing
      `DashboardWidget`, detail panel via the existing `DashboardCard` and
      `KPIValue` — no new Design System components needed), registered in
      `Presentation/DependencyInjection/ServiceCollectionExtensions.cs`
      and `Themes/Views.xaml`.
- [x] `Shell/App.xaml.cs`: `"customers"` sidebar entry swapped from
      `PlaceholderModule` to `CustomerModule` — the one-line swap the
      Phase 07 module system was explicitly designed to allow.

## Risks

- **Fake data only.** No backend/API integration exists yet — same
  accepted risk Dashboard carried through Phase 06B. Swapping
  `FakeCustomerRepository` for a real implementation of
  `ICustomerRepository` is the only change needed later; nothing above
  Infrastructure is aware the data is fake.
- **Read-only scope.** No CQRS command pattern exists anywhere in this
  codebase yet (Dashboard never needed one either) — the first write use
  case (e.g. "update customer status") will need to establish that
  pattern from scratch rather than follow precedent.
- **No automated tests.** Per `coding-standards.md` §7, the testing
  strategy itself is Phase 08's deliverable; the test projects in
  `tests/` are still empty scaffolds solution-wide, not a gap specific to
  this phase.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` — 0 warnings, 0 errors
      (`TreatWarningsAsErrors=true` enforced).
- [x] `dotnet test RojanDesktop.sln` — no failures (no test cases exist
      yet in any project, including `Rojan.Desktop.ArchitectureTests`;
      nothing to regress).
- [x] Dependency direction preserved: `Domain/Customers` has zero
      references to `Application`/`Infrastructure`/`Presentation`;
      `Application/Customers` depends only on `Domain`; `Infrastructure`
      implements the Domain-defined interface; `Presentation` depends
      only on `Application` (never reaches into `Domain` directly, same
      rule Dashboard follows).
- [x] Sidebar "Customers" entry keeps its existing id/glyph/order
      (`"customers"`, `"◈"`, `10`) — only the registered `IModule`
      implementation changed, so navigation ordering is unaffected.

## Approval

Approved by: <pending> — <date>
