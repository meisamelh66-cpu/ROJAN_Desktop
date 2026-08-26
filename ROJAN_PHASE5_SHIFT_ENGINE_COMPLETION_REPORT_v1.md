# ROJAN DESKTOP — PHASE 5 SHIFT ENGINE COMPLETION & HARDENING REPORT v1

**No commit. No push. No merge. No amend. All changes are new, uncommitted working-tree changes on top of `92052c7`.**

---

## A. Final Implemented Scope

Unchanged from the committed baseline: weekly availability, one-off date overrides, leave, and ad-hoc blocks per specialist, real and Backend-authoritative end to end (`SpecialistScheduleController`), through a repository → permission-gated command service → ViewModel → UI chain matching every other real module in this app. This pass added no new capability — it hardened the existing one after a fresh, assumption-free audit of the actual committed source.

## B. Hardening Changes

Fresh audit (Phase 1) found two real, verifiable defects, not present in the previous completion report's own claims because they weren't tested for at the time:

**1. Mutation commands had no error handling.** `SaveDayAvailabilityAsync`, `ClearDayAvailabilityAsync`, `AddOverrideAsync`, `RemoveOverrideAsync`, `AddLeaveAsync`, `RemoveLeaveAsync`, `AddBlockAsync`, `RemoveBlockAsync` all called their command-service method directly, with no try/catch — only `LoadAsync` had one. Traced the consequence directly: `AsyncRelayCommand.Execute` is `async void` with no exception handling beyond a `finally` that resets its re-entrancy guard. A permission denial (`UnauthorizedOperationException`, thrown client-side by `ScheduleCommandServicePermissionGate` before any Backend call) or a real Backend failure (`ApiException`) during any mutation — not the initial page load — would propagate as an unhandled exception, not the graceful `DashboardState.Error` this app's own convention provides. **Fix:** wrapped all 8 mutation methods in the same try/catch → `ErrorMessage`/`State = DashboardState.Error` pattern `LoadAsync` already used — no new pattern invented, just applied consistently.

**2. `LoadAsync` never used the app's own dedicated Empty state.** `DashboardState.Empty` exists with its own real visual treatment (`DashboardWidget`/`Themes/DashboardComponents.xaml`) specifically for "genuinely nothing here yet" — every other real module in this app has access to it, but `LoadAsync` always set `Loaded` on success regardless of whether a specialist had anything configured at all. This is distinct from "specialist without availability" (a real, already-correct per-day "Closed" label) — Phase 3's own requirement separates them as two different states for a reason: a specialist can have zero weekly availability but a real block/leave/override on file, which is not the same as a genuinely blank schedule. **Fix:** `LoadAsync` now checks, after populating everything, whether every day is unconfigured *and* there are zero overrides/leaves/blocks — only then does it set `Empty`; any real content anywhere keeps it `Loaded`.

Both fixes are scoped entirely to `SpecialistScheduleViewModel.cs` — no other file was touched to produce them.

## C. Backend Integration Verification

Re-verified `BackendScheduleRepository.cs` directly against source, not assumed from the prior report:
- **Endpoints**: all 12 methods target `/api/v1/salons/{salonId}/specialists/{specialistId}/schedule/...`, matching `SpecialistScheduleController` exactly (weekly-availability GET/PUT/DELETE, overrides GET/PUT/DELETE, leaves GET/POST/DELETE, blocks GET/POST/DELETE).
- **HTTP methods**: correct per verb — reads are GET, `SetWeeklyAvailabilityAsync`/`SetOverrideAsync` are PUT (idempotent set-by-key, matching the real endpoint shape), `CreateLeaveAsync`/`CreateBlockAsync` are POST (real creates), every remove is DELETE.
- **Request/response DTOs**: match `SpecialistScheduleDtos.kt` field-for-field, confirmed at implementation time and unchanged since.
- **Permission behavior**: `ScheduleCommandServicePermissionGate` checks real backend `MANAGE_SCHEDULE_ALL` via `IBackendPermissionGate`, not the legacy local table — re-confirmed by direct read, unchanged.
- **Error propagation**: every method throws `ApiException` with a real status code and message on failure — none swallow or mask an error.
- **No fake fallback data**: confirmed — `ResolveSalonIdAsync` throws rather than substituting anything when no real salon resolves; there is no Fake implementation of this module anywhere in the codebase to fall back to even if one wanted to.
- **No local schedule authority**: confirmed — the repository computes nothing; every value returned is a direct mapping of what Backend sent.

No defect found in this layer. No change made here.

## D. UI State Verification

| Required state | Status | Notes |
|---|---|---|
| 1. Loading | ✓ Already correct | `DashboardWidget State="{Binding State}"`, set at the start of `LoadAsync`. |
| 2. Success | ✓ Already correct | Real data rendered per section, no fabricated values anywhere (confirmed by direct XAML/binding read). |
| 3. Empty schedule | ✓ Fixed this pass | See §B.2. |
| 4. Specialist without availability | ✓ Already correct | Real per-day "تعطیل" (Closed) label when `Availability` is null, confirmed distinct from the new Empty state (§B.2's second test verifies the two don't get conflated). |
| 5. Backend error | ✓ Fixed this pass (for mutations) | Load-time errors were already correct; mutation-time errors were not, until §B.1. |
| 6. Permission denied | ✓ Fixed this pass (for mutations) | Same fix as #5 — a denial surfaces via the identical `DashboardState.Error` treatment as any other error, distinguished only by message text. This matches every other module's own convention in this app (no module has a visually distinct "permission denied" state); introducing one only for Schedule would be a new pattern, not a fix, so none was added. |

