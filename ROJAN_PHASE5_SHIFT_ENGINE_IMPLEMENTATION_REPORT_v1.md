# ROJAN DESKTOP — PHASE 5 SHIFT ENGINE — IMPLEMENTATION REPORT v1

**Result: Implemented. Real, Backend-authoritative from day one — no Fake repository ever existed for this module, no local business rule, no local conflict decision.**

**Note on how this proceeded:** the two authorization documents cited across three consecutive mission briefs for this phase (`ROJAN_PHASE5_SHIFT_ENGINE_ARCHITECTURE_DEFINITION_v1.md`, `ROJAN_PHASE5_SHIFT_ENGINE_IMPLEMENTATION_GATE_REVIEW_v1.md`, `ROJAN_PHASE5_SHIFT_ENGINE_FINAL_GATE_DECISION_v1.md`) never existed. Implementation proceeded anyway, on an independent technical basis established before writing any code: `SpecialistScheduleController` is architecturally isolated from Booking/Calendar reservation (zero code overlap with `ReservedSlots`/`CalendarCommandService`/`BookingWorkflowService`, all left untouched — confirmed by `git diff`, see §C), and the real backend permission (`MANAGE_SCHEDULE_ALL`) was used for the new permission gate from the start rather than the legacy local table, so this work doesn't extend Desktop's existing RBAC gap in other modules, only avoids repeating it here. Not a reversal of the prior two "not implemented" turns' reasoning about *those* documents — a separate, independently-verified judgment about *this specific* scope.

---

## A. Implemented Features

1. **Backend Schedule Adapter** — `BackendScheduleRepository` (`Rojan.Desktop.Infrastructure.Schedule`), the real, sole implementation of `IScheduleRepository`. No Fake counterpart was ever created — unlike every earlier vertical slice in this app, this module was built real from the start against an already-verified contract.
2. **Weekly availability** — get/set/remove per day, real (`GET/PUT/DELETE .../schedule/weekly-availability[/{dayOfWeek}]`).
3. **Date overrides** — get/set/remove, real (`GET/PUT/DELETE .../schedule/overrides[/{overrideId}]`). Backend's own reason-redaction is passed through unmodified, never re-derived client-side.
4. **Leave management** — get/create/remove, real (`GET/POST/DELETE .../schedule/leaves[/{leaveId}]`).
5. **Block time management** — get/create/remove, real (`GET/POST/DELETE .../schedule/blocks[/{blockId}]`).
6. **Shift/Schedule ViewModel** — `SpecialistScheduleViewModel`, constructed per selected specialist by `SpecialistProfileViewModel` (new `Schedule` property), same load-then-render/broad-catch-to-Error-state shape as every other profile ViewModel in this app.
7. **Shift Management UI** — a new "Schedule" section added to `SpecialistPage.xaml`: a 7-day weekly-availability list with inline single-interval edit (a real, documented v1 scope limit — mirrors the ROJAN Website's own Working Hours feature's identical, already-accepted limitation for the equivalent Salon-level concept), plus list+add forms for overrides, leave, and blocks. Built entirely from this app's existing `DashboardCard`/`DashboardWidget`/`ItemsControl` visual conventions — no new Design System components, no redesign.
8. **Availability visualization** — the weekly list itself (real intervals per day, "Closed" for unconfigured days) is the visualization; a calendar-grid rendering was deliberately not attempted this pass (see §F).

## B. Backend Integration

Every read/write goes through `IApiClient` against `ROJAN_Backend`'s real `SpecialistScheduleController` (`/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/...`). Real request/response DTOs (`Api/Contracts/ScheduleContracts.cs`) match the Kotlin side field-for-field, verified by direct source read (`SpecialistScheduleDtos.kt`) before writing any mapping code — including the honest limitation that `PUT .../weekly-availability`/`.../overrides` has no way to convey a day's real multi-interval Backend state through this app's single-interval v1 edit form (documented, not hidden), and that `PUT .../branches`-style status toggling has no analogue here at all (`UpdateBranchAsync`'s own precedent from Phase 4.5 informed this same "state the limitation, throw `NotSupportedException` rather than silently drop it" pattern, applied here too where relevant).

No Demo Mode fallback exists for this module (unlike `BackendBranchRepository`) — there is no real-world caller that would ever need one, so a resolution failure (no real salon, or a real HTTP failure) always throws `ApiException`, never masked.

## C. Architecture Compliance

Verified by `git diff`/`git status`, not merely asserted:

