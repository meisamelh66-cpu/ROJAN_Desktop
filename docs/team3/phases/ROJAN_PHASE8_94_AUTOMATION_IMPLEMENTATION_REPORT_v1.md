# ROJAN AI — TEAM 3 — PHASE 8.94 — MISSING-GUARD SWEEP — WAVE F (AUTOMATION TABS) — IMPLEMENTATION v1

**Type:** Implementation. Code + tests changed. **No commit performed** (STOP before commit — Phase 8.95 is the commit scope review).
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `4b1afca` (unchanged — nothing committed)
**Reference:** `ROJAN_PHASE8_93_AUTOMATION_TABS_SCOPE_AUDIT_v1.md`
**Objective:** Guard the remaining Automation tab command failures while preserving the Phase 8.39 filtered-cancellation pattern.

---

## A. FILES CHANGED

```
 src/Rojan.Desktop.Presentation/ViewModels/Automation/BusinessRulesTabViewModel.cs   | 24 +++++-
 src/Rojan.Desktop.Presentation/ViewModels/Automation/ScheduledJobsTabViewModel.cs   | 12 ++-
 src/Rojan.Desktop.Presentation/ViewModels/Automation/WorkflowsTabViewModel.cs       | 48 ++++++++---
 tests/Rojan.Desktop.Presentation.Tests/Automation/BusinessRulesTabViewModelTests.cs | 45 ++++++++++
 tests/Rojan.Desktop.Presentation.Tests/Automation/ScheduledJobsTabViewModelTests.cs | 23 ++++++
 tests/Rojan.Desktop.Presentation.Tests/Automation/StubAutomationServices.cs         | 41 ++++++++-
 tests/Rojan.Desktop.Presentation.Tests/Automation/WorkflowsTabViewModelTests.cs     | 87 +++++++++++++++++++
 7 files changed, 262 insertions(+), 18 deletions(-)
```

**3 production, 1 stub, 3 test — exactly the STRICT SCOPE allowance.** No other file touched. `AutomationPageViewModel`, `AutomationDashboardTabViewModel`, `ApprovalsTabViewModel`, service contracts, DI, RBAC, auth, navigation, localization `.resx` / `Strings.cs` — all untouched.

### Per-file detail

| File | Change |
|---|---|
| `WorkflowsTabViewModel.cs` | `ArchiveAsync`, `DeleteAsync`, `LoadVersionHistoryAsync` bodies wrapped in the filtered `try` / `catch`. `LoadVersionHistoryAsync` additionally sets `ErrorMessage = null` on the success path (it has no follow-on `LoadAsync` to clear it). No new field, no ctor change, no `using` added (`Localization.Strings` resolves via the parent namespace). |
| `ScheduledJobsTabViewModel.cs` | `DeleteAsync` body wrapped in the filtered `try` / `catch`. |
| `BusinessRulesTabViewModel.cs` | `ToggleEnabledAsync`, `DeleteAsync` bodies wrapped in the filtered `try` / `catch`. |
| `StubAutomationServices.cs` | +7 additive `Exception?` seams, all null-path byte-identical: `StubWorkflowService.GetVersionsException` / `.ArchiveException` / `.DeleteException`; `StubScheduledJobService.DeleteException`; `StubBusinessRuleService.SetEnabledException` / `.DeleteException`. (One extra — `GetVersionsException` — beyond the 6 guard methods, needed for the version-history path.) |
| 3 test files | `+ using Rojan.Desktop.Presentation.Localization;` + 8 new `[Fact]`s. Every pre-existing test unchanged. |

### Scope note — one audited member deferred by this phase

Phase 8.93 §B.2 identified **7** unguarded members. Phase 8.94's GUARD METHODS list authorises **6** — it omits **`ScheduledJobsTabViewModel.ToggleEnabledAsync`** (lines 165–169: `await _scheduledJobService.SetEnabledAsync(job.Id, !job.IsEnabled); await LoadAsync();`). Per STRICT SCOPE that method was **left unguarded** in this phase. It is the last unguarded user-triggered command in the Automation domain and should be closed in a short follow-up (or folded into Phase 8.95's review if re-authorised). Its sibling `BusinessRulesTabViewModel.ToggleEnabledAsync` **is** guarded here, so the two enable/disable toggles are now inconsistent until that follow-up lands.

---

## B. GUARD COVERAGE

