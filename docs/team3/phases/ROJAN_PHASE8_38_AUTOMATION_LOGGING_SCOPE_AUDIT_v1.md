# ROJAN AI — TEAM 3 — PHASE 8.38 — WAVE 2C-2 AUTOMATION LOGGING — SCOPE AUDIT v1

**Type:** Scope audit only. **No source modified. No logger added. No tests added. No commit. No push.**
**Branch:** `feature/team3-desktop-completion`
**Objective:** Audit Automation-related ViewModels and define a safe logging implementation scope.
This is the **first phase requiring parent→child ViewModel logger plumbing**.

---

## A. Git State

| Item | Value |
|---|---|
| HEAD | `38c24dad5e2f46b54c45aaa8ee77f6f5d1714b08` (`38c24da` — *fix(desktop): log invite lookup and accept failures*, Phase 8.37 / Wave 2C-1b) |
| Branch | `feature/team3-desktop-completion` |
| Tracked working tree | **clean** — `git status` shows only untracked `.md` reports, no modified/deleted tracked files |
| Tests at HEAD | 2,557 / 2,557 pass (Presentation.Tests 614) |
| Architecture tests | 7 / 7 |
| Self-logging ViewModel coverage | 20 of 56 |

Wave 2C-1 (Support + AcceptInvite) is complete. Wave 2C-2 (Automation) is the recommended next
logging batch per the Phase 8.37 checkpoint.

---

## B. ViewModel Inventory

All Automation ViewModels live in
`src/Rojan.Desktop.Presentation/ViewModels/Automation/`. All are `public sealed class : ViewModelBase`.
**None currently hold any `ILogger` field.**

### B.1 Parent

| VM | Lines | Ctor dependencies | Broad `catch` | DI |
|---|---|---|---|---|
| `AutomationPageViewModel` | 77 | 7 services: `ICurrentSessionService`, `IAutomationDashboardQueryService`, `IWorkflowService`, `IBusinessRuleService`, `IScheduledJobService`, `IApprovalService`, `IWorkflowExecutionEngine` | **0** | `AddTransient<AutomationPageViewModel>()` — `Presentation/DependencyInjection/ServiceCollectionExtensions.cs:74` |

`AutomationPageViewModel` reads `CurrentOrganization?.Id`, `CurrentBranch?.Id`,
`CurrentRole.ToString()` once, then constructs all five tab ViewModels **inline with `new`**
(`:38–42`) and fire-and-forgets `.LoadAsync()` on each (`:52–56`). It has **no `try`/`catch` of its
own** — it needs **no logger for itself**; it only needs to **carry and forward** a logger to each child.

### B.2 Child tab ViewModels (all `new`-by-parent, never DI-resolved)

| VM | Ctor dependencies | Broad `catch (Exception) when (not OCE)` sites | Catch → method |
|---|---|---|---|
| `AutomationDashboardTabViewModel` | `IAutomationDashboardQueryService`, `IWorkflowExecutionEngine` | **1** | `LoadAsync` (:116) |
| `ApprovalsTabViewModel` | `IApprovalService`, `string currentUserId` | **2** | `LoadAsync` (:73), `DecideAsync` (:89) |
| `BusinessRulesTabViewModel` | `IBusinessRuleService`, `string organizationId`, `string branchId` | **2** | `LoadAsync` (:139), `CreateAsync` (:168) |
| `ScheduledJobsTabViewModel` | `IScheduledJobService`, `IWorkflowService`, `string organizationId`, `string branchId` | **3** | `LoadAsync` (:129), `CreateAsync` (:153), `RunNowAsync` (:178) |
| `WorkflowsTabViewModel` | `IWorkflowService`, `IWorkflowExecutionEngine`, `string currentUserId`, `string organizationId`, `string branchId` | **5** | `LoadAsync` (:138), `CreateDraftAsync` (:173), `PublishAsync` (:197), `RunNowAsync` (:221), `RollbackAsync` (:234) |

