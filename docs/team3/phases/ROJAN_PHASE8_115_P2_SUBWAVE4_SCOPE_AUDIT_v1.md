# ROJAN AI — TEAM 3 — PHASE 8.115 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 4 (AUTOMATION TABS) — SCOPE AUDIT v1

**Type:** AUDIT ONLY. No source / test / localization / service / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `b509054` (`fix(desktop): sanitize organization specialists and services error surfacing`)
**Reference:** `ROJAN_PHASE8_102_SANITIZE_ERROR_SURFACE_SCOPE_AUDIT_v1.md` §F sub-wave 4, `ROJAN_PHASE8_114_P2_SUBWAVE3_COMMIT_REPORT_v1.md`
**Recommendation: ship all 13 sites (5 tab VMs) in ONE commit. LOWEST-risk sub-wave — no `catch`-clause change (the `when` filter keeps the `exception` variable); a single string swap per site. 2 test-file `using` additions; no new tests strictly required.**

---

## A. GIT STATE

```
git rev-parse HEAD        → b5090549dac02b1d20de2bd4f211e3d4b27098a8
git branch --show-current → feature/team3-desktop-completion
git status (tracked)      → clean
git diff --cached         → (empty)
```

Untracked: only `ROJAN_*.md`. Baseline (checkpoint §E, `b509054`): **2,715 / 2,715** — Domain 456, Presentation 772, Application 791, Infrastructure 609, Shell 80, Architecture 7. Build 0/0.

---

## B. INVENTORY — 13 sites / 5 tab ViewModels

`AutomationPageViewModel` (the parent orchestrator) has **no `= exception.Message`** — not a target. `AutomationDashboardTabViewModel` and `ApprovalsTabViewModel` **are** targets here (they were "clean" of *unguarded* commands for Wave F, but their pre-existing filtered guards still surface `exception.Message`).

### B.0 The pattern — all 13 are the **Phase 8.39 filtered shape**

```csharp
catch (Exception exception) when (exception is not OperationCanceledException)
{
    ErrorMessage = exception.Message;          // ← the leak
    State = DashboardState.Error;              // LoadAsync sites only
    LogOperationFailed(nameof(<Method>));      // already operation-name-only
}
```

**The `when (exception is not OperationCanceledException)` clause references `exception`, so the `catch` variable CANNOT be dropped** (unlike sub-waves 1–3). The fix is the minimal one — swap `ErrorMessage = exception.Message` → `ErrorMessage = Localization.Strings.Common_ActionFailedMessage`, and **nothing else** (keep the filter, `State = Error`, `LogOperationFailed`). This is exactly the shape Wave F (`ROJAN_PHASE8_94_*`) used for the *newly-guarded* commands in these same 3 files.

### B.1 Full inventory

| # | File · line | Method | Surface | `State = Error`? | Notes |
|---|---|---|---|---|---|
| 1 | `Automation/WorkflowsTabViewModel.cs:144` | `LoadAsync` | `ErrorMessage` | ✅ | + `State` |
| 2 | `Automation/WorkflowsTabViewModel.cs:190` | `CreateDraftAsync` | `ErrorMessage` | ❌ (ends `await LoadAsync()` on success) | — |
| 3 | `Automation/WorkflowsTabViewModel.cs:215` | `PublishAsync` | `ErrorMessage` | ❌ | — |
| 4 | `Automation/WorkflowsTabViewModel.cs:256` | `RunNowAsync` | `ErrorMessage` | ❌ | `_executionEngine.ExecuteAsync(...)` — workflow execution |
| 5 | `Automation/WorkflowsTabViewModel.cs:270` | `RollbackAsync` | `ErrorMessage` | ❌ | — |
| 6 | `Automation/ScheduledJobsTabViewModel.cs:135` | `LoadAsync` | `ErrorMessage` | ✅ | — |
| 7 | `Automation/ScheduledJobsTabViewModel.cs:160` | `CreateAsync` | `ErrorMessage` | ❌ | cron expression in the request |
| 8 | `Automation/ScheduledJobsTabViewModel.cs:202` | `RunNowAsync` | `ErrorMessage` | ❌ | `_scheduledJobService.RunDueJobAsync(...)` |
| 9 | `Automation/BusinessRulesTabViewModel.cs:145` | `LoadAsync` | `ErrorMessage` | ✅ | — |
| 10 | `Automation/BusinessRulesTabViewModel.cs:175` | `CreateAsync` | `ErrorMessage` | ❌ | rule conditions + action params in the request |
| 11 | `Automation/ApprovalsTabViewModel.cs:79` | `LoadAsync` | `ErrorMessage` | ✅ | — |
| 12 | `Automation/ApprovalsTabViewModel.cs:96` | `DecideAsync` | `ErrorMessage` | ❌ | approve/reject a pending request |
| 13 | `Automation/AutomationDashboardTabViewModel.cs:122` | `LoadAsync` | `ErrorMessage` | ✅ | aggregate summary |

