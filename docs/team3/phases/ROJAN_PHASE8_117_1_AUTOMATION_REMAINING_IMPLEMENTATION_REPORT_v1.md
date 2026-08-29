# ROJAN AI — TEAM 3 — PHASE 8.117.1 — P2 ERROR-SURFACE SANITIZATION — AUTOMATION REMAINING SITES — IMPLEMENTATION ADDENDUM v1

**Type:** Implementation addendum. Code + tests changed. **No commit performed** (STOP — awaiting the updated Sub-Wave 4 commit scope review).
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `b509054` (unchanged — nothing committed)
**Reference:** `ROJAN_PHASE8_115_P2_SUBWAVE4_SCOPE_AUDIT_v1.md`, `ROJAN_PHASE8_116_P2_SUBWAVE4_IMPLEMENTATION_REPORT_v1.md`, `ROJAN_PHASE8_117_P2_SUBWAVE4_COMMIT_SCOPE_REVIEW_v1.md`

---

## Purpose

Phase 8.116's STRICT SCOPE production file list authorised only `WorkflowsTabViewModel.cs` / `ScheduledJobsTabViewModel.cs` / `BusinessRulesTabViewModel.cs` (+ the no-op `AutomationPageViewModel.cs`), covering **10 of the 13** audited sub-wave-4 sites. The Phase 8.117 commit scope review confirmed the remaining **3 sites** — `ApprovalsTabViewModel.LoadAsync` / `.DecideAsync` and `AutomationDashboardTabViewModel.LoadAsync` — as a scope-restriction deferral, not a blocker, and recommended a short addendum. Phase 8.117.1 authorises exactly those 2 files. This addendum closes them, bringing sub-wave 4 to **13 / 13**.

---

## A. FILES CHANGED — 4 (2 prod + 2 test), all within STRICT SCOPE

```
 src/Rojan.Desktop.Presentation/ViewModels/Automation/ApprovalsTabViewModel.cs            | 4 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Automation/AutomationDashboardTabViewModel.cs  | 2 +-
 tests/Rojan.Desktop.Presentation.Tests/Automation/ApprovalsTabViewModelTests.cs          | 5 +++++
 tests/Rojan.Desktop.Presentation.Tests/Automation/AutomationDashboardTabViewModelTests.cs | 3 +++
 4 files changed, 12 insertions(+), 3 deletions(-)
```

**Not touched:** services, backend contracts, DI, `Strings.resx` / `.en` / `.ar`, Shell, navigation, other ViewModels, `AutomationPageViewModel.cs` (no `= exception.Message`, no failure boundary — nothing to sanitize). No new files, no new stubs.

**`using` additions:** none in production (both VMs use the fully-qualified `Localization.Strings.…` form, matching the Phase 8.39 / Wave F style in the same files). Both **test** files gained `+ using Rojan.Desktop.Presentation.Localization;` (line 3) so the new assertions can reference `Strings.Common_ActionFailedMessage` unqualified.

**Working tree for the full sub-wave 4** (Phase 8.116 + this addendum, all uncommitted on `b509054`): 5 prod + 5 test = 10 files.

---

## B. SITES SANITIZED — 3 (sub-wave 4 total now 13 / 13)

| # | VM · method | Surface | `State = Error` | `when` filter | `LogOperationFailed` |
|---|---|---|---|---|---|
| 11 | `ApprovalsTabViewModel.LoadAsync` | `ErrorMessage` | ✅ kept (`DashboardState.Error`) | ✅ `when (exception is not OperationCanceledException)` — byte-unchanged | ✅ `nameof(LoadAsync)` |
| 12 | `ApprovalsTabViewModel.DecideAsync` | `ErrorMessage` | n/a (success path ends `await LoadAsync()`) | ✅ unchanged | ✅ `nameof(DecideAsync)` |
| 13 | `AutomationDashboardTabViewModel.LoadAsync` | `ErrorMessage` | ✅ kept (`DashboardState.Error`) | ✅ unchanged | ✅ `nameof(LoadAsync)` |

Each: **only** the surface line changed — `ErrorMessage = exception.Message;` → `ErrorMessage = Localization.Strings.Common_ActionFailedMessage;`.

**Byte-unchanged everywhere:** the `catch (Exception exception) when (exception is not OperationCanceledException)` clause (the `when` predicate still references `exception`, so the variable is retained — no compiler warning; identical shape to the Phase 8.39 filtered catches already in these files), every `State = DashboardState.Error`, every `LogOperationFailed(nameof(<Method>))`, both `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Automation … operation failed. Operation={Operation}")]` signatures, the `await LoadAsync()` success-path reload in `DecideAsync`.

### Cancellation behaviour — unchanged

`OperationCanceledException` (and `TaskCanceledException`) is still excluded by the `when` clause → not caught → propagates as before. The filter predicate is byte-identical. No cancellation → the generic `ErrorMessage` + a single operation-name-only log line.

---

## C. SECURITY IMPACT

Both catches in each VM now assign the fixed localized constant. `exception` stays bound (the `when` clause needs it) but is **no longer referenced in the catch body** — `exception.Message` / `.ToString()` / `.InnerException` cannot reach the bound `ErrorMessage` TextBlock.