Also verified: no fabricated schedule values, no local availability calculation, real values throughout (re-confirmed against §C). No hardcoded color or literal English/Persian string found in the Schedule XAML section — every visual uses `{StaticResource Rojan.*}` and every string goes through `{x:Static loc:Strings.*}`, consistent with the existing Design System. No UI file was modified this pass — the two defects were both ViewModel-level, not XAML-level.

## E. Security Verification

Unchanged from the committed baseline, re-confirmed: real `IBackendPermissionGate`/`MANAGE_SCHEDULE_ALL`, no local permission table, no new permission invented, no bypass path. The permission-denial fix in §B.1 makes the *existing* real enforcement visible to the user correctly — it does not change what is or isn't allowed.

## F. Files Changed (this hardening pass only)

```
src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistScheduleViewModel.cs   (121 insertions, 12 deletions)
tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistScheduleViewModelTests.cs (103 insertions, 9 deletions)
```
Confirmed via `git diff --stat` scoped to exactly these two paths — nothing else in the working tree was touched by this pass (verified against the full `git status` output, which still shows only the same pre-existing, unrelated files this session has documented in every prior report).

## G. Exact Validation Results

```
Build:  PASS (0 Warning(s), 0 Error(s))

Full Test Suite (fresh run, this turn):
    Rojan.Desktop.Domain.Tests            Total: 454   Passed: 454   Failed: 0   Skipped: 0
    Rojan.Desktop.Application.Tests       Total: 780   Passed: 780   Failed: 0   Skipped: 0
    Rojan.Desktop.Infrastructure.Tests    Total: 627   Passed: 627   Failed: 0   Skipped: 0
    Rojan.Desktop.Presentation.Tests      Total: 520   Passed: 520   Failed: 0   Skipped: 0
    Rojan.Desktop.Shell.Tests             Total: 72    Passed: 72    Failed: 0   Skipped: 0
    Rojan.Desktop.ArchitectureTests       Total: 6     Passed: 6     Failed: 0   Skipped: 0

    Solution total: 2,459 passed, 0 failed, 0 skipped.
    (Up from 2,454 at the committed baseline — +5, exactly the five new tests added this pass.)

Architecture Tests: PASS (6/6)
```
Not carried forward from any prior report — this is a fresh execution, this turn, against the current working tree including this pass's own changes.

New tests added this pass (5): `SaveDayAvailabilityCommand_BackendFailure_SetsErrorStateInsteadOfThrowing`, `SaveDayAvailabilityCommand_PermissionDenied_SetsErrorStateInsteadOfThrowing`, `RemoveBlockCommand_BackendFailure_SetsErrorStateInsteadOfThrowing`, `Constructor_NothingConfiguredAtAll_StateIsEmpty`, `Constructor_NoWeeklyAvailabilityButHasABlock_StateIsLoadedNotEmpty`.

## H. Production Readiness Decision

## READY

| Criterion | Status |
|---|---|
| Backend authority preserved | ✓ |
| No fake data | ✓ |
| No local schedule authority | ✓ |
| Error handling complete | ✓ (fixed this pass) |
| Loading state complete | ✓ |
| Empty state complete | ✓ (fixed this pass) |
| Permission handling complete | ✓ (fixed this pass) |
| Required tests passing | ✓ 2,459/2,459 |
| Architecture tests passing | ✓ 6/6 |
| Build passing | ✓ |

Every criterion in this phase's own list is met, verified fresh, not assumed.

## I. Remaining Limitations (documented, not blockers)

- **Single-interval-per-save weekly availability editing** — a real, deliberate v1 scope limit, unchanged from the committed baseline, matching the ROJAN Website's own identical, already-accepted limitation for the equivalent Salon-level feature.
- **Mutations show no distinct "saving..." indicator during their own network call** — the widget stays in its current visual state until the post-mutation reload flashes through Loading again. This matches every other profile/page ViewModel's own established convention in this app (Skills, Service-Assignment, etc. behave identically) — not a Schedule-specific gap, and not changed here to avoid introducing a new UI pattern inconsistent with the rest of the app.
- **No real weekly-availability deactivation path beyond "remove entirely"** — unchanged from the committed baseline; the real Backend has no `active` toggle on this endpoint (matches the identical, already-documented limitation `BackendSpecialistRepository.UpdateSpecialistAsync` and `BackendBranchRepository.UpdateBranchAsync` both carry for their own status fields).
- **HR's real financial-calculation gap (unrelated module)** remains open — not touched, not in scope for this pass, already tracked separately in `ROJAN_DESKTOP_PHASE4A_IMPLEMENTATION_IMPACT_MAP_v1.md`.

---

## Architecture Safety Confirmation

Verified via `git status`/`git diff --stat` before and after this pass: **no file under `Bookings/`, `Calendar/`, `BookingWorkflow/`, Authentication, or RBAC core was touched.** No file under `Inventory/`, `Accounting/`, `HR/`, or `Reporting/` was touched. The only files modified are the two listed in §F. No unrelated change was discovered or absorbed.

---

## Stop Condition

**Implementation/hardening complete. Fresh validation complete. Report complete. Waiting for next phase assignment.**

**Summary for the record:**
- **What was actually changed**: `SpecialistScheduleViewModel.cs` (2 real defects fixed: missing mutation error handling, missing Empty-state usage) + its test file (5 new tests).
- **Exact files changed**: 2 (listed in full in §F).
- **Exact test numbers**: 2,459 total, 2,459 passed, 0 failed, 0 skipped.
- **Build result**: PASS.
- **Production readiness decision**: READY.
