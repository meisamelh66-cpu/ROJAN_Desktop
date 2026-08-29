# ROJAN AI — TEAM 3 — PHASE 8.117 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 4 (AUTOMATION TABS) — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No** source / test / fix / new-change / commit / push / merge / rebase / amend. Nothing staged.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `b509054` (unchanged)
**Reference:** `ROJAN_PHASE8_115_P2_SUBWAVE4_SCOPE_AUDIT_v1.md`, `ROJAN_PHASE8_116_P2_SUBWAVE4_IMPLEMENTATION_REPORT_v1.md`
**Verdict: READY TO COMMIT** for the 10 sites Phase 8.116's file list authorised. **3 audited sites (`ApprovalsTabViewModel` ×2, `AutomationDashboardTabViewModel` ×1) were excluded by Phase 8.116's own STRICT SCOPE and remain — this is a scope-restriction deferral, not a blocker on this commit. See §C.**

---

## A. GIT STATE

```
git rev-parse HEAD        → b5090549dac02b1d20de2bd4f211e3d4b27098a8
git branch --show-current → feature/team3-desktop-completion
git diff --cached --stat  → (empty — nothing staged)
```

### Modified tracked files — 6, all Phase 8.116

```
 M src/Rojan.Desktop.Presentation/ViewModels/Automation/WorkflowsTabViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Automation/ScheduledJobsTabViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Automation/BusinessRulesTabViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Automation/WorkflowsTabViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Automation/ScheduledJobsTabViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Automation/BusinessRulesTabViewModelTests.cs
```

Diffstat: `6 files changed, 33 insertions(+), 10 deletions(-)`. Untracked: only `ROJAN_*.md`. **Confirmed: only Phase 8.116 changes present; staging empty.**

- **Prod deletions (10):** all are `ErrorMessage = exception.Message;` lines replaced with `ErrorMessage = Localization.Strings.Common_ActionFailedMessage;`.
- **Test deletions: 0** — every test change is a pure `+` (13 no-leak assertions added to the existing Phase 8.39 failure tests). No test removed, no test renamed.

---

## B. SCOPE

| Modified file | Prod / test | Notes |
|---|---|---|
| `Automation/WorkflowsTabViewModel.cs` | prod | 5 catches — `LoadAsync`, `CreateDraftAsync`, `PublishAsync`, `RunNowAsync`, `RollbackAsync`; each `ErrorMessage = exception.Message` → `= Localization.Strings.Common_ActionFailedMessage` |
| `Automation/ScheduledJobsTabViewModel.cs` | prod | 3 catches — `LoadAsync`, `CreateAsync`, `RunNowAsync`; same swap |
| `Automation/BusinessRulesTabViewModel.cs` | prod | 2 catches — `LoadAsync`, `CreateAsync`; same swap |
| `Automation/WorkflowsTabViewModelTests.cs` | test | `+ AssertGenericSurfaceNoLeak(sut)` helper + 5 call sites |
| `Automation/ScheduledJobsTabViewModelTests.cs` | test | `+ Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` + `+ DoesNotContain(Secret, sut.ErrorMessage)` in 3 tests |
| `Automation/BusinessRulesTabViewModelTests.cs` | test | same, 2 tests |

| Must stay untouched | Status |
|---|---|
| `AutomationPageViewModel.cs` | ✅ not in diff — **no `= exception.Message` / no broad catch exists in it** (it is the parent orchestrator; it delegates to the tab VMs and has no failure boundary of its own) — nothing to sanitize |
| `ApprovalsTabViewModel.cs` / `AutomationDashboardTabViewModel.cs` | ✅ not in diff — **excluded from Phase 8.116's authorised production file list** (see §C) |
| Services / query & command services | ✅ not in diff |
| Backend contracts / DTOs | ✅ not in diff |
| DI registration | ✅ not in diff |
| Localization files (`Strings.resx` / `.en` / `.ar`) | ✅ not in diff — `Common_ActionFailedMessage` reused (Wave A) |
| Test stubs (`StubAutomationServices.cs`) | ✅ not in diff — every failure path uses a pre-existing `Exception?` seam |
| Shell / navigation / other ViewModels | ✅ not in diff |

**6 files, 100% within the STRICT SCOPE allowance. No `using` additions anywhere.**

---

## C. SITE COVERAGE

### The two "13" lists do not match — resolved here

