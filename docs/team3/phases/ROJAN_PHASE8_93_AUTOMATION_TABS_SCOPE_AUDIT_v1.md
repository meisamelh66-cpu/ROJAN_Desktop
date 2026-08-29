# ROJAN AI — TEAM 3 — PHASE 8.93 — MISSING-GUARD SWEEP — WAVE F (AUTOMATION TABS) — SCOPE AUDIT v1

**Type:** AUDIT ONLY. No source change. No test change. No guard added. No service / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `4b1afca431ec0eb6a366055be9054bfc4dacc1e1`
**Objective:** Audit the Automation tab ViewModels and identify the remaining unguarded user-triggered command-failure boundaries, using the Wave A–E pattern **adapted to the tabs' pre-existing filtered-catch shape**.

---

## A. GIT STATE

```
git rev-parse HEAD        → 4b1afca431ec0eb6a366055be9054bfc4dacc1e1
git branch --show-current → feature/team3-desktop-completion
git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'   → (empty)
```

| Check | Result |
|---|---|
| HEAD | `4b1afca` (Wave E / AI Center commit) ✅ |
| Branch | `feature/team3-desktop-completion` ✅ |
| Tracked working tree | **clean** ✅ |
| Untracked | only `ROJAN_*.md` reports |
| Last 3 commits | `4b1afca` guard AI Center · `6f64ffa` guard report export · `5640123` guard reporting |

Baseline test suite (checkpoint §E, `4b1afca`): **2,691 / 2,691** — Domain 456, Application 791, Presentation 748, Infrastructure 609, Shell 80, Architecture 7.

---

## B. AUTOMATION INVENTORY

The Automation domain has **5 tab ViewModels** under `src/…/ViewModels/Automation/`, all `sealed partial`, each with a **single** `ILogger<TSelf>` field + an instance-form operation-name-only `[LoggerMessage(EventId = 1, Level = Error)] private partial void LogOperationFailed(string operation);` (Phase 8.39 Wave 2C-2). **No logging-infrastructure change needed** — Wave F only adds call sites. `AutomationPageViewModel` (the parent) already forwards `ILogger<TChild>?` to each of the 5 tabs at their `new` site (Phase 8.39).

### B.1 Per-tab command audit