| VM | Method | Before | After |
|---|---|---|---|
| `WorkflowsTabViewModel` | `ArchiveAsync` | unguarded → generic crash dialog | filtered `try`/`catch` → `ErrorMessage = Strings.Common_ActionFailedMessage` + `LogOperationFailed("ArchiveAsync")` |
| `WorkflowsTabViewModel` | `DeleteAsync` | unguarded | filtered `try`/`catch` → generic error + `LogOperationFailed("DeleteAsync")` |
| `WorkflowsTabViewModel` | `LoadVersionHistoryAsync` (fire-and-forget from `SelectedWorkflow` setter) | unguarded → unobserved task exception → crash dialog | filtered `try`/`catch` → generic error + `LogOperationFailed("LoadVersionHistoryAsync")`; `ErrorMessage = null` on success |
| `ScheduledJobsTabViewModel` | `DeleteAsync` | unguarded | filtered `try`/`catch` → generic error + `LogOperationFailed("DeleteAsync")` |
| `BusinessRulesTabViewModel` | `ToggleEnabledAsync` | unguarded | filtered `try`/`catch` → generic error + `LogOperationFailed("ToggleEnabledAsync")` |
| `BusinessRulesTabViewModel` | `DeleteAsync` | unguarded | filtered `try`/`catch` → generic error + `LogOperationFailed("DeleteAsync")` |

**Guarded this phase: 6/6 authorised.** Automation user-triggered command coverage after this phase: **~18/19** (only `ScheduledJobsTabViewModel.ToggleEnabledAsync` remains — see §A scope note).

### Guard shape (identical to the tabs' 10 pre-existing Phase 8.39 guards, minus the leak)

```csharp
private async Task ArchiveAsync(WorkflowDefinitionDto workflow)
{
    try
    {
        await _workflowService.ArchiveAsync(workflow.Id).ConfigureAwait(true);   // UNCHANGED
        await LoadAsync().ConfigureAwait(true);                                   // UNCHANGED — clears ErrorMessage on success
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
        ErrorMessage = Localization.Strings.Common_ActionFailedMessage;
        LogOperationFailed(nameof(ArchiveAsync));
    }
}
```

- **Reuses the existing `ErrorMessage` property** — no new `ActionErrorMessage` / `HasActionError` pair (the tab VMs already surface command errors through `ErrorMessage`; a second surface would be inconsistent). No new bindable member → **no XAML change**.
- **No `State = DashboardState.Error`** — a failed archive/delete/toggle does not blank the tab (matches the existing command guards; only `LoadAsync` sets `State`).
- `LoadVersionHistoryAsync` keeps its `VersionHistory.Clear()` + early `return` for the null-selection case inside the `try`; the `ErrorMessage = null` clear sits after the `foreach` so it runs only on a genuine successful load.

---

## C. CANCELLATION HANDLING

All 6 new guards use the exact Phase 8.39 filter:

```csharp
catch (Exception exception) when (exception is not OperationCanceledException)
```

| Requirement | Result |
|---|---|
| User cancellation stays silent | ✅ — `OperationCanceledException` (and `TaskCanceledException : OperationCanceledException`) is excluded by the `when` filter; the guard body never runs for it. |
| `OperationCanceledException` must not become `ErrorMessage` | ✅ — filter excludes it before `ErrorMessage` is assigned. |
| No cancellation logging noise | ✅ — `LogOperationFailed` is inside the filtered body; a cancelled operation logs nothing. |
| Cancellation behaviour preserved | ✅ — an excluded `OperationCanceledException` propagates exactly as it does today for the 10 existing filtered guards in these same files. No tab method threads a `CancellationToken` currently, so the filter is defensive (the `_service` methods all accept one); it is not a behaviour change. |

**Test coverage of the cancellation path:** verified directly for `LoadVersionHistoryAsync` via `SelectingAWorkflow_VersionHistoryCancellation_StaysSilent_NoErrorNoLog` — seeding `GetVersionsException = new OperationCanceledException()` and asserting the setter does not throw, `SelectedWorkflow` is preserved, `ErrorMessage` stays `null`, and `logger.Entries` is empty. The command-triggered methods (`ArchiveAsync` / `DeleteAsync` / `ToggleEnabledAsync`) are driven through `AsyncRelayCommand.Execute` (`async void`); an *un*-filtered exception there is raised on xUnit's async sync-context and would fail the run rather than be catchable by `Record.Exception`, so those methods are covered by (a) the byte-identical filter shared with the 10 existing guards and (b) the non-cancellation failure tests proving the guard swallows everything else. This matches the existing suite, which likewise does not unit-test the command-level `when` clause.