**Total instrumentable catch sites: 13** (1 + 2 + 2 + 3 + 5).

### B.3 Uniform error-handling pattern (all 13 sites, verified identical)

```csharp
catch (Exception exception) when (exception is not OperationCanceledException)
{
    ErrorMessage = exception.Message;   // unchanged — surfaces to the tab's error banner
    State = DashboardState.Error;       // present on LoadAsync sites; absent on action sites
}
```

Every catch **swallows** (no rethrow) and records `exception.Message` to a bound `ErrorMessage`
property. This is exactly the pattern instrumented in Waves 1 / 2A / 2B — a single `[LoggerMessage]`
call appended **after** the unchanged `ErrorMessage = exception.Message;` line.

### B.4 Out of scope (missing-guard, do NOT add catches)

These methods call services with **no `try`/`catch`** — adding guards is a separate *missing-guard*
concern, explicitly **not** part of this logging wave:
`WorkflowsTabViewModel.LoadVersionHistoryAsync` / `ArchiveAsync` / `DeleteAsync`;
`BusinessRulesTabViewModel.ToggleEnabledAsync` / `DeleteAsync`;
`ScheduledJobsTabViewModel.ToggleEnabledAsync` / `DeleteAsync`.

### B.5 Existing tests

`tests/Rojan.Desktop.Presentation.Tests/Automation/`:

| File | Notes |
|---|---|
| `StubAutomationServices.cs` | `internal sealed` in-memory stubs: `FakeCurrentSessionService`, `StubWorkflowService`, `StubBusinessRuleService`, `StubScheduledJobService`, `StubApprovalService`, `StubWorkflowExecutionEngine`, `StubAutomationDashboardQueryService`. **None currently have a throw hook** (only `StubScheduledJobService.RunDueJobResultFactory` and `StubWorkflowService.ValidateErrors` are configurable). Shared across all 6 test files. |
| `AutomationPageViewModelTests.cs` | 3 tests; `CreateSut()` builds the parent with the 7 stubs positionally. |
| `AutomationDashboardTabViewModelTests.cs`, `ApprovalsTabViewModelTests.cs`, `BusinessRulesTabViewModelTests.cs`, `ScheduledJobsTabViewModelTests.cs`, `WorkflowsTabViewModelTests.cs` | Per-tab wiring tests; each has a local `CreateSut()` building the tab with stubs positionally. |

`RecordingLogger<T>` — `tests/Rojan.Desktop.Presentation.Tests/Specialists/RecordingLogger.cs`,
namespace `Rojan.Desktop.Presentation.Tests.Specialists` — reusable cross-namespace via `using`
(already done in Membership / Organizations / Specialists test files).

---

## C. Parent–Child Dependency Analysis

### C.1 The plumbing problem

The five tab ViewModels are **not** in the DI container. They are `new`-ed by
`AutomationPageViewModel`. The DI container therefore cannot inject an `ILogger<WorkflowsTabViewModel>`
directly into `WorkflowsTabViewModel`. The parent must **receive** a logger per child from DI and
**pass it through** to each `new`.

### C.2 Precedent — `AccountingPageViewModel` → `PosCheckoutViewModel`

`src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs`:
- `:32` `private readonly ILogger<PosCheckoutViewModel>? _posCheckoutLogger;` — a **typed, nullable
  pass-through field** for the child
- `:48` ctor param `ILogger<PosCheckoutViewModel>? posCheckoutLogger = null` — **optional**, appended
  before the class's own `logger`
- `:227` `new PosCheckoutViewModel(..., _posCheckoutLogger)` — forwarded as the last arg
- `PosCheckoutViewModel` ctor takes `ILogger<PosCheckoutViewModel>? logger = null` and does
  `?? NullLogger<PosCheckoutViewModel>.Instance`

`SpecialistPageViewModel` / `SpecialistProfileViewModel` follow the same shape for
`ILogger<SpecialistScheduleViewModel>?` / `ILogger<SpecialistAvailabilityViewModel>?`.