- **`Strings` reference style:** `WorkflowsTabViewModel` / `ScheduledJobsTabViewModel` / `BusinessRulesTabViewModel` already use `Localization.Strings.Common_ActionFailedMessage` (Wave F) — **no `using`**. `ApprovalsTabViewModel` / `AutomationDashboardTabViewModel` have no `Strings` reference → impl uses the **same fully-qualified `Localization.Strings.…` form** (consistent, **no `using` addition in any prod file**).
- **Logger:** each of the 5 VMs has a single `ILogger<TSelf>` + instance-form `[LoggerMessage(EventId = 1, Level = Error, Message = "Automation <workflows|scheduled jobs|business rules|approvals|dashboard> operation failed. Operation={Operation}")]`. Untouched.

### B.2 Not in scope — already correct

The **Wave F** command guards in `WorkflowsTabViewModel` (`ArchiveAsync` / `DeleteAsync` / `LoadVersionHistoryAsync`), `ScheduledJobsTabViewModel` (`DeleteAsync` / `ToggleEnabledAsync`), `BusinessRulesTabViewModel` (`ToggleEnabledAsync` / `DeleteAsync`) → already `= Localization.Strings.Common_ActionFailedMessage` (`ROJAN_PHASE8_94_*` / `8.94.1`). Do **not** touch. `LoginViewModel` / `MobileOtpLoginViewModel` (typed catches).

---

## C. CLASSIFICATION

| Category | Members | Action |
|---|---|---|
| **A — sensitive user-visible leak** | **all 13 sites** — filtered `catch (Exception exception) when (…)` → `ErrorMessage = exception.Message` bound to a `TextBlock` | **sanitize — the sub-wave-4 work** |
| **B — already sanitized** | the 7 Wave F command guards in these 3 files → `Common_ActionFailedMessage` | **do not touch** |
| **C — intentional technical message** | none |
| **D — out of scope** | sub-waves 5–6; `AutomationPageViewModel` (no `= exception.Message`) |

All 13 Category-A sites are the identical shape and the identical (minimal) fix.

---

## D. SECURITY

| Exposure class | Where reachable | Concrete |
|---|---|---|
| **Workflow definitions** (step graphs, step names, descriptions, trigger config) | `WorkflowsTabViewModel` `CreateDraftAsync` / `PublishAsync` / `RollbackAsync` — a `WorkflowRules` / backend validation error can quote a step name or a definition fragment |
| **Workflow execution detail** | `WorkflowsTabViewModel.RunNowAsync`, `ScheduledJobsTabViewModel.RunNowAsync` — `_executionEngine.ExecuteAsync` / `RunDueJobAsync` failure can carry a step-failure message or a fact value |
| **Automation / business rules** (`BusinessRuleConditionDto` field/operator/value, `BusinessRuleActionDto` params — discount %, target workflow id) | `BusinessRulesTabViewModel.CreateAsync` — a rule-validation error can quote a condition or an action parameter |
| **Cron expressions** (`ScheduledJobDto.CronExpression`) | `ScheduledJobsTabViewModel.CreateAsync` — a schedule-parse error can echo the raw cron string |
| **Business / customer triggers** | trigger-subscription config in a workflow error |
| **Approval content** (`ApprovalRequestDto` title / description, requester id) | `ApprovalsTabViewModel` `LoadAsync` / `DecideAsync` |
| **Internal configuration / identifiers** | org id / branch id / user id scope every one of the 13 service calls |
| Backend bodies / internal hosts / file paths / DB fragments | all 13 (`HttpRequestException`, EF text echoed in a 500) | generic infra leak |

