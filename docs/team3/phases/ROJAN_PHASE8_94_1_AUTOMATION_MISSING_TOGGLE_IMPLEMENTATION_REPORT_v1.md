# ROJAN AI — TEAM 3 — PHASE 8.94.1 — WAVE F (AUTOMATION TABS) — IMPLEMENTATION CORRECTION v1

**Type:** Implementation correction (closes the one audited member deferred by Phase 8.94's method list). Code + tests changed. **No commit performed.**
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `4b1afca` (unchanged — Wave F is still uncommitted; Phase 8.95 reviews 8.94 + 8.94.1 together)
**Reference:** `ROJAN_PHASE8_94_AUTOMATION_IMPLEMENTATION_REPORT_v1.md` §A scope note
**Objective:** Guard `ScheduledJobsTabViewModel.ToggleEnabledAsync` — the last unguarded user-triggered Automation command.

---

## A. FILES CHANGED (this correction)

```
 src/Rojan.Desktop.Presentation/ViewModels/Automation/ScheduledJobsTabViewModel.cs   | +12  (ToggleEnabledAsync body wrapped)
 tests/Rojan.Desktop.Presentation.Tests/Automation/StubAutomationServices.cs         | +7   (StubScheduledJobService.SetEnabledException seam)
 tests/Rojan.Desktop.Presentation.Tests/Automation/ScheduledJobsTabViewModelTests.cs | +41  (2 new [Fact])
```

**3 files — exactly the STRICT SCOPE allowance.** `WorkflowsTabViewModel`, `BusinessRulesTabViewModel`, `AutomationPageViewModel`, `AutomationDashboardTabViewModel`, `ApprovalsTabViewModel`, service contracts, DI, RBAC, auth, navigation, shared localization — untouched.

### Combined Wave F working tree (8.94 + 8.94.1, all uncommitted on base `4b1afca`)

```
 src/.../Automation/BusinessRulesTabViewModel.cs        | 24 +++++-
 src/.../Automation/ScheduledJobsTabViewModel.cs        | 24 ++++++--   (DeleteAsync from 8.94 + ToggleEnabledAsync from 8.94.1)
 src/.../Automation/WorkflowsTabViewModel.cs            | 48 +++++++++---
 tests/.../Automation/BusinessRulesTabViewModelTests.cs | 45 +++++++++++
 tests/.../Automation/ScheduledJobsTabViewModelTests.cs | 64 ++++++++++++++++
 tests/.../Automation/StubAutomationServices.cs         | 48 +++++++++++-
 tests/.../Automation/WorkflowsTabViewModelTests.cs     | 87 ++++++++++++++++++
```

---

## B. GUARD IMPLEMENTED

```csharp
private async Task ToggleEnabledAsync(ScheduledJobDto job)
{
    try
    {
        await _scheduledJobService.SetEnabledAsync(job.Id, !job.IsEnabled).ConfigureAwait(true);   // UNCHANGED
        await LoadAsync().ConfigureAwait(true);                                                     // UNCHANGED — clears ErrorMessage on success
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
        ErrorMessage = Localization.Strings.Common_ActionFailedMessage;
        LogOperationFailed(nameof(ToggleEnabledAsync));
    }
}
```

Byte-identical in shape to the file's other guards (`LoadAsync`, `CreateAsync`, `RunNowAsync`, and the `DeleteAsync` added in 8.94):

| Rule | Compliance |
|---|---|
| No `exception.Message` | ✅ — fixed constant `Strings.Common_ActionFailedMessage` |
| No payload logging | ✅ — operation-name-only `[LoggerMessage] LogOperationFailed("ToggleEnabledAsync")`; `exception` never passed |
| No `State = DashboardState.Error` | ✅ — a failed toggle does not blank the tab |
| No `ActionErrorMessage` introduction | ✅ — reuses the existing `ErrorMessage` property; no new field, no ctor/DI change, no XAML change |
| Reuse existing `ILogger` + instance `[LoggerMessage]` | ✅ — no `SYSLIB1020` (build 0/0) |

**Automation user-triggered command coverage is now complete — 19/19 guarded.**

---

## C. CANCELLATION

- Filter `catch (Exception exception) when (exception is not OperationCanceledException)` — `OperationCanceledException` / `TaskCanceledException` propagate uncaught, exactly as for the file's existing guards.
- No cancellation → `ErrorMessage`. No cancellation → log entry (`LogOperationFailed` is inside the filtered body).
- No `CancellationToken` is threaded by `ToggleEnabledAsync` today; the filter is the defensive Phase 8.39 convention (the service method accepts a token).
- Command-level (`async void` via `AsyncRelayCommand`) OCE is not unit-tested — an unfiltered exception there is raised on the runner's sync-context and would abort the run rather than be catchable, matching the existing suite. Behavioural coverage of the filtered shape is provided by `WorkflowsTabViewModelTests.SelectingAWorkflow_VersionHistoryCancellation_StaysSilent_NoErrorNoLog` (Phase 8.94).

---

## D. STATE PRESERVATION

| Check | Result |
|---|---|
| Selected job unchanged on failure | ✅ — `SelectedJob` is never touched by `ToggleEnabledAsync`; the throw happens at `SetEnabledAsync` before any state mutation |
| Toggle state not corrupted | ✅ — the stub flips `IsEnabled` only on the success path; on failure `Jobs[0].IsEnabled` is unchanged (test-asserted `Assert.True(sut.Jobs[0].IsEnabled)`) |
| Existing reload behaviour preserved | ✅ — `await LoadAsync()` stays inside the `try`; on success it runs and clears `ErrorMessage` (test-asserted); on failure it is correctly skipped |

---

## E. TESTS

**+2 tests** (`Presentation.Tests` 756 → 758; Automation namespace 52 → 54).

| Test | Asserts |
|---|---|
| `ToggleEnabledCommand_Failure_ShowsGenericError_PreservesJobState_LogsOperationOnly` | `SetEnabledException = new InvalidOperationException(Secret)` → command does not throw · `ErrorMessage == Strings.Common_ActionFailedMessage` · `Jobs[0].IsEnabled` still `true` · **single** `Error` log entry, `Operation=ToggleEnabledAsync`, `DoesNotContain(Secret)` |
| `ToggleEnabledCommand_SuccessAfterFailure_ClearsError` | fail once → `ErrorMessage` set · clear the seam · toggle again → `ErrorMessage == null` (via the follow-on `LoadAsync`) · `Jobs[0].IsEnabled == false` |

`Secret = "cron-0-9-star-star-1-SECRET"` (file constant). Pre-existing `ToggleEnabledCommand_FlipsIsEnabled` happy-path test unchanged and green.

Checklist: failure does not throw ✅ · cancellation remains silent ✅ (structural + 8.94 behavioural) · `ErrorMessage` appears ✅ · success clears error ✅ · operation-only logging ✅ · no secret payload leakage ✅.

---

## F. VALIDATION

| Gate | Expected | Actual |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full test suite | ~2,701 | **2,701 / 2,701 PASS** ✅ |
| — Domain | — | 456 |
| — Presentation | +2 → 758 | **758** ✅ |
| — Application | 791 | 791 |
| — Infrastructure | 609 | 609 |
| — Shell | 80 | 80 |
| — ArchitectureTests | 7 / 7 | **7 / 7** ✅ |
| Automation subset | — | **54 / 54** ✅ |

Suite progression: 2,691 (`4b1afca`) → 2,699 (Phase 8.94) → **2,701** (Phase 8.94.1, +2).

---

## G. COMMIT READINESS

| Item | State |
|---|---|
| Scope | ✅ 3 files, within the STRICT SCOPE allowance |
| Base HEAD | `4b1afca` — unchanged; nothing committed |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,701 / 2,701; Architecture 7 / 7 |
| Pattern fidelity | ✅ filtered catch, reuse `ErrorMessage` + existing `[LoggerMessage]`, generic string, no `State = Error`, no `ActionErrorMessage`, no ctor/DI/contract change |
| Security | ✅ sentinel-enforced — no exception message / payload to log or UI |
| Stub seam | ✅ 1 additive `Exception?` (`SetEnabledException`), null-path byte-identical |
| Line endings | working-copy CRLF files edited; `core.autocrlf=true` normalises to LF in the committed blob (repo-consistent) — cosmetic only |
| Open items | **none** — Automation command guard coverage is now complete (19/19). The pre-existing `= exception.Message` load-error surfacings across the tab VMs remain the untouched "sanitize load-error surfacing" P2. |
| Proposed commit subject (Wave F, covering 8.94 + 8.94.1) | `fix(desktop): guard remaining automation tab command failures` |
| Proposed staged files | the 7 modified Automation files — **no `git add -A` / `git add .`** |

---

## STOP

Phase 8.94.1 correction complete. Base HEAD `4b1afca` unchanged (no commit). Build 0/0, **2,701 / 2,701** tests pass, Architecture 7/7.
`ScheduledJobsTabViewModel.ToggleEnabledAsync` is now guarded with the filtered-cancellation shape, generic error surface, and operation-name-only logging — closing the last unguarded user-triggered command in the Automation domain. Wave F now guards **7 methods** across the 3 tab VMs (`WorkflowsTabViewModel` ×3, `ScheduledJobsTabViewModel` ×2, `BusinessRulesTabViewModel` ×2).

**Awaiting Phase 8.95 — Wave F Commit Scope Review.**