**This is the established pattern. Wave 2C-2 replicates it, once per child (×5).**

### C.3 Recommended plumbing shape

**`AutomationPageViewModel`** (stays `sealed class` — no `partial`, no `[LoggerMessage]`, 0 catches):

```csharp
public AutomationPageViewModel(
    ICurrentSessionService currentSessionService,
    IAutomationDashboardQueryService dashboardQueryService,
    IWorkflowService workflowService,
    IBusinessRuleService businessRuleService,
    IScheduledJobService scheduledJobService,
    IApprovalService approvalService,
    IWorkflowExecutionEngine executionEngine,
    // ── new, all optional, appended last ──
    ILogger<AutomationDashboardTabViewModel>? dashboardLogger = null,
    ILogger<WorkflowsTabViewModel>? workflowsLogger = null,
    ILogger<BusinessRulesTabViewModel>? businessRulesLogger = null,
    ILogger<ScheduledJobsTabViewModel>? scheduledJobsLogger = null,
    ILogger<ApprovalsTabViewModel>? approvalsLogger = null)
{
    ...
    Dashboard      = new AutomationDashboardTabViewModel(dashboardQueryService, executionEngine, dashboardLogger);
    Workflows      = new WorkflowsTabViewModel(workflowService, executionEngine, currentUserId, organizationId, branchId, workflowsLogger);
    BusinessRules  = new BusinessRulesTabViewModel(businessRuleService, organizationId, branchId, businessRulesLogger);
    ScheduledJobs  = new ScheduledJobsTabViewModel(scheduledJobService, workflowService, organizationId, branchId, scheduledJobsLogger);
    Approvals      = new ApprovalsTabViewModel(approvalService, currentUserId, approvalsLogger);
    ...
}
```

**Each tab ViewModel** — the standard self-logging shape from Waves 1 / 2A / 2B:
- `sealed class` → `sealed partial class`
- `+ using Microsoft.Extensions.Logging;` `+ using Microsoft.Extensions.Logging.Abstractions;`
- `private readonly ILogger<TSelf> _logger;`
- ctor `+ ILogger<TSelf>? logger = null` (optional, appended last)
- `_logger = logger ?? NullLogger<TSelf>.Instance;`
- one instance-form partial:
  ```csharp
  [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "<tab> automation operation failed. Operation={Operation}")]
  private partial void LogOperationFailed(string operation);
  ```
- in each catch, **after** the unchanged `ErrorMessage = exception.Message;`:
  `LogOperationFailed(nameof(<Method>));`

### C.4 `SYSLIB1020` / `CS8795` risk assessment

`SYSLIB1020` fires when the `[LoggerMessage]` source generator finds **2+ `ILogger` fields** in one
class.

| Class | `ILogger` fields after change | Instance-form `[LoggerMessage]` safe? |
|---|---|---|
| `AutomationPageViewModel` | 5 pass-through fields, **but no `[LoggerMessage]` in this class** | N/A — no source-gen here |
| `AutomationDashboardTabViewModel` | 1 | ✅ |
| `ApprovalsTabViewModel` | 1 | ✅ |
| `BusinessRulesTabViewModel` | 1 | ✅ |
| `ScheduledJobsTabViewModel` | 1 | ✅ |
| `WorkflowsTabViewModel` | 1 | ✅ |

**No static-form `[LoggerMessage]` needed anywhere in this wave.** The parent holds 5 loggers but emits
no log message itself, so the generator never runs on it.

### C.5 DI impact

**None required.** `AddTransient<AutomationPageViewModel>()` is unchanged. The open-generic
`ILogger<T>` registration (`Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:91`
`AddLogging()`) resolves each `ILogger<XxxTabViewModel>` automatically. All five new ctor params are
optional, so **no existing call site or test breaks**.

### C.6 Architecture-test impact