**Answer to the SECURITY REVIEW task:** yes — `exception.Message` at these 13 sites can expose workflow definitions, automation rules, cron expressions, business triggers, internal configuration, and backend payloads.

### Sanitization pattern (the Wave F shape — filter preserved)

```csharp
// before
catch (Exception exception) when (exception is not OperationCanceledException)
{
    ErrorMessage = exception.Message;
    State = DashboardState.Error;              // LoadAsync sites — UNCHANGED
    LogOperationFailed(nameof(LoadAsync));     // UNCHANGED
}

// after
catch (Exception exception) when (exception is not OperationCanceledException)   // filter + variable KEPT (the when clause needs it)
{
    ErrorMessage = Localization.Strings.Common_ActionFailedMessage;
    State = DashboardState.Error;              // UNCHANGED
    LogOperationFailed(nameof(LoadAsync));     // UNCHANGED
}
```

- The `exception` variable stays (it is used by the `when` clause) but is no longer referenced in the body — no compiler warning, and this is the exact shape already present in each file for the Wave F guards.
- `OperationCanceledException` still propagates uncaught (filter unchanged) — no cancellation → generic error, no cancellation log noise.
- No `State` is added or removed.

### Logs — unchanged

All 13 keep `LogOperationFailed(nameof(<Method>))`. `[LoggerMessage]` templates byte-unchanged. The Phase 8.39 operation-name-only **log** no-leak assertions (`AssertSingleErrorFor` / `DoesNotContain(Secret, entry.Message)` in every one of the 5 test files) are retained and still pass.

---

## E. ARCHITECTURE

| Question | Answer |
|---|---|
| Existing `[LoggerMessage]` availability | **Yes — no logging change.** All 5 tab VMs have an `ILogger<TSelf>` + operation-name-only instance-form `[LoggerMessage]` invoked in the same catch. `AutomationPageViewModel` already forwards `ILogger<TChild>?` to each tab (Phase 8.39). Untouched. |
| Existing localization usage | `Strings.Common_ActionFailedMessage` ships (all 3 `.resx`, Wave A). 3 of 5 prod VMs already reference `Localization.Strings.…` (Wave F). `ApprovalsTabViewModel` / `AutomationDashboardTabViewModel` do not, but the impl uses the same fully-qualified form → **no `using` addition in any prod file, no `.resx` change.** |
| Test impact | **Minimal — no test edit is strictly required.** The existing Phase 8.39 failure tests for all 13 sites assert only `State == Error` and the operation-name-only **log** (`DoesNotContain(Secret, entry.Message)`) — never `sut.ErrorMessage` — so they pass unchanged after the swap. **Recommended hardening:** add `Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` + `Assert.DoesNotContain(Secret, sut.ErrorMessage)` to each of the 13 existing tests (~13 assertion *additions*, not flips). `+ using Rojan.Desktop.Presentation.Localization;` needed in `ApprovalsTabViewModelTests` + `AutomationDashboardTabViewModelTests` (the other 3 already have it). Net new tests ≈ 0. |
| Stub impact | **None** — every failure path uses a pre-existing `Exception?` seam on `StubAutomationServices.cs` (`GetAllException` / `CreateDraftException` / `PublishException` / `RollbackException` / `GetSummaryException` / `DecideException` / `RunDueJobException` / …, all from Phase 8.39 + Wave F). |
| DI impact | **None.** |
| Service / contract impact | **None.** |
| `SYSLIB1020` / partial | Not relevant — no `[LoggerMessage]` touched; classes already `partial`. |

---

## F. RECOMMENDATION — WAVE SIZE

### Ship all 13 sites in ONE commit.

