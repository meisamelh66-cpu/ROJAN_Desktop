# ROJAN AI — TEAM 3 — PHASE 8.116 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 4 (AUTOMATION TABS) — IMPLEMENTATION v1

**Type:** Implementation. Code + tests changed. **No commit performed** (STOP — Phase 8.117 is the commit scope review).
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `b509054` (unchanged — nothing committed)
**Reference:** `ROJAN_PHASE8_115_P2_SUBWAVE4_SCOPE_AUDIT_v1.md`

---

## A. FILES CHANGED — 6 (3 prod + 3 test), all within STRICT SCOPE

```
 src/Rojan.Desktop.Presentation/ViewModels/Automation/WorkflowsTabViewModel.cs      | 10 +++++-----
 src/Rojan.Desktop.Presentation/ViewModels/Automation/ScheduledJobsTabViewModel.cs  |  6 +++---
 src/Rojan.Desktop.Presentation/ViewModels/Automation/BusinessRulesTabViewModel.cs  |  4 ++--
 tests/Rojan.Desktop.Presentation.Tests/Automation/WorkflowsTabViewModelTests.cs    | 13 +++++++++++++
 tests/Rojan.Desktop.Presentation.Tests/Automation/ScheduledJobsTabViewModelTests.cs |  6 ++++++
 tests/Rojan.Desktop.Presentation.Tests/Automation/BusinessRulesTabViewModelTests.cs |  4 ++++
 6 files changed, 33 insertions(+), 10 deletions(-)
```

**Not touched:** services, backend contracts, DI, `Strings.resx` / `.en` / `.ar`, Shell, navigation, other ViewModels. No new files, no new stubs, **no `using` additions** (all 3 prod VMs and all 3 test files already reference the localization namespace — the prod VMs via the fully-qualified `Localization.Strings.…` form from Wave F).

### Scope note — 3 audited sites deferred by this phase's file list

Phase 8.115 §B scoped **13** sites / 5 tab VMs. Phase 8.116's STRICT SCOPE production list is `AutomationPageViewModel.cs` (**no `= exception.Message` — nothing to do**), `WorkflowsTabViewModel.cs`, `ScheduledJobsTabViewModel.cs`, `BusinessRulesTabViewModel.cs` — it **omits `ApprovalsTabViewModel.cs` and `AutomationDashboardTabViewModel.cs`**. So **10 of the 13 sites** are sanitized here; the 3 remaining (`ApprovalsTabViewModel.LoadAsync` / `.DecideAsync`, `AutomationDashboardTabViewModel.LoadAsync`) are **deferred** — their existing Phase 8.39 tests (`State == Error` + operation-name-only log) remain green, unchanged. Recommend folding them into a short sub-wave-4 addendum or into sub-wave 6.

---

## B. SITES SANITIZED — 10

| # | VM · method | Surface | `State = Error` | `when` filter | `LogOperationFailed` |
|---|---|---|---|---|---|
| 1 | `WorkflowsTabViewModel.LoadAsync` | `ErrorMessage` | ✅ kept | ✅ `when (exception is not OperationCanceledException)` — byte-unchanged | ✅ `nameof(LoadAsync)` |
| 2 | `WorkflowsTabViewModel.CreateDraftAsync` | `ErrorMessage` | n/a | ✅ unchanged | ✅ `nameof(CreateDraftAsync)` |
| 3 | `WorkflowsTabViewModel.PublishAsync` | `ErrorMessage` | n/a | ✅ unchanged | ✅ `nameof(PublishAsync)` |
| 4 | `WorkflowsTabViewModel.RunNowAsync` | `ErrorMessage` | n/a | ✅ unchanged | ✅ `nameof(RunNowAsync)` |
| 5 | `WorkflowsTabViewModel.RollbackAsync` | `ErrorMessage` | n/a | ✅ unchanged | ✅ `nameof(RollbackAsync)` |
| 6 | `ScheduledJobsTabViewModel.LoadAsync` | `ErrorMessage` | ✅ kept | ✅ unchanged | ✅ `nameof(LoadAsync)` |
| 7 | `ScheduledJobsTabViewModel.CreateAsync` | `ErrorMessage` | n/a | ✅ unchanged | ✅ `nameof(CreateAsync)` |
| 8 | `ScheduledJobsTabViewModel.RunNowAsync` | `ErrorMessage` | n/a | ✅ unchanged | ✅ `nameof(RunNowAsync)` |
| 9 | `BusinessRulesTabViewModel.LoadAsync` | `ErrorMessage` | ✅ kept | ✅ unchanged | ✅ `nameof(LoadAsync)` |
| 10 | `BusinessRulesTabViewModel.CreateAsync` | `ErrorMessage` | n/a | ✅ unchanged | ✅ `nameof(CreateAsync)` |