| ViewModel | Command → method | Already guarded? | Error surface | User impact today on failure |
|---|---|---|---|---|
| **`WorkflowsTabViewModel`** | `LoadCommand` → `LoadAsync` | ✅ `catch (Exception exception) when (exception is not OperationCanceledException)` → `ErrorMessage = exception.Message` + `State = Error` + `LogOperationFailed(nameof(LoadAsync))` | `State` / `ErrorMessage` | recovered; `ErrorMessage` shows raw `exception.Message` (P2 leak — §E) |
| | `CreateDraftCommand` → `CreateDraftAsync` | ✅ filtered catch → `ErrorMessage = exception.Message` + `LogOperationFailed` (no `State`) | `ErrorMessage` (inline) | recovered; raw-message P2 leak |
| | `PublishCommand` → `PublishAsync` | ✅ filtered catch → `ErrorMessage = exception.Message` + `LogOperationFailed` | `ErrorMessage` | recovered; raw-message P2 leak |
| | `RunNowCommand` → `RunNowAsync` | ✅ filtered catch → `ErrorMessage = exception.Message` + `LogOperationFailed` | `ErrorMessage` | recovered; raw-message P2 leak |
| | `RollbackCommand` → `RollbackAsync` | ✅ filtered catch → `ErrorMessage = exception.Message` + `LogOperationFailed` | `ErrorMessage` | recovered; raw-message P2 leak |
| | **`ArchiveCommand` → `ArchiveAsync`** | ❌ **NONE** — `await _workflowService.ArchiveAsync(workflow.Id)` + `await LoadAsync()` | none | **generic `App.DispatcherUnhandledException` dialog** on a failed archive |
| | **`DeleteCommand` → `DeleteAsync`** | ❌ **NONE** — `await _workflowService.DeleteAsync(workflow.Id)` + `await LoadAsync()` | none | generic dialog on a failed workflow delete |
| | **`LoadVersionHistoryAsync`** (private) | ❌ **NONE** — `await _workflowService.GetVersionsAsync(SelectedWorkflow.ParentWorkflowId)`; invoked `_ = LoadVersionHistoryAsync()` fire-and-forget from the **`SelectedWorkflow` setter** | none | generic dialog when the user selects a workflow and its version list fails to load |
| **`ScheduledJobsTabViewModel`** | `LoadCommand` → `LoadAsync` | ✅ filtered catch → `ErrorMessage` + `State = Error` + log | `State` / `ErrorMessage` | recovered; P2 leak |
| | `CreateCommand` → `CreateAsync` | ✅ filtered catch → `ErrorMessage` + log | `ErrorMessage` | recovered; P2 leak |
| | `RunNowCommand` → `RunNowAsync` | ✅ filtered catch → `ErrorMessage` + log | `ErrorMessage` | recovered; P2 leak |
| | **`ToggleEnabledCommand` → `ToggleEnabledAsync`** | ❌ **NONE** — `await _scheduledJobService.SetEnabledAsync(job.Id, !job.IsEnabled)` + `await LoadAsync()` | none | generic dialog on a failed enable/disable |
| | **`DeleteCommand` → `DeleteAsync`** | ❌ **NONE** — `await _scheduledJobService.DeleteAsync(job.Id)` + `await LoadAsync()` | none | generic dialog on a failed job delete |
| **`BusinessRulesTabViewModel`** | `LoadCommand` → `LoadAsync` | ✅ filtered catch → `ErrorMessage` + `State = Error` + log | `State` / `ErrorMessage` | recovered; P2 leak |
| | `CreateCommand` → `CreateAsync` | ✅ filtered catch → `ErrorMessage` + log | `ErrorMessage` | recovered; P2 leak |
| | **`ToggleEnabledCommand` → `ToggleEnabledAsync`** | ❌ **NONE** — `await _businessRuleService.SetEnabledAsync(rule.Id, !rule.IsEnabled)` + `await LoadAsync()` | none | generic dialog on a failed enable/disable |
| | **`DeleteCommand` → `DeleteAsync`** | ❌ **NONE** — `await _businessRuleService.DeleteAsync(rule.Id)` + `await LoadAsync()` | none | generic dialog on a failed rule delete |
| **`AutomationDashboardTabViewModel`** | `LoadCommand` → `LoadAsync` | ✅ filtered catch → `ErrorMessage` + `State = Error` + log | `State` / `ErrorMessage` | **no unguarded command** — audited clean |
| **`ApprovalsTabViewModel`** | `LoadCommand` → `LoadAsync`; `ApproveCommand` / `RejectCommand` → `DecideAsync` | ✅ both filtered catch → `ErrorMessage` + log | `State` / `ErrorMessage` | **no unguarded command** — audited clean |

### B.2 The gap — **6 command methods + 1 secondary-load path** across 3 tab VMs

| VM | Unguarded members |
|---|---|
| `WorkflowsTabViewModel` | `ArchiveAsync`, `DeleteAsync`, `LoadVersionHistoryAsync` (fire-and-forget setter path) |
| `ScheduledJobsTabViewModel` | `ToggleEnabledAsync`, `DeleteAsync` |
| `BusinessRulesTabViewModel` | `ToggleEnabledAsync`, `DeleteAsync` |

This matches `ROJAN_PHASE8_64_*` §D's "~7" for Wave F. `AutomationDashboardTabViewModel` and `ApprovalsTabViewModel` need nothing.

### B.3 CancellationToken usage

**None of the tab VM methods threads a `CancellationToken`** — every call passes the service's `default`. The pre-existing guards nonetheless use `catch (Exception exception) when (exception is not OperationCanceledException)` — a Phase 8.39 **defensive** convention: the `_workflowService` / `_scheduledJobService` / `_businessRuleService` / `_executionEngine` methods all accept a `CancellationToken` parameter, so *if* a future caller passes a live token and cancels mid-await, the resulting `OperationCanceledException` must **propagate** (not become a UI error, not be logged). §D covers this.

---

## C. CLASSIFICATION