| Metric | Value |
|---|---|
| Sites | **13** |
| ViewModels | **5** (`WorkflowsTabViewModel`, `ScheduledJobsTabViewModel`, `BusinessRulesTabViewModel`, `ApprovalsTabViewModel`, `AutomationDashboardTabViewModel`) |
| Files | 5 prod + 5 test = **10** |
| `using` additions | **0 prod** + 2 test (`ApprovalsTabViewModelTests`, `AutomationDashboardTabViewModelTests`) |
| Estimated tests | ~13 assertion *additions* to existing tests + 0 net new; suite ≈ 2,715 → **~2,715** |
| Risk | **LOWEST of the P2 sub-waves so far** — no `catch`-clause change at all (the `when` filter keeps the variable); a single-line string swap per site; the filter, `State = Error`, and all logging are untouched; the identical shape already exists in 3 of the 5 files (Wave F); every failure path already has a test + a seam and passes unchanged |

**No split needed.** One domain (Automation), one shape, tight cluster. `AutomationPageViewModel` needs nothing.

### Implementation plan (Phase 8.116)

- **Prod files (5) — each: `ErrorMessage = exception.Message` → `ErrorMessage = Localization.Strings.Common_ActionFailedMessage`; keep `catch (Exception exception) when (exception is not OperationCanceledException)`, `State = Error`, `LogOperationFailed(nameof(...))` exactly:**
  - `WorkflowsTabViewModel.cs` — `LoadAsync`, `CreateDraftAsync`, `PublishAsync`, `RunNowAsync`, `RollbackAsync` (5)
  - `ScheduledJobsTabViewModel.cs` — `LoadAsync`, `CreateAsync`, `RunNowAsync` (3)
  - `BusinessRulesTabViewModel.cs` — `LoadAsync`, `CreateAsync` (2)
  - `ApprovalsTabViewModel.cs` — `LoadAsync`, `DecideAsync` (2)
  - `AutomationDashboardTabViewModel.cs` — `LoadAsync` (1)
  - **No `using` additions** (fully-qualified `Localization.Strings.…` throughout, matching the Wave F guards).
- **Test files (5):** add `Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` + `Assert.DoesNotContain(Secret, sut.ErrorMessage)` to the 13 existing failure tests; `+ using …Localization;` to `ApprovalsTabViewModelTests` + `AutomationDashboardTabViewModelTests`.
- **No** DI / service / contract / `.resx` / stub / new-file change.
- **Commit subject:** `fix(desktop): sanitize automation tab error surfacing`

### Separate from Missing-Guard work

Missing-Guard Sweep (`794648e` … `0260bc3`) is complete. This changes the *message string* in *pre-existing* filtered catches — no new guard, no behaviour, no filter change.

---

## STOP

Phase 8.115 audit complete. HEAD `b509054`, tracked tree clean, baseline 2,715 / 2,715.
**13 Category-A sites across 5 Automation tab ViewModels** — `WorkflowsTabViewModel` (`LoadAsync` / `CreateDraftAsync` / `PublishAsync` / `RunNowAsync` / `RollbackAsync`), `ScheduledJobsTabViewModel` (`LoadAsync` / `CreateAsync` / `RunNowAsync`), `BusinessRulesTabViewModel` (`LoadAsync` / `CreateAsync`), `ApprovalsTabViewModel` (`LoadAsync` / `DecideAsync`), `AutomationDashboardTabViewModel` (`LoadAsync`) — surface `exception.Message` to a bound `TextBlock` from a **Phase 8.39 filtered** `catch (Exception exception) when (exception is not OperationCanceledException)`, exposing workflow definitions, business-rule conditions/actions, cron expressions, triggers, org/branch/user ids, and backend payloads. **The `when` clause needs the `exception` variable, so the fix is the minimal one:** `ErrorMessage = exception.Message` → `ErrorMessage = Localization.Strings.Common_ActionFailedMessage`, keeping the filter, `State = Error`, and every operation-name-only log call. **No `catch`-clause change, no `using` addition in prod, no `.resx` / DI / service / contract / stub change.** The existing Phase 8.39 tests pass unchanged (they never assert `ErrorMessage`); recommended hardening adds ~13 surface no-leak assertions + 2 test `using` lines.
**Recommendation: one commit, all 13 sites, LOWEST risk of the P2 sub-waves. Suite ~2,715.**

**Awaiting Phase 8.116 authorization.**