Each: **only** the surface line changed — `ErrorMessage = exception.Message;` → `ErrorMessage = Localization.Strings.Common_ActionFailedMessage;`.

**Byte-unchanged everywhere:** the `catch (Exception exception) when (exception is not OperationCanceledException)` clause (the `when` predicate still references `exception`, so the variable is retained — no compiler warning, and this is the exact shape already present for the Wave F guards in the same files), every `State = DashboardState.Error`, every `LogOperationFailed(nameof(<Method>))`, every `[LoggerMessage]` signature, the `await LoadAsync()` success-path reload on the command sites.

### Cancellation behaviour — unchanged

`OperationCanceledException` (and `TaskCanceledException : OperationCanceledException`) is still excluded by the `when` clause → not caught → propagates as before. No cancellation → the generic `ErrorMessage`; no cancellation → a log line. The filter predicate is byte-identical.

---

## C. SECURITY IMPACT

Every one of the 10 catches now assigns the fixed localized constant. The `exception` variable is still bound (the `when` clause needs it) **but is no longer referenced in the catch body** — `exception.Message` / `.ToString()` / `.InnerException` cannot reach the surface.

| Data class | Was reachable via | Now |
|---|---|---|
| **Workflow definitions** (step names, descriptions, trigger config) | `WorkflowsTabViewModel` `CreateDraftAsync` / `PublishAsync` / `RollbackAsync` | **not reachable** — tests seed `Secret = "workflow-definition-SECRET-vip"` and now assert `DoesNotContain(Secret, sut.ErrorMessage)` |
| **Workflow / job execution detail** | `WorkflowsTabViewModel.RunNowAsync`, `ScheduledJobsTabViewModel.RunNowAsync` | **not reachable** — seeded `Secret` asserted absent from `sut.ErrorMessage` |
| **Cron expressions** | `ScheduledJobsTabViewModel.CreateAsync` | **not reachable** — test seeds `Secret = "cron-0-9-star-star-1-SECRET"`, now asserts `DoesNotContain(Secret, sut.ErrorMessage)` |
| **Business-rule conditions / actions** (field/operator/value, discount %, target workflow id) | `BusinessRulesTabViewModel.CreateAsync` | **not reachable** — test seeds `Secret = "IF-Customer-is-VIP-SECRET"`, now asserts `DoesNotContain(Secret, sut.ErrorMessage)` |
| Triggers / internal configuration / org·branch·user ids | all 10 | **not reachable** — generic constant |
| Backend bodies / internal hosts / file paths / DB fragments | all 10 | **not reachable** — generic constant |

**Logs unchanged** — operation-name-only in all 10. The Phase 8.39 operation-name-only **log** no-leak assertions (`AssertSingleErrorFor` / `DoesNotContain(Secret, entry.Message)`) are retained and still pass in every one of the 3 test files.

---

## D. TESTS

**+0 net tests** (Presentation.Tests stays at **772**). The audit's recommendation was followed: instead of new tests, the **13 assertions were added to the existing Phase 8.39 failure tests** so the strengthened contract is locked in without churn.

| File | Change |
|---|---|
| `WorkflowsTabViewModelTests` | `+ private static void AssertGenericSurfaceNoLeak(WorkflowsTabViewModel sut)` helper (`Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` + `Assert.DoesNotContain(Secret, sut.ErrorMessage ?? "")`), called from the 5 existing tests (`LoadAsync_Failure_…`, `CreateDraftAsync_Failure_…`, `PublishAsync_Failure_…`, `RunNowAsync_Failure_…`, `RollbackAsync_Failure_…`) right after `AssertSingleErrorFor(...)`. |
| `ScheduledJobsTabViewModelTests` | `+ Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` + `+ Assert.DoesNotContain(Secret, sut.ErrorMessage ?? "")` in the 3 existing tests (`LoadAsync_Failure_…`, `CreateAsync_Failure_…`, `RunNowAsync_Failure_…`), before the existing log assertions. |
| `BusinessRulesTabViewModelTests` | same, in the 2 existing tests (`LoadAsync_Failure_…`, `CreateAsync_Failure_…`). |