**None.** `Microsoft.Extensions.Logging.Abstractions` is already a Presentation `PackageReference`
(`Rojan.Desktop.Presentation.csproj:14`) and is explicitly **not** on the
`DependencyDirectionTests` forbidden list. No `System.Windows.*` type is introduced. Expect 7 / 7
unchanged.

---

## D. Security Constraints

**Design rule (unchanged from Phase 8.15 onward): operation name only. The `Exception` object is
NEVER passed to the logger.** The `[LoggerMessage]` signature is `(string operation)` — no `Exception`
parameter — and every call site passes `nameof(<Method>)`.

The only log lines this wave can ever produce:

```
<timestamp> [Error] …AutomationDashboardTabViewModel: Dashboard automation operation failed. Operation=LoadAsync
<timestamp> [Error] …WorkflowsTabViewModel: Workflows automation operation failed. Operation=PublishAsync
<timestamp> [Error] …BusinessRulesTabViewModel: Business rules automation operation failed. Operation=CreateAsync
<timestamp> [Error] …ScheduledJobsTabViewModel: Scheduled jobs automation operation failed. Operation=RunNowAsync
<timestamp> [Error] …ApprovalsTabViewModel: Approvals automation operation failed. Operation=DecideAsync
```

### D.1 FORBIDDEN — must never reach the log

| Category | Automation-specific examples in these VMs |
|---|---|
| `Exception` object / `Exception.Message` | never passed — the source of every other leak |
| Backend response body | only ever carried by `Exception.Message` — never passed |
| Workflow content | workflow names/descriptions (`NewWorkflowName`, `WorkflowDefinitionDto.Name`), step definitions, `ParentWorkflowId`, version numbers |
| Business-rule content | rule names, condition fields/values ("IF Customer is VIP → Apply Discount"), action values, `BusinessRuleDto` facts |
| Scheduled-job content | job names, **cron expressions**, frequency, target workflow ids |
| Approval content | request **titles / descriptions / decision comments** (`DecisionComment`), approver roles, `ApprovalRequestDto` fields |
| Identity | `_currentUserId` (from `CurrentRole.ToString()`), requesting-user ids |
| Tenant identifiers | `_organizationId`, `_branchId` |
| Customer / employee / product data | anything a rule or approval may reference about a person |
| Tokens | bearer / session — not held by these VMs; never logged |

### D.2 ALLOWED

- The literal method name via `nameof` (`LoadAsync`, `CreateAsync`, `CreateDraftAsync`,
  `PublishAsync`, `RunNowAsync`, `RollbackAsync`, `DecideAsync`)
- `LogLevel.Error` (clears the `LocalFileLoggerProvider` `Warning` floor)
- `EventId = 1` per class

### D.3 Behaviour-preservation checklist (per site)