| Source | The "13" it names |
|---|---|
| **Phase 8.115 audit §B** | Workflows 5 + ScheduledJobs 3 + BusinessRules 2 + **`ApprovalsTabViewModel` 2 + `AutomationDashboardTabViewModel` 1** = 13. `AutomationPageViewModel` = **0** sites. |
| **Phase 8.117 TASK C** | Workflows 5 + ScheduledJobs 3 + BusinessRules 2 + **`AutomationPageViewModel.LoadAsync` 1** = 11 named (labelled "13"). |
| **Phase 8.116 STRICT SCOPE production file list** | `AutomationPageViewModel.cs` (no sites), `WorkflowsTabViewModel.cs`, `ScheduledJobsTabViewModel.cs`, `BusinessRulesTabViewModel.cs` — **`ApprovalsTabViewModel.cs` and `AutomationDashboardTabViewModel.cs` were NOT listed.** |

### Coverage of the sites Phase 8.116 was authorised to touch — **10 / 10 ✅**

| # | VM · method | Before | After | `when` filter | `State = Error` | `LogOperationFailed` |
|---|---|---|---|---|---|---|
| 1 | `WorkflowsTabViewModel.LoadAsync` | `ErrorMessage = exception.Message` | `= Localization.Strings.Common_ActionFailedMessage` | ✅ byte-unchanged | ✅ kept | ✅ `nameof(LoadAsync)` |
| 2 | `WorkflowsTabViewModel.CreateDraftAsync` | ″ | ″ | ✅ | n/a | ✅ `nameof(CreateDraftAsync)` |
| 3 | `WorkflowsTabViewModel.PublishAsync` | ″ | ″ | ✅ | n/a | ✅ `nameof(PublishAsync)` |
| 4 | `WorkflowsTabViewModel.RunNowAsync` | ″ | ″ | ✅ | n/a | ✅ `nameof(RunNowAsync)` |
| 5 | `WorkflowsTabViewModel.RollbackAsync` | ″ | ″ | ✅ | n/a | ✅ `nameof(RollbackAsync)` |
| 6 | `ScheduledJobsTabViewModel.LoadAsync` | ″ | ″ | ✅ | ✅ kept | ✅ `nameof(LoadAsync)` |
| 7 | `ScheduledJobsTabViewModel.CreateAsync` | ″ | ″ | ✅ | n/a | ✅ `nameof(CreateAsync)` |
| 8 | `ScheduledJobsTabViewModel.RunNowAsync` | ″ | ″ | ✅ | n/a | ✅ `nameof(RunNowAsync)` |
| 9 | `BusinessRulesTabViewModel.LoadAsync` | ″ | ″ | ✅ | ✅ kept | ✅ `nameof(LoadAsync)` |
| 10 | `BusinessRulesTabViewModel.CreateAsync` | ″ | ″ | ✅ | n/a | ✅ `nameof(CreateAsync)` |

`grep -rn "= exception.Message;" src/…/ViewModels/Automation/` → **only** `ApprovalsTabViewModel.cs:79`, `:96`, `AutomationDashboardTabViewModel.cs:122` remain.

### The 3 remaining sites — NOT a blocker on this commit

- **`AutomationPageViewModel.LoadAsync`** (named in TASK C) — **there is no such leak site.** `AutomationPageViewModel` has no `= exception.Message` and no broad catch. Nothing to sanitize; correctly untouched.
- **`ApprovalsTabViewModel.LoadAsync` / `.DecideAsync` (2)** and **`AutomationDashboardTabViewModel.LoadAsync` (1)** — audited in Phase 8.115, but **Phase 8.116's STRICT SCOPE production file list did not authorise their files.** They were never in scope for this implementation. This is a **scope-restriction deferral**, materially different from "an authorised site left untouched":
  - Every site the Phase 8.116 STRICT SCOPE *did* authorise is sanitised (10/10).
  - TASK C's "MARK BLOCKER" condition — *"if any **approved** site remains untouched"* — is not met, because those 3 files were explicitly excluded from Phase 8.116's approval.
  - **Recommendation for Phase 8.118:** authorise a short **sub-wave-4 addendum** (or fold into sub-wave 6) covering `ApprovalsTabViewModel.cs` + `AutomationDashboardTabViewModel.cs` (3 sites, identical `when`-filtered shape, `+ using …Localization;` or the FQ form, ~3–4 assertion additions to the existing Phase 8.39 tests, LOW risk). This commit should proceed for the 10 authorised sites.

---

## D. SECURITY

Every one of the 10 catches now assigns the fixed localized constant. The `exception` variable is **still bound** (the `when` predicate references it) **but is no longer read in the catch body** — `exception.Message` / `.ToString()` / `.InnerException` cannot reach the surface.