| Category | Members | Action |
|---|---|---|
| **A — backend-connected mutation command needing a guard** | `WorkflowsTabViewModel.ArchiveAsync` / `.DeleteAsync`; `ScheduledJobsTabViewModel.ToggleEnabledAsync` / `.DeleteAsync`; `BusinessRulesTabViewModel.ToggleEnabledAsync` / `.DeleteAsync` — **6 mutations**; plus `WorkflowsTabViewModel.LoadVersionHistoryAsync` (user-selection-triggered read, currently an unobserved-task-exception on failure) — **1 secondary load** | **guard in Phase 8.94 — 7 members** |
| **B — read-only** | — (no pure background read command; `LoadVersionHistoryAsync` is user-selection-triggered → Category A) |
| **C — already guarded** | 5 in `WorkflowsTabViewModel` (`LoadAsync` / `CreateDraftAsync` / `PublishAsync` / `RunNowAsync` / `RollbackAsync`); 3 in `ScheduledJobsTabViewModel` (`LoadAsync` / `CreateAsync` / `RunNowAsync`); 2 in `BusinessRulesTabViewModel` (`LoadAsync` / `CreateAsync`); all of `AutomationDashboardTabViewModel` + `ApprovalsTabViewModel` | **do not modify** |
| **D — cancellation-sensitive** | **all** guards — existing and the 7 new — MUST use the `catch (Exception exception) when (exception is not OperationCanceledException)` filter (§D). No tab method threads a token today, but the filter is a hard requirement for consistency and future-safety. |

---

## D. CANCELLATION REVIEW

| Requirement | Finding |
|---|---|
| **Methods requiring `catch (Exception) when (exception is not OperationCanceledException)`** | **all 7 new guards** (`ArchiveAsync`, `DeleteAsync`, `LoadVersionHistoryAsync` — Workflow; `ToggleEnabledAsync`, `DeleteAsync` — ScheduledJob; `ToggleEnabledAsync`, `DeleteAsync` — BusinessRule) — matching the 10 existing Category-C guards across the same 3 VMs. |
| **Cancellation behavior preserved** | ✅ — the `when (exception is not OperationCanceledException)` filter means a raised `OperationCanceledException` (or `TaskCanceledException : OperationCanceledException`) **is not caught** by the guard; it propagates exactly as it does today for the 10 existing guards. `AsyncRelayCommand` treats a propagated `OperationCanceledException` from an `async void` execute as a benign cancellation (not an app-crash) — same as the current behaviour for every other filtered guard in these VMs. |
| **No `OperationCanceledException` converted into a UI error** | ✅ — the filter excludes it; the guard body (which sets `ErrorMessage` / logs) only runs for non-cancellation exceptions. |
| **No logging noise from user cancellation** | ✅ — `LogOperationFailed` is inside the guard body, which the filter skips for `OperationCanceledException`. A cancelled operation logs nothing. |

**Confirmed:** the new guards are a byte-for-byte-shape copy of the tabs' existing filtered guards — no behavioural divergence on the cancellation path.

---

## E. SECURITY

Automation carries **workflow definitions (step graphs, names, descriptions), business rules (field/operator/value conditions, action parameters — discount %, target workflow id), scheduling data (cron expressions, frequencies), and the org/branch/user ids that scope every operation.**

### E.1 The 7 new guards

| Vector | Finding |
|---|---|
| `Exception.Message` / `.ToString()` → log file | **not exposed** — reuses each VM's operation-name-only `[LoggerMessage] LogOperationFailed(string operation)`; the caught exception is **never passed**. `LocalFileLoggerProvider` renders no backend body / rule payload / workflow content. Test-enforced via the existing `AssertSingleErrorFor` helper (seeded `Secret` `DoesNotContain`). |
| **Rule payload / workflow content leakage** → log | **prevented** — the guard reads no `BusinessRuleDto` / `WorkflowDefinitionDto` field into the log; `{Operation}` = `nameof(Method)` (a literal method name string) |
| Backend exception bodies → log | **prevented** — no exception variable passed to the logger |
| `Exception.Message` → UI | **prevented (stricter than the existing 5-per-VM guards)** — the new guards write **`ErrorMessage = Localization.Strings.Common_ActionFailedMessage`** (a fixed localized constant), **not** `ErrorMessage = exception.Message` |

### E.2 Pre-existing UI leak (OUT OF SCOPE — "sanitize load-error surfacing" P2)