- catch filter `when (exception is not OperationCanceledException)` unchanged
- `ErrorMessage = exception.Message;` unchanged (still feeds the tab's error banner)
- `State = DashboardState.Error;` unchanged where present
- log call appended as the **last** statement in the catch block
- no `#pragma`/`finally`/command-wiring change
- parent: `new` order, `LoadAsync` fire-and-forget order, `SelectTabCommand`, `SelectedTabIndex` all unchanged

---

## E. Test Strategy

### E.1 Reuse, don't rebuild

- **`RecordingLogger<T>`** (`…Tests.Specialists`) — reuse via `using`. Do **not** create a new logger double.
- Existing per-tab `CreateSut()` helpers and `AutomationPageViewModelTests.CreateSut()` compile
  **unchanged** (new ctor params are optional) — no existing test body is modified.

### E.2 Throw hooks — the one unavoidable test-infra change

The shared stubs in `StubAutomationServices.cs` have no way to force a failure. Two options:

| Option | Assessment |
|---|---|
| **(Recommended)** Add a nullable throw hook to each shared stub used by a catch site — e.g. `public Exception? LoadException { get; set; }` on `StubWorkflowService`, `StubBusinessRuleService`, `StubScheduledJobService`, `StubApprovalService`, `StubAutomationDashboardQueryService`; the relevant methods do `if (LoadException is not null) throw LoadException;` at entry. **Additive, default `null`, single file, zero behaviour change for existing tests.** | Lowest total risk. These are `internal` test-only doubles in one file; every existing test keeps passing because the hook defaults off. |
| Private nested throwing stubs per new test file (`ThrowingWorkflowService : IWorkflowService`, …) | Purist "don't touch shared stubs" path, but `IWorkflowService` (~10 members), `IScheduledJobService` (~9), `IBusinessRuleService` (~8) each need a full re-implementation per file — far more surface area and more error-prone than a one-line additive hook. |

**Recommendation: Option 1** — it is the "unavoidable, minimal, additive" case the standing constraint
allows. Flag it explicitly in the implementation report and commit-scope review so the reviewer sees
the shared-stub delta is additive-only.

`FakeCurrentSessionService` needs **no** change — the parent has 0 catches and its session reads
can't fail in a way this wave logs.

### E.3 Test matrix (per tab, using `RecordingLogger<T>`)

For each of the 5 tabs:
1. **`LoadAsync` failure logs one `Error` entry, operation name only** — seed the stub's throw hook
   with an exception whose message embeds a recognisable secret
   (e.g. `"VIP-customer-rule-SECRET"`, `"cron 0 9 * * 1 SECRET"`, `"approval-comment-SECRET"`);
   assert: exactly one entry, `LogLevel.Error`, `Message` contains `"Operation=LoadAsync"` and the
   `nameof` method, and `Assert.DoesNotContain(secret, entry.Message)`.
2. **No logger supplied → `NullLogger`, failure path never throws** — construct the tab with no
   logger arg, force the failure, assert `ErrorMessage` is still set and no exception escapes.

Plus, for tabs with additional instrumented actions, one assertion each that the action catch
(`DecideAsync`, `CreateAsync`, `PublishAsync`, `RunNowAsync`, `RollbackAsync`, `CreateDraftAsync`)
logs `Operation=<thatMethod>`.

Optional parent smoke test: `AutomationPageViewModel` constructed with five `RecordingLogger<T>`
instances → force each tab's `LoadAsync` to fail via the stubs → assert each recorder captured its
tab's entry (proves the pass-through wiring).

### E.4 Expected test-count delta

~**+11 to +14** tests (2 per tab + ~1 action-site assertion for the multi-catch tabs + optional
parent smoke). Projected total **2,557 → ≈ 2,570**. Exact number reported in the implementation phase.

---

## F. Recommended Implementation Order

**One implementation phase, one commit** (see G for the split contingency).

1. **Parent plumbing** — `AutomationPageViewModel`: +2 `using`s, 5 nullable pass-through fields, 5
   optional ctor params (appended last), forward each to its `new`. No `partial`, no `[LoggerMessage]`.
2. **Tab ViewModels, ascending catch count** (smallest blast radius first, easy to bisect a build break):
   1. `AutomationDashboardTabViewModel` (1 catch — `LoadAsync`)
   2. `ApprovalsTabViewModel` (2 — `LoadAsync`, `DecideAsync`)
   3. `BusinessRulesTabViewModel` (2 — `LoadAsync`, `CreateAsync`)
   4. `ScheduledJobsTabViewModel` (3 — `LoadAsync`, `CreateAsync`, `RunNowAsync`)
   5. `WorkflowsTabViewModel` (5 — `LoadAsync`, `CreateDraftAsync`, `PublishAsync`, `RunNowAsync`, `RollbackAsync`)
   Each: `sealed`→`sealed partial`, +2 `using`s, `_logger` field, optional ctor param, `NullLogger`
   fallback, one instance-form `[LoggerMessage]` (EventId 1, Error), `LogOperationFailed(nameof(X))`
   appended in each catch after the unchanged `ErrorMessage = exception.Message;`.
3. **Test infra** — add additive `Exception?` throw hooks to the 5 shared stubs in
   `StubAutomationServices.cs`.
4. **Tests** — per §E.3, one file per tab (5 files touched), reusing `RecordingLogger<T>`.
5. **Validate** — `dotnet build` (expect 0 warnings / 0 errors; watch for `SYSLIB1020` — should not
   occur), `dotnet test --no-build` (expect ≈ 2,570 / all pass), architecture 7 / 7.

### Files in scope (implementation phase)

| # | File | Change |
|---|---|---|
| 1 | `…/ViewModels/Automation/AutomationPageViewModel.cs` | +usings, 5 fields, 5 ctor params, forward to 5 `new` |
| 2 | `…/ViewModels/Automation/AutomationDashboardTabViewModel.cs` | self-logging shape, 1 call site |
| 3 | `…/ViewModels/Automation/ApprovalsTabViewModel.cs` | self-logging shape, 2 call sites |
| 4 | `…/ViewModels/Automation/BusinessRulesTabViewModel.cs` | self-logging shape, 2 call sites |
| 5 | `…/ViewModels/Automation/ScheduledJobsTabViewModel.cs` | self-logging shape, 3 call sites |
| 6 | `…/ViewModels/Automation/WorkflowsTabViewModel.cs` | self-logging shape, 5 call sites |
| 7 | `tests/…/Automation/StubAutomationServices.cs` | additive `Exception?` throw hooks (5 stubs) |
| 8 | `tests/…/Automation/AutomationDashboardTabViewModelTests.cs` | +tests |
| 9 | `tests/…/Automation/ApprovalsTabViewModelTests.cs` | +tests |
| 10 | `tests/…/Automation/BusinessRulesTabViewModelTests.cs` | +tests |
| 11 | `tests/…/Automation/ScheduledJobsTabViewModelTests.cs` | +tests |
| 12 | `tests/…/Automation/WorkflowsTabViewModelTests.cs` | +tests |
| (opt) | `tests/…/Automation/AutomationPageViewModelTests.cs` | +1 parent pass-through smoke test |

**Not touched:** DI registration, any interface, any DTO, any Domain/Infrastructure/Shell file, any
API client, RBAC, navigation, `FakeCurrentSessionService`, `RecordingLogger.cs`.

---

## G. Split Contingency (TASK 7)

TASK 7: *"Consider splitting if — parent plumbing increases risk / multiple lifecycle patterns exist."*

**Assessment: neither trigger fires.**

| Trigger | Finding |
|---|---|
| Multiple lifecycle patterns | **No.** All 5 tabs are identical: `new`-ed in the parent ctor, fire-and-forget `LoadAsync()`, identical `catch (Exception) when (not OCE)` → `ErrorMessage = exception.Message;` shape. One pattern. |
| Parent plumbing increases risk | **Low.** The parent gains 5 optional nullable fields and forwards them; it has **0 catches**, emits **0 log messages**, needs **no `partial`**, and every new param is optional so nothing downstream breaks. No `SYSLIB1020` exposure (no source-gen in the parent). No DI change. |

**Recommendation: single implementation phase, single commit** — subject `fix(desktop): add ViewModel
diagnostic logging (automation tabs)`.

**Contingency only** — if the commit-scope review judges the ~12-file diff too large to review as one
unit, split by **commit** (not by phase):
- Commit A — parent plumbing + `AutomationDashboardTabViewModel` + `ApprovalsTabViewModel` +
  `ScheduledJobsTabViewModel` + stub hooks + their tests
- Commit B — `WorkflowsTabViewModel` + `BusinessRulesTabViewModel` + their tests

Commit A must include the parent plumbing for the tabs it instruments; the two Commit-B tabs would
receive their pass-through param and `new`-forwarding in Commit B (parent ctor grows again). Single
commit is cleaner and is the primary recommendation.

---

## STOP

Audit complete. No source, tests, DI, or stubs modified. No commit, no push. `ROJAN_PHASE8_38_…v1.md`
written. Awaiting Phase 8.39 (implementation authorization) or a scope adjustment.