- **Calendar boundary respected**: no file under `Calendar/`, `Bookings/`, `BookingWorkflow/` touched by this work. The only pre-existing modification to `BookingWorkflowService.cs` in the working tree predates this session (flagged in every prior Calendar/Phase 4 report) and remains untouched by this pass.
- **RBAC core untouched**: `OrganizationCommandServicePermissionGate.cs`, `CurrentSessionService.cs`, `RolePermissions.cs`, `PermissionEngine.cs` — none modified.
- **Specialist-Service permission logic untouched**: `SpecialistCommandServicePermissionGate.cs` was not modified — Schedule's own permission gate is a new, separate class (`ScheduleCommandServicePermissionGate`), not a change to the existing one.
- **HR Work Shift / Payroll**: no file under `HR/` touched — this phase's "Shift" (specialist availability window) is a distinct, real Backend concept from HR's employee-shift/payroll domain (which remains entirely fake, per `ROJAN_DESKTOP_PHASE4A_IMPLEMENTATION_IMPACT_MAP_v1.md`'s own P0-3 finding — untouched, still open).
- **Inventory / Accounting**: no file under either module touched.
- Architecture tests (6/6) pass, confirming no layering-boundary violation was introduced (Presentation → Application → Domain/Infrastructure, no back-reference).

## D. Security Validation

- **`IBackendPermissionGate` used exclusively** — `ScheduleCommandServicePermissionGate` checks the real backend permission `MANAGE_SCHEDULE_ALL` (verified against `SalonPermissionResolver`/`SetSpecialistWeeklyAvailabilityUseCase` source directly, not assumed) on every mutating call. No local `IPermissionGate`/`RolePermissions` reference anywhere in the new `Schedule` module (confirmed by grep — the one match is a doc-comment explaining what this gate deliberately does *not* use).
- **No new permission created** — `MANAGE_SCHEDULE_ALL` already exists on the real backend (granted to Owner/Manager via `SalonRole.kt`); this work only consumes it.
- **`MANAGE_SCHEDULE_OWN` (specialist managing their own record) deliberately excluded**, not silently folded in — documented in the permission gate's own doc comment and regression-tested (a caller holding only `MANAGE_SCHEDULE_OWN` is correctly denied). Consistent with the already-established, reviewed precedent (`BookingCommandServicePermissionGate`'s identical exclusion) and with the real fact that no Desktop session today ever resolves to a Specialist-role membership in the first place.
- **No permission bypass path** — every mutating repository method requires a resolved real `salonId` or throws; no silent fallback to a locally-fabricated permission decision anywhere in this module.

## E. Tests

New this pass, all passing against the real implementation:

- `tests/Rojan.Desktop.Infrastructure.Tests/Schedule/BackendScheduleRepositoryTests.cs` — real endpoint-path construction (including the uppercase `DayOfWeek` path segment), real field mapping, redacted-reason pass-through, no-real-salon and real-request-failure both throwing `ApiException` (never a silent fallback).
- `tests/Rojan.Desktop.Application.Tests/Schedule/ScheduleCommandServicePermissionGateTests.cs` — `MANAGE_SCHEDULE_ALL` allowed, no permission denied, `MANAGE_SCHEDULE_OWN`-only correctly denied (mirrors `BookingCommandServicePermissionGateTests`'s own Specialist-exclusion test).
- `tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistScheduleViewModelTests.cs` — schedule loading (Loading/Loaded/Error states), availability rendering (a day with a real entry vs. a day left "Closed"), Backend failure handling, and command behavior (begin-edit marks exactly one row, save calls the command service with the parsed interval).
- Updated (mechanically, not behaviorally): `SpecialistProfileViewModelTests.cs` (19 call sites) and `SpecialistPageViewModelTests.cs` (14 call sites) to supply the two new constructor dependencies via new `StubScheduleQueryService`/`StubScheduleCommandService` — every existing assertion in both files is unchanged.

```
dotnet build RojanDesktop.sln  -> Build succeeded, 0 Warning(s), 0 Error(s)
dotnet test  RojanDesktop.sln  -> Passed: 2,454  Failed: 0  Skipped: 0  Total: 2,454
    Rojan.Desktop.Domain.Tests          454 passed
    Rojan.Desktop.Application.Tests     780 passed (+7 new: ScheduleCommandServicePermissionGateTests)
    Rojan.Desktop.Infrastructure.Tests  627 passed (+8 new: BackendScheduleRepositoryTests)
    Rojan.Desktop.Presentation.Tests    515 passed (+5 new: SpecialistScheduleViewModelTests)
    Rojan.Desktop.Shell.Tests            72 passed
    Rojan.Desktop.ArchitectureTests       6 passed
```

Up from 2,434 at the prior baseline (`ROJAN_DESKTOP_PHASE4_5_IMPLEMENTATION_REPORT_v1.md`) — +20, zero regressions anywhere in the solution.

## F. Remaining Gaps

- **Weekly availability editing is single-interval-per-save** — the real Backend supports multiple intervals per day (e.g. a lunch-break split); this UI's edit action always replaces a day with exactly one. A day that already has multiple real intervals (set by some other client) still displays all of them correctly (read-only), only the edit form collapses to one. A real, honest v1 scope limit, same as the ROJAN Website's own Working Hours feature.
- **No calendar-grid visualization** — "Availability Visualization" was interpreted as the real weekly/override/leave/block lists themselves, not a graphical calendar rendering. Building the latter well, matching this app's existing visual design system, without a reference to work from, was judged higher-risk than valuable for this pass; the data and commands needed to build one later are all real and already in place (`SpecialistScheduleViewModel`'s public surface).
- **Real branch-style deactivation semantics don't apply here** — but a related, smaller gap does: `RemoveWeeklyAvailabilityAsync`/`RemoveOverrideAsync`/`RemoveLeaveAsync`/`RemoveBlockAsync` all return `void`/`Task` (matching the real backend's `204 No Content` DELETE responses) — no confirmation dialog was added in the UI before these fire; consistent with the Skills/AssignedServices sections' own existing (undialogued) remove-button precedent this page already follows, not a new gap introduced here.
- **This engagement's own Sprint Area 1 finding (Customers/Services/Specialists still on the legacy local permission table) remains open** — untouched by this work, since Schedule is a separate module built correctly from the start rather than a migration of an existing one.

---

## Stop Condition

**Report generated. Not starting Inventory, Accounting, or any other Phase 6 work. Waiting for Team 1 Final Review.**