The **10 existing** Category-C guards across these 3 VMs (and the Load guards on all 5 tabs) do `ErrorMessage = exception.Message` — the same "sanitize load-error surfacing" P2 flagged for `ReportingPageViewModel` (`ROJAN_PHASE8_81_*` §D.2) and `AiCenterPageViewModel` (`ROJAN_PHASE8_89_*` §D.2). **Wave F does not touch them** (Category C). Given Automation's rule/workflow content, the P2 phase should flip these to the generic string too, making each tab VM's error surface fully consistent. **The 7 new Wave F guards are already leak-free** (they use the generic constant from the start).

`RunNowAsync` (workflow / job execution) is Category C — not touched.

---

## F. ARCHITECTURE

| Check | Value |
|---|---|
| **`ILogger` availability** | each of the 3 target tab VMs has a single `ILogger<TSelf> _logger` field + instance-form `[LoggerMessage] LogOperationFailed(string operation)` (Phase 8.39). **Reusable as-is** — Wave F adds 7 call sites (2 + 2 + 2 + 1). |
| **`[LoggerMessage]` pattern** | instance-form, `EventId = 1`, `Level = Error`, message `"Automation <workflows/scheduled jobs/business rules> operation failed. Operation={Operation}"` — unchanged, only new call sites |
| **`ILoggerFactory` need** | **none** — the tab VMs are `new`'d by `AutomationPageViewModel`, which already passes `ILogger<TChild>?` to each (Phase 8.39 pass-through). No parent change. No child VM below the tabs. |
| **`SYSLIB1020` risk** | **none** — one `ILogger` field + instance-form `[LoggerMessage]` per VM (compiled clean at `4b1afca` and every wave since Phase 8.39) |
| **DI impact** | **none** — no constructor change; no new field. Each new guard reuses `_logger` via `LogOperationFailed(...)`. |
| **Localization** | `+ using Rojan.Desktop.Presentation.Localization;` to each of the 3 tab VMs (they import `Rojan.Desktop.Application.Automation` but not `…Localization`), **or** the fully-qualified `Localization.Strings.Common_ActionFailedMessage` (resolves via the parent `Rojan.Desktop.Presentation` namespace). No `.resx` change — `Common_ActionFailedMessage` ships (Wave A `794648e`); there is **no `Automation_*Error` string**. |

---

## G. GUARD STRATEGY & TEST PLAN

### G.1 Guard shape (matches the tabs' 10 existing filtered guards, minus the leak)

```csharp
private async Task ArchiveAsync(WorkflowDefinitionDto workflow)
{
    try
    {
        await _workflowService.ArchiveAsync(workflow.Id).ConfigureAwait(true);   // UNCHANGED
        await LoadAsync().ConfigureAwait(true);                                    // UNCHANGED (clears ErrorMessage on success)
    }
    catch (Exception exception) when (exception is not OperationCanceledException)
    {
        ErrorMessage = Localization.Strings.Common_ActionFailedMessage;
        LogOperationFailed(nameof(ArchiveAsync));
    }
}
```

- **Wave A–E `ActionErrorMessage` pattern does NOT apply verbatim** — these VMs already have an inline `ErrorMessage` property used by their 10 existing command guards for exactly this purpose. Adding a parallel `ActionErrorMessage` would give each tab VM *two* inline error surfaces. **Reuse `ErrorMessage`** (the established surface) with the **generic constant** (`Common_ActionFailedMessage`) and the **filtered catch**. No new property, no `HasActionError`, no XAML binding change (the views already bind `ErrorMessage`).
- **No `State = DashboardState.Error`** — matching the existing command guards (only `LoadAsync` sets `State`). A failed archive/delete/toggle does not blank the tab.
- `ArchiveAsync` / `DeleteAsync` (×3) call `await LoadAsync()` on the success path — inside the `try`; `LoadAsync` is self-guarded and clears `ErrorMessage = null` at its start, so a successful mutation clears any prior error.
- **`LoadVersionHistoryAsync`** (no follow-on `LoadAsync`): wrap the body in the filtered catch; **additionally clear `ErrorMessage = null` on the success path** inside the `try` (its one deviation from the mutation guards, since nothing else clears it). It is called from **one** place (`SelectedWorkflow` setter, fire-and-forget) — wrapping its body directly is safe (no other caller could have its exception swallowed).