---

## D. SECURITY

Automation payloads in scope: workflow definitions (step graphs, names, descriptions), business-rule conditions (`field`/`operator`/`value`) and action parameters (discount %, target workflow id), scheduling data (cron expressions, frequencies), org/branch/user ids.

| Vector | Result |
|---|---|
| Exception message / body → log | **not reachable** — each guard calls the VM's operation-name-only `[LoggerMessage] LogOperationFailed(string operation)`; the caught `exception` is never passed. `{Operation}` is a compile-time `nameof(...)` string. |
| Rule / workflow / job payload → log | **not reachable** — no DTO field is read into the log call. |
| Exception message → UI | **prevented** — the new guards assign the fixed constant `Strings.Common_ActionFailedMessage`, never `exception.Message`. This is **stricter** than the 10 pre-existing guards in these files, which still do `ErrorMessage = exception.Message` (the "sanitize load-error surfacing" P2 — unchanged here, Category C). |
| Partial version-history exposure | **prevented** — on `LoadVersionHistoryAsync` failure `VersionHistory` has already been `.Clear()`ed and no items are added; the user sees an empty list + the generic error, never a partially-populated history. |

Test-enforced: every new failure test seeds a unique `Secret` sentinel into the thrown exception (`"workflow-definition-SECRET-vip"`, `"cron-0-9-star-star-1-SECRET"`, `"IF-Customer-is-VIP-SECRET"`) and asserts `Assert.DoesNotContain(Secret, entry.Message)` **and** `ErrorMessage == Strings.Common_ActionFailedMessage`.

---

## E. LOGGING

| Check | Result |
|---|---|
| `[LoggerMessage]` reused | ✅ — each VM's existing instance-form `[LoggerMessage(EventId = 1, Level = Error, Message = "Automation <area> operation failed. Operation={Operation}")]` — no new declaration, no signature change. |
| `ILogger` field | ✅ — single `ILogger<TSelf>` per VM, unchanged. |
| `ILoggerFactory` / DI / ctor | ✅ none — no constructor parameter added, no DI registration touched. `AutomationPageViewModel` already forwards `ILogger<TChild>?` to each tab (Phase 8.39). |
| `SYSLIB1020` | ✅ not triggered — one `ILogger` + instance-form `[LoggerMessage]` (build is 0/0). |
| Operation values emitted | `ArchiveAsync`, `DeleteAsync` (Workflows), `LoadVersionHistoryAsync`, `DeleteAsync` (ScheduledJobs), `ToggleEnabledAsync`, `DeleteAsync` (BusinessRules) — all `nameof(...)`. |

---

## F. TESTS

**+8 tests** (`Presentation.Tests` 748 → 756). Pre-existing Automation tests: all 44 unchanged and green (52 total in the Automation namespace now).

| File | New `[Fact]` | Asserts |
|---|---|---|
| `WorkflowsTabViewModelTests` | `ArchiveCommand_Failure_ShowsGenericError_PreservesWorkflow_LogsOperationOnly` | no throw · `ErrorMessage == Common_ActionFailedMessage` · workflow still in `Workflows` · single `Error` log, `Operation=ArchiveAsync`, no `Secret` |
| | `DeleteCommand_Failure_ShowsGenericError_PreservesWorkflow_LogsOperationOnly` | same, `Operation=DeleteAsync` |
| | `SelectingAWorkflow_VersionHistoryFailure_ShowsGenericError_PreservesSelection_LogsOperationOnly` | setter no throw · `SelectedWorkflow` preserved · `VersionHistory` empty · generic `ErrorMessage` · single log `Operation=LoadVersionHistoryAsync`, no `Secret` |
| | `SelectingAWorkflow_VersionHistoryCancellation_StaysSilent_NoErrorNoLog` | `OperationCanceledException` seam → setter no throw · selection preserved · `ErrorMessage` null · **zero** log entries |
| | `SelectingAWorkflow_VersionHistorySuccess_ClearsPriorError` | prior `ArchiveAsync` failure sets `ErrorMessage` → selecting a workflow clears it to `null` · `VersionHistory` populated |
| `ScheduledJobsTabViewModelTests` | `DeleteCommand_Failure_ShowsGenericError_PreservesJob_LogsOperationOnly` | no throw · generic `ErrorMessage` · job still in `Jobs` · single log `Operation=DeleteAsync`, no `Secret` |
| `BusinessRulesTabViewModelTests` | `ToggleEnabledCommand_Failure_ShowsGenericError_PreservesRuleState_LogsOperationOnly` | no throw · generic `ErrorMessage` · `Rules[0].IsEnabled` unchanged (`true`) · single log `Operation=ToggleEnabledAsync`, no `Secret` |
| | `DeleteCommand_Failure_ShowsGenericError_PreservesRule_LogsOperationOnly` | no throw · generic `ErrorMessage` · rule still in `Rules` · single log `Operation=DeleteAsync`, no `Secret` |