| Data class | Was reachable via | Now |
|---|---|---|
| **Approval decision comments** (free-text manager notes — can contain payroll figures, disciplinary detail, PII) | `ApprovalsTabViewModel.DecideAsync` | **not reachable** — test seeds `Secret = "approval-comment-SECRET-payroll"`, asserts `DoesNotContain(Secret, sut.ErrorMessage)` |
| **Approval request detail** (requester id, org·branch id, workflow-execution id, step approver names) | `ApprovalsTabViewModel.LoadAsync` / `.DecideAsync` | **not reachable** — generic constant |
| **Workflow names / dashboard aggregates** (workflow titles surfaced through the summary + recent-executions strip) | `AutomationDashboardTabViewModel.LoadAsync` | **not reachable** — test seeds `Secret = "workflow-name-SECRET-9f3"`, asserts `DoesNotContain(Secret, sut.ErrorMessage)` |
| Backend bodies / internal hosts / file paths / DB fragments | all 3 | **not reachable** — generic constant |

**Logs unchanged** — operation-name-only in all 3. The Phase 8.39 operation-name-only **log** no-leak assertions (`Contains("Operation=…", entry.Message)` + `DoesNotContain(Secret, entry.Message)`) are retained and still pass.

With this addendum, **every one of the 13 sub-wave-4 Automation error surfaces** now emits only `Strings.Common_ActionFailedMessage`. `grep -rn "= exception.Message" src/Rojan.Desktop.Presentation/ViewModels/Automation/` → **empty**.

---

## D. TEST CHANGES

**+0 net tests** (Presentation.Tests stays at **772**). Following the sub-wave-4 pattern: the no-leak contract was strengthened by adding assertions to the **existing Phase 8.39 failure tests**, not by adding new tests.

| File | Change |
|---|---|
| `ApprovalsTabViewModelTests` | `+ using …Localization;`. `Secret = "approval-comment-SECRET-payroll"`. Added `Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` + `Assert.DoesNotContain(Secret, sut.ErrorMessage ?? string.Empty, StringComparison.Ordinal)` to `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak` and `DecideAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak`, before the existing log assertions. |
| `AutomationDashboardTabViewModelTests` | `+ using …Localization;`. `Secret = "workflow-name-SECRET-9f3"`. Same 2-line assertion added to `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak`. |

No new test files, no stub changes. Every pre-existing Automation-tab test unchanged in intent and green.

---

## E. VALIDATION

| Gate | Expected | Actual (working tree = `b509054` + Phase 8.116 + this addendum) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **Build succeeded. 0 Warning(s) / 0 Error(s)** ✅ |
| Full test suite | 2,715+ | **2,715 / 2,715 PASS** ✅ |
| — Domain | 456 | 456 ✅ |
| — **Presentation** | 772 | **772** ✅ (assertions added to existing tests — no net-new) |
| — Application | 791 | 791 ✅ |
| — Infrastructure | 609 | 609 ✅ |
| — Shell | 80 | 80 ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Automation subset | — | **54 / 54 PASS** ✅ |

Suite progression: 2,715 (`b509054`) → **2,715** (P2 sub-wave 4 full — additive assertions, no net-new tests).

---

## F. COMMIT RECOMMENDATION

| Item | State |
|---|---|
| Scope | ✅ 4 files (2 prod + 2 test), all within Phase 8.117.1's STRICT SCOPE allowance |
| Base HEAD | `b509054` — unchanged; nothing staged |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,715 / 2,715; Architecture 7 / 7; Automation subset 54 / 54 |
| Sites | ✅ 3 / 3 this phase → **13 / 13 sub-wave 4** — only the `ErrorMessage =` line changed; `when` filter, `State = Error`, `LogOperationFailed`, `await LoadAsync()` reload byte-unchanged |
| Cancellation | ✅ `when (exception is not OperationCanceledException)` predicate byte-identical |
| Security | ✅ approval decision comments, approval/request identifiers, workflow names, and backend payloads structurally unreachable from every Automation surface; sentinel-enforced |
| Behaviour | ✅ unchanged — error-state recovery, filtered cancellation, `DecideAsync` reload preserved |
| Localization | ✅ no `.resx` change; no prod `using` additions (2 test-file `using` additions only) |
| DI / services / contracts / stubs | ✅ none |
| Deferred | **none** — sub-wave 4 is now complete at 13 / 13 |
| Line endings | tool-edited files may show LF/CRLF `git diff` warnings; `core.autocrlf=true` normalises to LF in the committed blob — cosmetic only |
| Proposed combined commit (Phase 8.116 + 8.117.1) subject | `fix(desktop): sanitize automation tab error surfacing` |
| Proposed staged files | the 10 sub-wave-4 files (5 prod + 5 test) — **no `git add -A` / `git add .`** |

### Separate from Missing-Guard work

This changes the *message string* in *pre-existing* filtered catches. No new guard, no behaviour change, no filter change. The Missing-Guard Sweep (`794648e` … `0260bc3`) is complete and untouched.

---

## STOP

Phase 8.117.1 addendum complete. Base HEAD `b509054` unchanged (no commit). Build 0/0, **2,715 / 2,715** tests pass, Architecture 7/7, Automation subset 54/54.

**3 remaining sub-wave-4 sites sanitized** — `ApprovalsTabViewModel` (`LoadAsync` / `DecideAsync`), `AutomationDashboardTabViewModel` (`LoadAsync`). Only the surface line changed — `ErrorMessage = exception.Message;` → `ErrorMessage = Localization.Strings.Common_ActionFailedMessage;`. The `catch … when (exception is not OperationCanceledException)` clause, every `State = Error`, and every operation-name-only log call are byte-unchanged; no `using` (prod) / `.resx` / DI / service / contract / stub change. **Approval decision comments, approval identifiers, workflow names, and backend payloads no longer reach any UI surface.** +0 net tests (4 no-leak assertions added to existing Phase 8.39 tests).

**Sub-wave 4 is now complete: 13 / 13 Automation error surfaces sanitized.**

**Awaiting the updated Sub-Wave 4 Commit Scope Review.**