### G.2 Files

| Group | Files | Count |
|---|---|---|
| Production | `Automation/WorkflowsTabViewModel.cs`, `Automation/ScheduledJobsTabViewModel.cs`, `Automation/BusinessRulesTabViewModel.cs` | 3 |
| Test stub | `tests/…/Automation/StubAutomationServices.cs` — **+7 additive `Exception?` seams**: `StubWorkflowService` `ArchiveException` / `DeleteException` / `GetVersionsException`; `StubScheduledJobService` `SetEnabledException` / `DeleteException`; `StubBusinessRuleService` `SetEnabledException` / `DeleteException` (the file already has 16 seams from Phase 8.39, e.g. `GetAllException` / `CreateDraftException` / `PublishException` / `RollbackException` / …) | 1 |
| Test | `tests/…/Automation/WorkflowsTabViewModelTests.cs`, `…/ScheduledJobsTabViewModelTests.cs`, `…/BusinessRulesTabViewModelTests.cs` | 3 |
| **Total** | | **7** |

No new file, no `Strings.cs` / `.resx` change, no ctor / DI / service / `[LoggerMessage]`-signature change, no `AutomationPageViewModel` change.

### G.3 Test plan

| Category | Tests | Count |
|---|---|---|
| **Failure logs operation-name-only + no leak** — one per new guard, using the existing `AssertSingleErrorFor(logger, "<Method>")` helper (single `Error` entry, `Operation=<Method>`, `DoesNotContain(Secret)`) | `ArchiveAsync`, `DeleteAsync`, `LoadVersionHistoryAsync` (Workflow); `ToggleEnabledAsync`, `DeleteAsync` (ScheduledJob); `ToggleEnabledAsync`, `DeleteAsync` (BusinessRule) | 7 |
| **Failure does not throw + `ErrorMessage` is generic** — `Record.Exception(() => Cmd.Execute(x))` is `null`; `ErrorMessage == Strings.Common_ActionFailedMessage`; `State != DashboardState.Error`; the item is still in its collection (mutation didn't apply locally) | ~4 (representative across the 3 VMs) | ~4 |
| **Success clears error** — `Archive`/`Delete`/`ToggleEnabled` fail once → `ErrorMessage` set → clear seam → succeed → `ErrorMessage == null` (via the follow-on `LoadAsync`; `LoadVersionHistoryAsync` via its own success clear) | ~2 | ~2 |

**Estimated new tests: ~13.** Conservative suite projection: **2,691 → ~2,704**. (A dedicated cancellation test is **not** added — matching the existing 10 guards, which have no cancellation test; no tab method threads a token, so `OperationCanceledException` cannot be produced without new plumbing.)

### G.4 Risk

**LOW.** 7 additive filtered `try`/`catch` that are a shape-copy of the 10 guards already in these files; fake-backed domain; no ctor / DI / service / `[LoggerMessage]` change; the 10 existing guards and the ~40 existing Automation-tab tests are untouched. The one deviation (`LoadVersionHistoryAsync` clears `ErrorMessage` on success) is a 1-line addition.

---

## H. PHASE 8.94 RECOMMENDATION

**PHASE 8.94 — MISSING-GUARD SWEEP — WAVE F (AUTOMATION TABS) — IMPLEMENTATION v1**

**Exact scope — modify ONLY:**
- `src/Rojan.Desktop.Presentation/ViewModels/Automation/WorkflowsTabViewModel.cs`:
  - `+ using Rojan.Desktop.Presentation.Localization;` (or fully-qualify)
  - wrap `ArchiveAsync`, `DeleteAsync` in the §G.1 `try { existing body } catch (Exception exception) when (exception is not OperationCanceledException) { ErrorMessage = Strings.Common_ActionFailedMessage; LogOperationFailed(nameof(<Method>)); }`
  - wrap `LoadVersionHistoryAsync` body in the same filtered catch; **add `ErrorMessage = null;` on the success path** (inside the `try`, after the `foreach`)
  - **do not touch** `LoadAsync`, `CreateDraftAsync`, `PublishAsync`, `RunNowAsync`, `RollbackAsync`, `BuildDefaultSteps`, the ctor, or the `[LoggerMessage]` signature
- `src/Rojan.Desktop.Presentation/ViewModels/Automation/ScheduledJobsTabViewModel.cs`: `+ using …Localization;`; wrap `ToggleEnabledAsync`, `DeleteAsync` identically. Do not touch `LoadAsync` / `CreateAsync` / `RunNowAsync`.
- `src/Rojan.Desktop.Presentation/ViewModels/Automation/BusinessRulesTabViewModel.cs`: `+ using …Localization;`; wrap `ToggleEnabledAsync`, `DeleteAsync` identically. Do not touch `LoadAsync` / `CreateAsync`.
- `tests/Rojan.Desktop.Presentation.Tests/Automation/StubAutomationServices.cs`: +7 additive `Exception?` seams (§G.2), null-path byte-identical
- `tests/Rojan.Desktop.Presentation.Tests/Automation/{Workflows,ScheduledJobs,BusinessRules}TabViewModelTests.cs`: ~13 new tests (§G.3), reusing each file's existing `AssertSingleErrorFor` / `CreateLoggedSut` helpers; existing tests unchanged

**DO NOT:** change any service / DI / ViewModel constructor / backend contract / RBAC / `CanExecute` / navigation / `AutomationPageViewModel` / `AutomationDashboardTabViewModel` / `ApprovalsTabViewModel` / `[LoggerMessage]` signature / `Strings.cs` / `.resx` / the 10 pre-existing filtered guards. No commit.

**Risk: LOW** (per §G.4).

**Validation expectation:**
- `dotnet build -c Debug` → **0 warnings / 0 errors** (single `ILogger` + instance form → no `SYSLIB1020`; the filtered `when` clause is already the established pattern — no `CA1031` change).
- Full suite → **~2,704 / ~2,704 PASS** (Presentation 748 → ~761; Domain 456, Application 791, Infrastructure 609, Shell 80 unchanged).
- Architecture tests → **7 / 7 PASS**.
- Deliverable: `ROJAN_PHASE8_94_AUTOMATION_TABS_IMPLEMENTATION_REPORT_v1.md`. STOP before commit; wait for Phase 8.95 commit scope review.
- **Commit (Phase 8.97):** one — `fix(desktop): guard remaining automation tab command failures` (`ROJAN_PHASE8_64_*` §D wording).

**Downstream:** after Wave F → **Wave G (P2 infra)** — Workspace / Notification / Settings / CommandPalette (~28 methods, local/infra, low priority) closes the Missing-Guard Sweep. Separately, a "sanitize load-error surfacing" P2 phase should flip every tab VM's + `ReportingPageViewModel`'s + `AiCenterPageViewModel`'s `= exception.Message` surfacings to the generic string in one pass.

---

## STOP

Phase 8.93 audit complete. HEAD `4b1afca`, tracked tree clean, baseline 2,691 / 2,691.
Wave F = **7 unguarded members across 3 Automation tab VMs** — `WorkflowsTabViewModel` (`ArchiveAsync`, `DeleteAsync`, `LoadVersionHistoryAsync`), `ScheduledJobsTabViewModel` (`ToggleEnabledAsync`, `DeleteAsync`), `BusinessRulesTabViewModel` (`ToggleEnabledAsync`, `DeleteAsync`). `AutomationDashboardTabViewModel` + `ApprovalsTabViewModel` are clean. **Key difference from Waves A–E:** the guards must be a shape-copy of the tabs' **10 pre-existing** `catch (Exception exception) when (exception is not OperationCanceledException)` guards — reusing the existing `ErrorMessage` property (not a new `ActionErrorMessage`), with the **generic constant** `Common_ActionFailedMessage` (leak-free, unlike the 10 existing guards' `= exception.Message` — flagged P2), `LogOperationFailed(nameof(Method))`, and **no `State = Error`**. The `when` filter preserves cancellation (propagate, no UI error, no log noise). Reuses each VM's existing single `ILogger` + instance-form `[LoggerMessage]` — no `ILoggerFactory`, no `SYSLIB1020`, no DI / ctor change. ~7 files, ~13 tests, one commit `fix(desktop): guard remaining automation tab command failures`.
**Recommended next: Phase 8.94 — Wave F (Automation tabs) Implementation.** Awaiting authorization.