Coverage of the TASK checklist: failure-does-not-throw ✅ · cancellation-remains-silent ✅ (`LoadVersionHistoryAsync`; see §C for the command-level rationale) · `ErrorMessage` appears ✅ · success clears error ✅ (`LoadVersionHistoryAsync`; `Archive`/`Delete`/`Toggle` clear via the follow-on `LoadAsync`) · selection/state preservation ✅ · operation-only logging ✅ · no workflow/rule/job payload leakage ✅.

> The Phase 8.93 estimate was ~13 tests; the delivered set is 8 — the per-method failure + state assertions were consolidated into single tests each rather than split, and no command-level cancellation tests were added (unsafe under `async void`; see §C).

---

## G. VALIDATION

| Gate | Expected | Actual |
|---|---|---|
| `dotnet build -c Debug` | 0 warn / 0 err | **0 Warning(s) / 0 Error(s)** ✅ |
| Full test suite | all pass, ~2,699–2,704 | **2,699 / 2,699 PASS** ✅ |
| — Domain.Tests | — | 456 |
| — Presentation.Tests | +8 → 756 | **756** ✅ |
| — Application.Tests | 791 | 791 |
| — Infrastructure.Tests | 609 | 609 |
| — Shell.Tests | 80 | 80 |
| — ArchitectureTests | 7 / 7 | **7 / 7** ✅ |
| Automation-namespace subset | all pass | **52 / 52** ✅ |

Suite progression: 2,691 (`4b1afca`) → **2,699** (+8, Missing-Guard Sweep Wave F / Automation tabs).

---

## H. COMMIT READINESS

| Item | State |
|---|---|
| Scope | ✅ 7 files, all within the STRICT SCOPE allowance (3 prod + 1 stub + 3 test) |
| Base HEAD | `4b1afca` — unchanged; nothing committed |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,699 / 2,699; Architecture 7 / 7 |
| Pattern fidelity | ✅ filtered `catch … when (exception is not OperationCanceledException)`, reuses existing `ErrorMessage` + existing `[LoggerMessage]`, generic `Common_ActionFailedMessage`, no `State = Error`, no ctor/DI/contract change |
| Security | ✅ no exception message / payload reaches log or UI; sentinel-enforced |
| Stub seams | ✅ 7 additive `Exception?`, null-path byte-identical |
| Line endings | new `using` lines + method bodies written on files that are CRLF in the working copy; `core.autocrlf=true` normalises to LF in the committed blob (repo-consistent) — cosmetic only, build/tests unaffected (same as phases 8.78 / 8.86) |
| Open item | `ScheduledJobsTabViewModel.ToggleEnabledAsync` still unguarded — deferred by this phase's method list; flag for a follow-up micro-phase |
| Proposed commit subject | `fix(desktop): guard remaining automation tab command failures` |
| Proposed staged files | the 7 modified files above — **no `git add -A` / `git add .`** |

---

## STOP

Phase 8.94 implementation complete. Base HEAD `4b1afca` unchanged (no commit). Build 0/0, **2,699 / 2,699** tests pass, Architecture 7/7.
6 authorised methods guarded across `WorkflowsTabViewModel` (`ArchiveAsync`, `DeleteAsync`, `LoadVersionHistoryAsync`), `ScheduledJobsTabViewModel` (`DeleteAsync`), `BusinessRulesTabViewModel` (`ToggleEnabledAsync`, `DeleteAsync`) — filtered-cancellation shape preserved, generic error surface, operation-name-only logging, no state/DI/contract change. One audited member (`ScheduledJobsTabViewModel.ToggleEnabledAsync`) was outside this phase's method list and remains unguarded.

**Awaiting Phase 8.95 — Wave F Commit Scope Review.**