| Data class | Was reachable via | Now |
|---|---|---|
| **Workflow definitions** (step names, descriptions, trigger config) | `WorkflowsTabViewModel` `CreateDraftAsync` / `PublishAsync` / `RollbackAsync` | **not reachable** — `WorkflowsTabViewModelTests` now asserts `DoesNotContain(Secret, sut.ErrorMessage)` with `Secret = "workflow-definition-SECRET-vip"` |
| **Workflow / job execution details** | `WorkflowsTabViewModel.RunNowAsync`, `ScheduledJobsTabViewModel.RunNowAsync` | **not reachable** — `DoesNotContain(Secret, sut.ErrorMessage)` |
| **Cron expressions** | `ScheduledJobsTabViewModel.CreateAsync` | **not reachable** — `ScheduledJobsTabViewModelTests` asserts `DoesNotContain(Secret, sut.ErrorMessage)` with `Secret = "cron-0-9-star-star-1-SECRET"` |
| **Business-rule conditions / actions** (field/operator/value, discount %, target workflow id) | `BusinessRulesTabViewModel.CreateAsync` | **not reachable** — `BusinessRulesTabViewModelTests` asserts `DoesNotContain(Secret, sut.ErrorMessage)` with `Secret = "IF-Customer-is-VIP-SECRET"` |
| Triggers / internal configuration / org·branch·user ids | all 10 | **not reachable** — generic constant |
| Backend bodies / internal hosts / file paths / DB fragments | all 10 | **not reachable** — generic constant |

### Logs — unchanged, still operation-name-only

All 10 keep `LogOperationFailed(nameof(<Method>))`. `[LoggerMessage]` templates (`"Automation <workflows|scheduled jobs|business rules> operation failed. Operation={Operation}"`) byte-unchanged. The Phase 8.39 operation-name-only **log** no-leak assertions (`AssertSingleErrorFor` / `DoesNotContain(Secret, entry.Message)`) are retained and still pass in all 3 test files.

---

## E. CANCELLATION

Verified against the diff: every one of the 10 catches keeps `catch (Exception exception) when (exception is not OperationCanceledException)` **byte-identical** — the `when` predicate line does not appear as a `-` in the diff anywhere; only the `ErrorMessage =` line changed.

| Property | Result |
|---|---|
| `when (exception is not OperationCanceledException)` still exists | ✅ on all 10 |
| Cancellation propagates | ✅ — `OperationCanceledException` / `TaskCanceledException` still excluded by the filter → not caught → propagates as before |
| No `ErrorMessage` on cancellation | ✅ — the catch body (which now sets the generic constant) only runs for non-cancellation exceptions |
| No log noise on cancellation | ✅ — `LogOperationFailed` sits inside the same filtered body |

---

## F. TESTS

| Gate | Expected | Actual (working tree = `b509054` + Phase 8.116) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full suite | 2,715 / 2,715 | **2,715 / 2,715 PASS** ✅ |
| — Domain / Application / Infrastructure / Shell | 456 / 791 / 609 / 80 | unchanged ✅ |
| — **Presentation** | 772 | **772** (assertions added to existing tests — no net-new) ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Automation subset | 54 / 54 | **54 / 54 PASS** ✅ |

Suite progression: 2,714 (`1260d4e`) → 2,715 (`b509054`, sub-wave 3) → **2,715** (sub-wave 4 — additive assertions, no net-new tests).

### Review of the requested test categories

| Category | Present |
|---|---|
| **generic error assertions** | ✅ — every one of the 10 existing Phase 8.39 failure tests now asserts `Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` |
| **sentinel no-leak tests** | ✅ — each also asserts `Assert.DoesNotContain(Secret, sut.ErrorMessage ?? string.Empty, …)` with the domain sentinel (`workflow-definition-…` / `cron-…` / `IF-Customer-is-VIP-…`); the pre-existing `DoesNotContain(Secret, entry.Message)` log assertions are retained |
| **regression tests** | ✅ — the happy-path tests (`LoadCommand_NoWorkflowsYet_StateIsEmpty`, `CreateDraftCommand_AddsANewDraft…`, `PublishCommand_MarksTheWorkflowPublished`, `RunNowCommand_InvokesTheExecutionEngine`, the Wave F command-guard tests, the `…VersionHistoryCancellation…` cancellation test, etc.) are all unchanged and green |

### Test additivity

**+0 net tests**, 0 renames, 0 deletions — 13 assertions and 1 helper added to the existing Phase 8.39 failure tests. No new test files, **no stub changes**.

---

## G. COMMIT READINESS