**No test file needed `+ using …Localization;`** (all 3 already have it). No new test files, no stub changes. Every pre-existing Automation-tab test unchanged in intent and green.

**Subset run:** Automation namespace → **54 / 54 PASS**.

---

## E. VALIDATION

| Gate | Expected | Actual (working tree = `b509054` + this change) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full test suite | ~2,715–2,718 | **2,715 / 2,715 PASS** ✅ |
| — Domain | 456 | 456 |
| — **Presentation** | 772 | **772** (assertions added to existing tests — no net-new) |
| — Application | 791 | 791 |
| — Infrastructure | 609 | 609 |
| — Shell | 80 | 80 |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Automation subset | 54 / 54 | **54 / 54 PASS** ✅ |

Suite progression: 2,715 (`b509054`) → **2,715** (P2 sub-wave 4 — additive assertions, no net-new tests).

---

## F. COMMIT RECOMMENDATION

| Item | State |
|---|---|
| Scope | ✅ 6 files (3 prod + 3 test), all within the STRICT SCOPE allowance |
| Base HEAD | `b509054` — unchanged; nothing staged |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,715 / 2,715; Architecture 7 / 7; subset 54 / 54 |
| Sites | ✅ 10 / 10 (this phase's file list) — **only** the `ErrorMessage =` line changed; the `when` filter, `State = Error`, `LogOperationFailed`, and the success-path reload are byte-unchanged |
| Cancellation | ✅ `when (exception is not OperationCanceledException)` predicate byte-identical — cancellation still propagates, no generic error, no log noise |
| Security | ✅ workflow definitions, cron expressions, business-rule conditions/actions, triggers, and backend payloads structurally unreachable from every surface; sentinel-enforced |
| Behaviour | ✅ unchanged — error-state recovery, filtered cancellation, `await LoadAsync()` reload all preserved |
| Localization | ✅ no `.resx` change; no `using` additions |
| DI / services / contracts / stubs | ✅ none |
| Deferred | `ApprovalsTabViewModel` (2 sites) + `AutomationDashboardTabViewModel` (1 site) — outside this phase's authorised file list; documented follow-up |
| Line endings | working-copy files edited via the tool may show LF/CRLF `git diff` warnings; `core.autocrlf=true` normalises to LF in the committed blob (repo-consistent) — cosmetic only |
| Proposed commit subject | `fix(desktop): sanitize automation tab error surfacing` |
| Proposed staged files | the 6 above — **no `git add -A` / `git add .`** |

### Separate from Missing-Guard work

This changes the *message string* in *pre-existing* filtered catches. No new guard, no behaviour, no filter change. The Missing-Guard Sweep (`794648e` … `0260bc3`) is complete and untouched.

---

## STOP

Phase 8.116 implementation complete. Base HEAD `b509054` unchanged (no commit). Build 0/0, **2,715 / 2,715** tests pass, Architecture 7/7, Automation subset 54/54.
**10 of the 13 audited sub-wave-4 sites sanitized** — `WorkflowsTabViewModel` (`LoadAsync` / `CreateDraftAsync` / `PublishAsync` / `RunNowAsync` / `RollbackAsync`), `ScheduledJobsTabViewModel` (`LoadAsync` / `CreateAsync` / `RunNowAsync`), `BusinessRulesTabViewModel` (`LoadAsync` / `CreateAsync`). **Only** the surface line changed — `ErrorMessage = exception.Message;` → `ErrorMessage = Localization.Strings.Common_ActionFailedMessage;`. The `catch (Exception exception) when (exception is not OperationCanceledException)` clause, every `State = Error`, and every operation-name-only log call are byte-unchanged; no `using` / `.resx` / DI / service / contract / stub change. **Workflow definitions, cron expressions, business-rule conditions/actions, and backend payloads no longer reach any UI surface.** +0 net tests (13 no-leak assertions added to the existing Phase 8.39 tests). `ApprovalsTabViewModel` (2 sites) + `AutomationDashboardTabViewModel` (1 site) were outside this phase's file list — deferred.

**Awaiting Phase 8.117 — Sub-Wave 4 Commit Scope Review.**