| Gate | State |
|---|---|
| Scope | ✅ 6 files (3 prod + 3 test), all authorised |
| Base HEAD | `b509054` — unchanged; staging empty |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,715 / 2,715; Architecture 7 / 7; subset 54 / 54 |
| Site coverage | ✅ **10 / 10** sites authorised by Phase 8.116's file list are sanitised. `AutomationPageViewModel` has no leak site. `ApprovalsTabViewModel` ×2 + `AutomationDashboardTabViewModel` ×1 were **excluded from Phase 8.116's STRICT SCOPE** — a scope-restriction deferral (§C), **not** a "MARK BLOCKER" condition |
| Sanitization | ✅ only the `ErrorMessage =` line changed; the `when` filter, `State = Error`, `LogOperationFailed`, and the success-path `await LoadAsync()` reload are byte-unchanged |
| Cancellation | ✅ `when (exception is not OperationCanceledException)` byte-identical on all 10 — cancellation still propagates, no error, no noise |
| Security | ✅ workflow definitions, cron expressions, business-rule conditions/actions, triggers, and backend payloads structurally unreachable from every surface; sentinel-enforced |
| Localization | ✅ no `.resx` change; no `using` additions |
| DI / services / contracts / stubs | ✅ none |
| Line endings | working-copy files edited via the tool may show LF/CRLF `git diff` warnings; `core.autocrlf=true` normalises to LF in the committed blob (repo-consistent) — cosmetic only |

### Proposed commit

**Subject (Phase 8.117 brief's wording):**
```
fix(desktop): sanitize automation tab error surfacing
```

**Body (suggested):**
```
Swap the raw exception.Message in the pre-existing Phase-8.39 filtered
broad catches to the generic Strings.Common_ActionFailedMessage so a
failed workflow/schedule/rule operation shows a safe message instead of
a workflow definition, a cron expression, a rule condition, or a
backend payload.

- WorkflowsTabViewModel: LoadAsync, CreateDraftAsync, PublishAsync,
  RunNowAsync, RollbackAsync
- ScheduledJobsTabViewModel: LoadAsync, CreateAsync, RunNowAsync
- BusinessRulesTabViewModel: LoadAsync, CreateAsync

Only the ErrorMessage assignment changed. The
catch (Exception exception) when (exception is not
OperationCanceledException) filter, every State = Error, every
operation-name-only LogOperationFailed(nameof(...)), and the
success-path await LoadAsync() reload are byte-unchanged. No
localization, DI, service or contract change. 13 no-leak assertions
added to the existing Phase 8.39 tests; +0 net tests.

ApprovalsTabViewModel (2) and AutomationDashboardTabViewModel (1) are
a follow-up.
```

**Trailers (required):**
```
Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

### Proposed staging (Phase 8.118 — explicit paths, NO `git add -A` / `git add .`)

```
git add \
  src/Rojan.Desktop.Presentation/ViewModels/Automation/WorkflowsTabViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Automation/ScheduledJobsTabViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Automation/BusinessRulesTabViewModel.cs \
  tests/Rojan.Desktop.Presentation.Tests/Automation/WorkflowsTabViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Automation/ScheduledJobsTabViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Automation/BusinessRulesTabViewModelTests.cs
```

Expected post-commit: new HEAD child of `b509054`; `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` update (§B commit table, §E 2,715 unchanged, §G sub-wave 4 = 10/13 sites; `ApprovalsTabViewModel` + `AutomationDashboardTabViewModel` follow-up).

---

## STOP

Phase 8.117 review complete. **Verdict: READY** for the 10 sites Phase 8.116's file list authorised. HEAD `b509054`, staging empty, 6 files modified and nothing else, build 0/0, 2,715/2,715, Architecture 7/7, Automation subset 54/54. Every authorised site drops the exception-message read and swaps `ErrorMessage` → `Localization.Strings.Common_ActionFailedMessage`; the `when (exception is not OperationCanceledException)` filter, every `State = Error`, every operation-name-only log call, and the success-path reload are byte-unchanged; no `using` / `.resx` / DI / service / contract / stub change.
**Scope gap (not a blocker):** `ApprovalsTabViewModel` (`LoadAsync` / `DecideAsync`) and `AutomationDashboardTabViewModel` (`LoadAsync`) — 3 sites audited at Phase 8.115 but **excluded from Phase 8.116's STRICT SCOPE production file list** — remain. `AutomationPageViewModel` has no leak site. Recommend a short sub-wave-4 addendum for the 3.

**Awaiting Phase 8.118 — Sub-Wave 4 Commit Authorization.**
